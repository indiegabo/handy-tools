using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.CutscenesModule.Events;
using IndieGabo.HandyTools.CutscenesModule.Services;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    public sealed class CutsceneRun
    {
        private readonly Dictionary<string, int> _signals = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<SerializableGuid, ExecutionState> _activeExecutions = new();
        private readonly List<SerializableGuid> _executionOrder = new();
        private readonly Dictionary<SerializableGuid, ParallelGroupState> _parallelGroups = new();
        private readonly GraphBlackboard _runtimeBlackboard;
        private readonly GraphDefinition _runtimeGraphDefinition;

        private SerializableGuid _lastNodeId = SerializableGuid.Empty;
        private SerializableGuid _lastExecutionId = SerializableGuid.Empty;
        private bool _isAdvancingRun;

        public CutsceneRun(CutsceneDirector director, ICutsceneService service)
        {
            Director = director;
            Service = service;
            Graph = director.Graph;
            _runtimeBlackboard = CutsceneGraphCoreRuntimeMigrationUtility.CreateGraphBlackboard(
                Graph.Blackboard);
            _runtimeGraphDefinition =
                CutsceneGraphCoreRuntimeMigrationUtility.CreateGraphDefinition(
                    Graph,
                    _runtimeBlackboard);
            StateStore = new CutsceneRuntimeStateStore();
            Trace = new CutsceneRunTrace();
            RuntimeTrace = new GraphRunTrace();
        }

        public CutsceneDirector Director { get; }

        public ICutsceneService Service { get; }

        public CutsceneGraph Graph { get; }

        public CutsceneGraphBlackboard Blackboard => Graph.Blackboard;

        public GraphBlackboard RuntimeBlackboard => _runtimeBlackboard;

        public GraphDefinition RuntimeGraphDefinition => _runtimeGraphDefinition;

        public CutsceneRuntimeStateStore StateStore { get; }

        public CutsceneRunTrace Trace { get; }

        public GraphRunTrace RuntimeTrace { get; }

        public CutsceneRunStatus Status { get; private set; } = CutsceneRunStatus.Idle;

        public SerializableGuid CurrentNodeId
        {
            get
            {
                if (_executionOrder.Count > 0
                    && _activeExecutions.TryGetValue(_executionOrder[0], out ExecutionState state))
                {
                    return state.NodeId;
                }

                return _lastNodeId;
            }
        }

        public SerializableGuid CurrentExecutionId
        {
            get
            {
                return _executionOrder.Count > 0
                    ? _executionOrder[0]
                    : _lastExecutionId;
            }
        }

        public IReadOnlyCollection<SerializableGuid> ActiveNodeIds => _executionOrder
            .Where(executionId => _activeExecutions.ContainsKey(executionId))
            .Select(executionId => _activeExecutions[executionId].NodeId)
            .Distinct()
            .ToArray();

        public float CurrentDeltaTime { get; private set; }

        public float CurrentUnscaledDeltaTime { get; private set; }

        public string FailureReason { get; private set; } = string.Empty;

        public bool IsTerminal => Status == CutsceneRunStatus.Success
            || Status == CutsceneRunStatus.Failed
            || Status == CutsceneRunStatus.Cancelled;

        public void Start()
        {
            if (Status != CutsceneRunStatus.Idle)
            {
                return;
            }

            if (!CutsceneGraphCoreRuntimeMigrationUtility.TryGetEntryNode(
                RuntimeGraphDefinition,
                out GraphNodeBase entryNode))
            {
                Fail("Cutscene graph does not contain an entry node.");
                return;
            }

            Status = CutsceneRunStatus.Running;
            HandyBus<CutsceneStartedEvent>.Raise(new CutsceneStartedEvent(Director, this));
            EnterNode(entryNode, SerializableGuid.Empty);
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (Status != CutsceneRunStatus.Running)
            {
                return;
            }

            _isAdvancingRun = true;

            try
            {
                CurrentDeltaTime = deltaTime;
                CurrentUnscaledDeltaTime = unscaledDeltaTime;

                List<SerializableGuid> executionSnapshot = _executionOrder.ToList();

                for (int index = 0; index < executionSnapshot.Count; index++)
                {
                    SerializableGuid executionId = executionSnapshot[index];

                    if (!_activeExecutions.TryGetValue(executionId, out ExecutionState state)
                        || !RuntimeGraphDefinition.TryGetNode(state.NodeId, out GraphNodeBase currentNode))
                    {
                        continue;
                    }

                    if (currentNode.RequiresTick
                        && !CutsceneGraphCoreRuntimeMigrationUtility.TryTickGraphNode(
                            currentNode,
                            state.Context))
                    {
                        RemoveExecution(executionId);
                        Fail("Current cutscene node could not be ticked.");
                        return;
                    }

                    ConsumePendingCompletion(executionId);

                    if (Status != CutsceneRunStatus.Running)
                    {
                        return;
                    }
                }
            }
            finally
            {
                _isAdvancingRun = false;
            }
        }

        public bool TryCompleteNode(SerializableGuid executionId, CutsceneNodeResult result)
        {
            return TryCompleteNode(
                executionId,
                CutsceneGraphExecutionContractAdapter.ToGraphExecutionResult(result));
        }

        public bool TryCompleteNode(SerializableGuid executionId, GraphExecutionResult result)
        {
            if (Status != CutsceneRunStatus.Running
                || executionId == SerializableGuid.Empty
                || !_activeExecutions.TryGetValue(executionId, out ExecutionState state)
                || state.HasPendingCompletion)
            {
                return false;
            }

            state.HasPendingCompletion = true;
            state.PendingResult = result;

            if (!_isAdvancingRun)
            {
                ConsumePendingCompletion(executionId);
            }

            return true;
        }

        public void Cancel(string reason)
        {
            if (IsTerminal)
            {
                return;
            }

            ExitActiveNodes();
            _activeExecutions.Clear();
            _executionOrder.Clear();
            _parallelGroups.Clear();

            FailureReason = reason ?? string.Empty;
            Status = CutsceneRunStatus.Cancelled;
            Trace.MarkEnded(Status, FailureReason);
            RuntimeTrace.MarkEnded(
                CutsceneGraphExecutionContractAdapter.ToGraphRunStatus(Status),
                FailureReason);

            HandyBus<CutsceneCancelledEvent>.Raise(new CutsceneCancelledEvent(Director, this, FailureReason));
            HandyBus<CutsceneEndedEvent>.Raise(new CutsceneEndedEvent(Director, this, Status, FailureReason));
        }

        public void EmitSignal(string signalId)
        {
            if (string.IsNullOrWhiteSpace(signalId))
            {
                return;
            }

            _signals.TryGetValue(signalId, out int count);
            _signals[signalId] = count + 1;
        }

        public bool HasSignal(string signalId)
        {
            return !string.IsNullOrWhiteSpace(signalId)
                && _signals.TryGetValue(signalId, out int count)
                && count > 0;
        }

        public bool TryConsumeSignal(string signalId)
        {
            if (!HasSignal(signalId))
            {
                return false;
            }

            int nextCount = _signals[signalId] - 1;

            if (nextCount <= 0)
            {
                _signals.Remove(signalId);
            }
            else
            {
                _signals[signalId] = nextCount;
            }

            return true;
        }

        private void ConsumePendingCompletion(SerializableGuid executionId)
        {
            if (!_activeExecutions.TryGetValue(executionId, out ExecutionState state)
                || !state.HasPendingCompletion)
            {
                return;
            }

            GraphExecutionResult result = state.PendingResult;
            state.HasPendingCompletion = false;

            if (!RuntimeGraphDefinition.TryGetNode(state.NodeId, out GraphNodeBase currentGraphNode))
            {
                RemoveExecution(executionId);
                Fail("Current cutscene node could not be resolved.");
                return;
            }

            RemoveExecution(executionId);

            if (!CutsceneGraphCoreRuntimeMigrationUtility.TryExitGraphNode(
                    currentGraphNode,
                    state.Context))
            {
                Fail("Current cutscene node could not exit.");
                return;
            }

            CutsceneNodeResult legacyResult =
                CutsceneGraphExecutionContractAdapter.ToCutsceneNodeResult(result);
            Trace.MarkNodeFinished(
                currentGraphNode.Id,
                legacyResult.Status,
                legacyResult.OutputKey,
                legacyResult.FailureReason);
            RuntimeTrace.MarkNodeFinished(
                currentGraphNode.Id,
                result.Status,
                result.OutputKey,
                result.FailureReason);

            HandyBus<CutsceneNodeFinishedEvent>.Raise(
                new CutsceneNodeFinishedEvent(
                    Director,
                    this,
                    currentGraphNode.Id,
                    legacyResult.Status,
                    legacyResult.OutputKey,
                    legacyResult.FailureReason));

            if (result.Status == GraphNodeStatus.Failure)
            {
                Fail(string.IsNullOrWhiteSpace(result.FailureReason)
                    ? "Cutscene node reported failure."
                    : result.FailureReason);
                return;
            }

            if (CutsceneGraphCoreRuntimeMigrationUtility.IsFinishNode(currentGraphNode))
            {
                ResolveCompletedExecution(state);
                return;
            }

            if (CutsceneGraphCoreRuntimeMigrationUtility.IsParallelNode(currentGraphNode))
            {
                SpawnParallelBranches(state, currentGraphNode);
                return;
            }

            string outputKey = string.IsNullOrWhiteSpace(result.OutputKey)
                ? CutsceneNodePorts.Next
                : result.OutputKey;

            if (!RuntimeGraphDefinition.TryGetOutgoingConnection(
                    currentGraphNode.Id,
                    outputKey,
                    out GraphConnection connection))
            {
                Fail($"Node '{currentGraphNode.DisplayTitle}' has no connection for output '{outputKey}'.");
                return;
            }

            if (!RuntimeGraphDefinition.TryGetNode(connection.ToNodeId, out GraphNodeBase nextNode))
            {
                Fail("The next cutscene node could not be resolved.");
                return;
            }

            EnterNode(nextNode, state.OwningParallelGroupId);
        }

        private void EnterNode(GraphNodeBase node, SerializableGuid owningParallelGroupId)
        {
            if (Status != CutsceneRunStatus.Running)
            {
                return;
            }

            bool previousAdvancingState = _isAdvancingRun;
            _isAdvancingRun = true;

            try
            {
                SerializableGuid executionId = SerializableGuid.NewGuid();
                CutsceneExecutionContext context = new(this, node.Id, executionId);
                ExecutionState state = new(executionId, node.Id, owningParallelGroupId, context);

                _activeExecutions[executionId] = state;
                _executionOrder.Add(executionId);
                _lastNodeId = node.Id;
                _lastExecutionId = executionId;

                Trace.MarkNodeStarted(node.Id);
                RuntimeTrace.MarkNodeStarted(node.Id);

                HandyBus<CutsceneNodeStartedEvent>.Raise(new CutsceneNodeStartedEvent(Director, this, node.Id));

                if (CutsceneGraphCoreRuntimeMigrationUtility.TryEnterGraphNode(node, context))
                {
                    ConsumePendingCompletion(executionId);
                    return;
                }

                RemoveExecution(executionId);
                Fail("The cutscene runtime node could not be executed.");
            }
            finally
            {
                _isAdvancingRun = previousAdvancingState;
            }
        }

        private void SpawnParallelBranches(ExecutionState completedState, GraphNodeBase parallelNode)
        {
            IReadOnlyList<GraphPortDefinition> outputPorts = parallelNode.GetOutputPorts();

            if (outputPorts == null || outputPorts.Count == 0)
            {
                Fail($"Fork node '{parallelNode.DisplayTitle}' does not define any branches.");
                return;
            }

            List<GraphNodeBase> branchTargets = new(outputPorts.Count);

            for (int index = 0; index < outputPorts.Count; index++)
            {
                GraphPortDefinition outputPort = outputPorts[index];

                if (!RuntimeGraphDefinition.TryGetOutgoingConnection(
                        parallelNode.Id,
                        outputPort.Key,
                        out GraphConnection connection))
                {
                    Fail($"Fork node '{parallelNode.DisplayTitle}' has no connection for output '{outputPort.DisplayName}'.");
                    return;
                }

                if (!RuntimeGraphDefinition.TryGetNode(connection.ToNodeId, out GraphNodeBase nextNode))
                {
                    Fail("The next cutscene node could not be resolved.");
                    return;
                }

                Trace.MarkOutputTraversed(parallelNode.Id, outputPort.Key);
                RuntimeTrace.MarkOutputTraversed(parallelNode.Id, outputPort.Key);
                branchTargets.Add(nextNode);
            }

            SerializableGuid parallelGroupId = SerializableGuid.NewGuid();
            _parallelGroups[parallelGroupId] = new ParallelGroupState(
                parallelGroupId,
                completedState.OwningParallelGroupId,
                branchTargets.Count);

            for (int index = 0; index < branchTargets.Count; index++)
            {
                EnterNode(branchTargets[index], parallelGroupId);

                if (Status != CutsceneRunStatus.Running)
                {
                    return;
                }
            }
        }

        private void ResolveCompletedExecution(ExecutionState state)
        {
            if (state.OwningParallelGroupId == SerializableGuid.Empty)
            {
                if (_executionOrder.Count == 0 && _parallelGroups.Count == 0)
                {
                    Complete();
                }

                return;
            }

            ResolveCompletedParallelBranch(state.OwningParallelGroupId);
        }

        private void ResolveCompletedParallelBranch(SerializableGuid parallelGroupId)
        {
            if (!_parallelGroups.TryGetValue(parallelGroupId, out ParallelGroupState parallelGroup))
            {
                return;
            }

            if (!parallelGroup.TryConsumeBranch())
            {
                return;
            }

            _parallelGroups.Remove(parallelGroupId);

            if (parallelGroup.OwningParallelGroupId != SerializableGuid.Empty)
            {
                ResolveCompletedParallelBranch(parallelGroup.OwningParallelGroupId);
                return;
            }

            if (_executionOrder.Count == 0 && _parallelGroups.Count == 0)
            {
                Complete();
            }
        }

        private void RemoveExecution(SerializableGuid executionId)
        {
            _activeExecutions.Remove(executionId);
            _executionOrder.Remove(executionId);
        }

        private void ExitActiveNodes()
        {
            List<ExecutionState> executionStates = _executionOrder
                .Where(executionId => _activeExecutions.ContainsKey(executionId))
                .Select(executionId => _activeExecutions[executionId])
                .ToList();

            for (int index = 0; index < executionStates.Count; index++)
            {
                ExecutionState state = executionStates[index];

                if (RuntimeGraphDefinition.TryGetNode(state.NodeId, out GraphNodeBase currentNode))
                {
                    CutsceneGraphCoreRuntimeMigrationUtility.TryExitGraphNode(
                        currentNode,
                        state.Context);
                }
            }
        }

        private void Complete()
        {
            _parallelGroups.Clear();
            Status = CutsceneRunStatus.Success;
            Trace.MarkEnded(Status, string.Empty);
            RuntimeTrace.MarkEnded(
                CutsceneGraphExecutionContractAdapter.ToGraphRunStatus(Status),
                string.Empty);
            HandyBus<CutsceneEndedEvent>.Raise(new CutsceneEndedEvent(Director, this, Status, string.Empty));
        }

        private void Fail(string reason)
        {
            ExitActiveNodes();
            _activeExecutions.Clear();
            _executionOrder.Clear();
            _parallelGroups.Clear();

            FailureReason = reason ?? string.Empty;
            Status = CutsceneRunStatus.Failed;
            Trace.MarkEnded(Status, FailureReason);
            RuntimeTrace.MarkEnded(
                CutsceneGraphExecutionContractAdapter.ToGraphRunStatus(Status),
                FailureReason);
            HandyBus<CutsceneEndedEvent>.Raise(new CutsceneEndedEvent(Director, this, Status, FailureReason));
        }

        private sealed class ExecutionState
        {
            public ExecutionState(
                SerializableGuid executionId,
                SerializableGuid nodeId,
                SerializableGuid owningParallelGroupId,
                CutsceneExecutionContext context)
            {
                ExecutionId = executionId;
                NodeId = nodeId;
                OwningParallelGroupId = owningParallelGroupId;
                Context = context;
            }

            public SerializableGuid ExecutionId { get; }

            public SerializableGuid NodeId { get; }

            public SerializableGuid OwningParallelGroupId { get; }

            public CutsceneExecutionContext Context { get; }

            public bool HasPendingCompletion { get; set; }

            public GraphExecutionResult PendingResult { get; set; }
        }

        private sealed class ParallelGroupState
        {
            public ParallelGroupState(
                SerializableGuid id,
                SerializableGuid owningParallelGroupId,
                int remainingBranches)
            {
                Id = id;
                OwningParallelGroupId = owningParallelGroupId;
                RemainingBranches = remainingBranches;
            }

            public SerializableGuid Id { get; }

            public SerializableGuid OwningParallelGroupId { get; }

            public int RemainingBranches { get; private set; }

            public bool TryConsumeBranch()
            {
                RemainingBranches = System.Math.Max(0, RemainingBranches - 1);
                return RemainingBranches == 0;
            }
        }
    }
}