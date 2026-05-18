using IndieGabo.HandyTools.CutscenesModule.Services;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    public sealed class CutsceneExecutionContext
    {
        private readonly CutsceneRun _run;
        private readonly SerializableGuid _currentNodeId;
        private readonly SerializableGuid _currentNodeExecutionId;

        internal CutsceneExecutionContext(
            CutsceneRun run,
            SerializableGuid currentNodeId,
            SerializableGuid currentNodeExecutionId)
        {
            _run = run;
            _currentNodeId = currentNodeId;
            _currentNodeExecutionId = currentNodeExecutionId;
        }

        public CutsceneDirector Director => _run.Director;

        public ICutsceneService Service => _run.Service;

        public SerializableGuid CurrentNodeId => _currentNodeId;

        public SerializableGuid CurrentNodeExecutionId => _currentNodeExecutionId;

        public float DeltaTime => _run.CurrentDeltaTime;

        public float UnscaledDeltaTime => _run.CurrentUnscaledDeltaTime;

        public float GetDeltaTime(CutsceneTimeMode timeMode)
        {
            return timeMode == CutsceneTimeMode.Unscaled ? UnscaledDeltaTime : DeltaTime;
        }

        public bool TryComplete(CutsceneNodeResult result)
        {
            return _run.TryCompleteNode(CurrentNodeExecutionId, result);
        }

        public bool TryCompleteNode(SerializableGuid executionId, CutsceneNodeResult result)
        {
            return _run.TryCompleteNode(executionId, result);
        }

        public void EmitSignal(string signalId)
        {
            _run.EmitSignal(signalId);
        }

        public bool HasSignal(string signalId)
        {
            return _run.HasSignal(signalId);
        }

        public bool TryConsumeSignal(string signalId)
        {
            return _run.TryConsumeSignal(signalId);
        }

        public T GetOrCreateNodeState<T>(string key, System.Func<T> factory)
        {
            return _run.StateStore.GetOrCreate(_currentNodeId, key, factory);
        }

        public bool TryGetNodeState<T>(string key, out T value)
        {
            return _run.StateStore.TryGet(_currentNodeId, key, out value);
        }

        public void SetNodeState<T>(string key, T value)
        {
            _run.StateStore.Set(_currentNodeId, key, value);
        }

        public void RemoveNodeState(string key)
        {
            _run.StateStore.Remove(_currentNodeId, key);
        }

        /// <summary>
        /// Exposes the graph-level blackboard so nodes can read and write shared values.
        /// </summary>
        public CutsceneGraphBlackboard Blackboard => _run.Graph.Blackboard;

        /// <summary>
        /// Returns an existing blackboard value or creates one via the provided factory.
        /// Supported runtime types: int, float, string, bool and UnityEngine.Object (or subclasses).
        /// </summary>
        public T GetOrCreateBlackboardValue<T>(string key, System.Func<T> factory)
        {
            return _run.Graph.Blackboard.GetOrCreateValue(key, factory);
        }

        /// <summary>
        /// Attempts to retrieve a typed value from the graph blackboard.
        /// </summary>
        public bool TryGetBlackboardValue<T>(string key, out T value)
        {
            return _run.Graph.Blackboard.TryGetValue(key, out value);
        }

        /// <summary>
        /// Attempts to retrieve a typed value from the graph blackboard through one stable variable reference.
        /// </summary>
        public bool TryGetBlackboardValue<T>(
            CutsceneBlackboardVariableReference variableReference,
            out T value)
        {
            value = default;

            return variableReference != null
                && variableReference.TryGetValue(_run.Graph.Blackboard, out value);
        }

        /// <summary>
        /// Attempts to resolve one serialized blackboard entry through one stable variable reference.
        /// </summary>
        public bool TryGetBlackboardEntry(
            CutsceneBlackboardVariableReference variableReference,
            out CutsceneGraphBlackboardEntry entry)
        {
            entry = null;

            return variableReference != null
                && variableReference.TryResolveEntry(_run.Graph.Blackboard, out entry);
        }

        /// <summary>
        /// Sets a typed value on the graph blackboard.
        /// </summary>
        public void SetBlackboardValue<T>(string key, T value)
        {
            _run.Graph.Blackboard.SetValue(key, value);
        }

        /// <summary>
        /// Sets one boxed value on the graph blackboard.
        /// </summary>
        public bool TrySetBlackboardValue(
            string key,
            object value,
            System.Type valueType = null)
        {
            return _run.Graph.Blackboard.TrySetBoxedValue(key, value, valueType);
        }

        /// <summary>
        /// Sets a typed value on one existing blackboard variable reference.
        /// </summary>
        public void SetBlackboardValue<T>(
            CutsceneBlackboardVariableReference variableReference,
            T value)
        {
            if (!TryGetBlackboardEntry(variableReference, out CutsceneGraphBlackboardEntry entry))
            {
                return;
            }

            if (!entry.TrySetValue(value))
            {
                _run.Graph.Blackboard.SetValue(entry.Key, value);
            }
        }

        /// <summary>
        /// Removes an entry from the graph blackboard.
        /// </summary>
        public void RemoveBlackboardValue(string key)
        {
            _run.Graph.Blackboard.Remove(key);
        }

        public bool TryGetService<T>(out T service) where T : class
        {
            return ServiceLocator.TryGet(out service);
        }
    }
}