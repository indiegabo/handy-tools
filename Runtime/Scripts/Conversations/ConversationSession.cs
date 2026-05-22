using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Events;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.Utils;
using UnityEngine.Playables;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Identifies the current lifecycle state of one runtime conversation session.
    /// </summary>
    public enum ConversationSessionState
    {
        /// <summary>
        /// The session has been created but playback has not started yet.
        /// </summary>
        NotStarted,

        /// <summary>
        /// The session is currently presenting one active runtime line.
        /// </summary>
        Running,

        /// <summary>
        /// The session reached a terminal state naturally.
        /// </summary>
        Completed,

        /// <summary>
        /// The session was interrupted explicitly by cancellation.
        /// </summary>
        Canceled,

        /// <summary>
        /// The session encountered invalid runtime data and aborted.
        /// </summary>
        Faulted,
    }

    /// <summary>
    /// Executes one exported runtime conversation payload as a linear MVP session.
    /// </summary>
    public sealed class ConversationSession
    {
        #region Fields

        private readonly ConversationData _conversation;

        private readonly Dictionary<SerializableGuid, ConversationNodeData> _nodesById = new();

        private readonly Dictionary<SerializableGuid, ConversationActorData>
            _actorsById = new();

        private readonly IReadOnlyDictionary<string, string> _localizedTextsById;

        private readonly GraphBlackboard _runtimeBlackboard;

        private readonly IGraphBlackboardScopeResolver _scopeResolver;

        private ConversationNodeData _currentNode;

        private AsyncNodeExecution _activeAsyncExecution;

        private ConversationSessionState _state = ConversationSessionState.NotStarted;

        private string _failureReason = string.Empty;

        #endregion

        #region Events

        /// <summary>
        /// Raised after the active node or lifecycle state changes.
        /// </summary>
        public event Action<ConversationSession> Changed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the exported conversation payload executed by this session.
        /// </summary>
        public ConversationData Conversation => _conversation;

        /// <summary>
        /// Gets the runtime blackboard used to resolve blackboard-backed values.
        /// </summary>
        public GraphBlackboard RuntimeBlackboard => _runtimeBlackboard;

        /// <summary>
        /// Gets the current lifecycle state.
        /// </summary>
        public ConversationSessionState State => _state;

        /// <summary>
        /// Gets whether the session currently exposes one active runtime text line.
        /// </summary>
        public bool HasActiveLine => _state == ConversationSessionState.Running
            && _currentNode != null
            && IsPresentableLineKind(_currentNode.Kind);

        /// <summary>
        /// Gets the kind of the currently active runtime node when one exists.
        /// </summary>
        public ConversationNodeKind? CurrentNodeKind => _currentNode == null
            ? null
            : _currentNode.Kind;

        /// <summary>
        /// Gets whether the current active line should render speaker or listener presentation.
        /// </summary>
        public bool CurrentLineUsesSpeakerPresentation => _currentNode?.Kind
            == ConversationNodeKind.SpokenLine;

        /// <summary>
        /// Gets the failure reason recorded when the session faults.
        /// </summary>
        public string FailureReason => _failureReason ?? string.Empty;

        /// <summary>
        /// Gets the identifier of the currently active node.
        /// </summary>
        public SerializableGuid CurrentNodeId => _currentNode?.NodeId ?? SerializableGuid.Empty;

        /// <summary>
        /// Gets the currently active speaker when the active line binds one conversant.
        /// </summary>
        public ConversationActorData CurrentSpeaker => ResolveActor(
            _currentNode?.SpeakerActorId ?? SerializableGuid.Empty);

        /// <summary>
        /// Gets the currently active listener when the active line binds one conversant.
        /// </summary>
        public ConversationActorData CurrentListener => ResolveActor(
            _currentNode?.ListenerActorId ?? SerializableGuid.Empty);

        /// <summary>
        /// Gets the conversant currently occupying the left presenter slot.
        /// </summary>
        public ConversationActorData CurrentLeftParticipant => ResolveParticipantForSlot(
            ConversationParticipantSlot.Left);

        /// <summary>
        /// Gets the conversant currently occupying the right presenter slot.
        /// </summary>
        public ConversationActorData CurrentRightParticipant => ResolveParticipantForSlot(
            ConversationParticipantSlot.Right);

        /// <summary>
        /// Gets the resolved current line text.
        /// </summary>
        public string CurrentLineText => ResolveStringValue(
            _currentNode?.LineText,
            _currentNode?.TextId ?? string.Empty);

        /// <summary>
        /// Gets the stable identifier of the currently active authored text payload.
        /// </summary>
        public string CurrentLineTextId => _currentNode?.TextId ?? string.Empty;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes one linear runtime session from one exported conversation payload.
        /// </summary>
        /// <param name="conversation">Exported conversation payload to execute.</param>
        /// <param name="runtimeBlackboard">Runtime blackboard used for value resolution.</param>
        /// <param name="scopeResolver">
        /// Optional resolver used by blackboard references that target non-local scopes.
        /// </param>
        /// <param name="localizedTextsById">
        /// Optional localized-text lookup keyed by authored conversation text ids.
        /// </param>
        public ConversationSession(
            ConversationData conversation,
            GraphBlackboard runtimeBlackboard = null,
            IGraphBlackboardScopeResolver scopeResolver = null,
            IReadOnlyDictionary<string, string> localizedTextsById = null)
        {
            _conversation = conversation ?? throw new ArgumentNullException(nameof(conversation));
            _runtimeBlackboard = runtimeBlackboard ?? new GraphBlackboard();
            _scopeResolver = scopeResolver;
            _localizedTextsById = localizedTextsById;

            IndexNodes(conversation.Nodes);
            IndexActors(conversation.Actors);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts playback from the exported entry node.
        /// </summary>
        /// <returns>True when the start request was accepted.</returns>
        public bool Start()
        {
            if (_state != ConversationSessionState.NotStarted)
            {
                return false;
            }

            _failureReason = string.Empty;
            return MoveToNode(_conversation.EntryNodeId);
        }

        /// <summary>
        /// Advances from the current line to the next node.
        /// </summary>
        /// <returns>True when the advance request was accepted.</returns>
        public bool Advance()
        {
            return HasActiveLine && MoveToNode(_currentNode.NextNodeId);
        }

        /// <summary>
        /// Skips the current line and routes immediately to the next node.
        /// </summary>
        /// <returns>True when the skip request was accepted.</returns>
        public bool SkipCurrentLine()
        {
            return HasActiveLine && MoveToNode(_currentNode.NextNodeId);
        }

        /// <summary>
        /// Advances the currently active asynchronous utility node when its runtime state
        /// depends on per-frame updates.
        /// </summary>
        /// <param name="deltaTime">Scaled delta time for the current frame.</param>
        /// <param name="unscaledDeltaTime">Unscaled delta time for the current frame.</param>
        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (_state != ConversationSessionState.Running
                || _currentNode == null
                || !IsAsyncNodeKind(_currentNode.Kind))
            {
                return;
            }

            if (_activeAsyncExecution == null
                || _activeAsyncExecution.NodeId != _currentNode.NodeId)
            {
                Fail(
                    $"Conversation '{_conversation.Title}' lost runtime state for node "
                    + $"'{_currentNode.NodeId.ToHexString()}'.");
                return;
            }

            if (_activeAsyncExecution.CompletionRequested)
            {
                CompleteAsyncNode();
                return;
            }

            if (_currentNode.Kind != ConversationNodeKind.Wait)
            {
                return;
            }

            float delta = _activeAsyncExecution.TimeMode == ConversationTimeMode.Unscaled
                ? unscaledDeltaTime
                : deltaTime;
            _activeAsyncExecution.ElapsedSeconds += delta;

            if (_activeAsyncExecution.ElapsedSeconds >= _activeAsyncExecution.WaitDurationSeconds)
            {
                CompleteAsyncNode();
            }
        }

        /// <summary>
        /// Cancels the session immediately.
        /// </summary>
        /// <returns>True when the cancel request was accepted.</returns>
        public bool Cancel()
        {
            if (_state == ConversationSessionState.Completed
                || _state == ConversationSessionState.Canceled
                || _state == ConversationSessionState.Faulted)
            {
                return false;
            }

            ReleaseActiveRuntimeNodeState(stopTimeline: true);
            _currentNode = null;
            _state = ConversationSessionState.Canceled;
            NotifyChanged();
            return true;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Indexes the exported node payloads by stable identifier.
        /// </summary>
        /// <param name="nodes">Exported nodes to index.</param>
        private void IndexNodes(IReadOnlyList<ConversationNodeData> nodes)
        {
            _nodesById.Clear();

            if (nodes == null)
            {
                return;
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                ConversationNodeData node = nodes[index];

                if (node == null || node.NodeId == SerializableGuid.Empty)
                {
                    continue;
                }

                _nodesById[node.NodeId] = node;
            }
        }

        /// <summary>
        /// Indexes the exported conversant payloads by stable identifier.
        /// </summary>
        /// <param name="actors">Exported conversants to index.</param>
        private void IndexActors(IReadOnlyList<ConversationActorData> actors)
        {
            _actorsById.Clear();

            if (actors == null)
            {
                return;
            }

            for (int index = 0; index < actors.Count; index++)
            {
                ConversationActorData actor = actors[index];

                if (actor == null || actor.ActorId == SerializableGuid.Empty)
                {
                    continue;
                }

                _actorsById[actor.ActorId] = actor;
            }
        }

        /// <summary>
        /// Routes to the next playable node, traversing utility entry nodes inline.
        /// </summary>
        /// <param name="nodeId">Node that should become active.</param>
        /// <returns>True when the request was accepted.</returns>
        private bool MoveToNode(SerializableGuid nodeId)
        {
            ReleaseActiveRuntimeNodeState(stopTimeline: true);
            _currentNode = null;

            SerializableGuid currentNodeId = nodeId;

            while (true)
            {
                if (currentNodeId == SerializableGuid.Empty)
                {
                    Complete();
                    return true;
                }

                if (!_nodesById.TryGetValue(currentNodeId, out ConversationNodeData node)
                    || node == null)
                {
                    Fail(
                        $"Conversation '{_conversation.Title}' could not resolve node "
                        + $"'{currentNodeId.ToHexString()}'.");
                    return false;
                }

                switch (node.Kind)
                {
                    case ConversationNodeKind.Entry:
                        currentNodeId = node.NextNodeId;
                        continue;

                    case ConversationNodeKind.SpokenLine:
                    case ConversationNodeKind.NarrationLine:
                        _currentNode = node;
                        _state = ConversationSessionState.Running;
                        NotifyChanged();
                        return true;

                    case ConversationNodeKind.Wait:
                        if (node.WaitDurationSeconds <= 0f)
                        {
                            currentNodeId = node.NextNodeId;
                            continue;
                        }

                        _currentNode = node;
                        _activeAsyncExecution = new AsyncNodeExecution
                        {
                            NodeId = node.NodeId,
                            WaitDurationSeconds = node.WaitDurationSeconds,
                            TimeMode = node.TimeMode,
                        };
                        _state = ConversationSessionState.Running;
                        NotifyChanged();
                        return true;

                    case ConversationNodeKind.EmitHandyBusEvent:
                        EmitExternalEvent(node);
                        currentNodeId = node.NextNodeId;
                        continue;

                    case ConversationNodeKind.WaitForEvent:
                        _currentNode = node;
                        _activeAsyncExecution = CreateWaitForEventExecution(node);
                        _state = ConversationSessionState.Running;
                        NotifyChanged();
                        return true;

                    case ConversationNodeKind.PlayTimeline:
                        if (!TryActivatePlayTimelineNode(node))
                        {
                            return false;
                        }

                        return true;

                    default:
                        Fail(
                            $"Conversation '{_conversation.Title}' contains unsupported "
                            + $"runtime node kind '{node.Kind}'.");
                        return false;
                }
            }
        }

        /// <summary>
        /// Marks the session as completed.
        /// </summary>
        private void Complete()
        {
            ReleaseActiveRuntimeNodeState(stopTimeline: true);
            _currentNode = null;
            _state = ConversationSessionState.Completed;
            NotifyChanged();
        }

        /// <summary>
        /// Marks the session as faulted with one diagnostic reason.
        /// </summary>
        /// <param name="reason">Diagnostic reason recorded on the session.</param>
        private void Fail(string reason)
        {
            ReleaseActiveRuntimeNodeState(stopTimeline: true);
            _currentNode = null;
            _failureReason = reason ?? string.Empty;
            _state = ConversationSessionState.Faulted;
            NotifyChanged();
        }

        /// <summary>
        /// Completes the currently active asynchronous node and routes to its next node.
        /// </summary>
        private void CompleteAsyncNode()
        {
            if (_currentNode == null)
            {
                return;
            }

            SerializableGuid nextNodeId = _currentNode.NextNodeId;
            ReleaseActiveRuntimeNodeState(stopTimeline: false);
            _currentNode = null;
            MoveToNode(nextNodeId);
        }

        /// <summary>
        /// Creates the runtime event binding used by one wait-for-event node.
        /// </summary>
        /// <param name="node">Runtime node that should become active.</param>
        /// <returns>The created runtime execution state.</returns>
        private AsyncNodeExecution CreateWaitForEventExecution(ConversationNodeData node)
        {
            AsyncNodeExecution execution = new()
            {
                NodeId = node.NodeId,
            };

            string expectedEventName = NormalizeEventName(node.EventName);
            execution.EventBinding = new EventBinding<ConversationExternalEventRaisedEvent>(
                raisedEvent =>
                {
                    if (!string.Equals(
                            raisedEvent.EventName,
                            expectedEventName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    if (!ReferenceEquals(_activeAsyncExecution, execution))
                    {
                        return;
                    }

                    execution.CompletionRequested = true;
                });

            HandyBus<ConversationExternalEventRaisedEvent>.Register(execution.EventBinding);
            return execution;
        }

        /// <summary>
        /// Activates one play-timeline runtime node.
        /// </summary>
        /// <param name="node">Runtime node that should become active.</param>
        /// <returns>True when the timeline node was activated successfully.</returns>
        private bool TryActivatePlayTimelineNode(ConversationNodeData node)
        {
            PlayableDirector playableDirector = node.PlayableDirector;

            if (playableDirector == null)
            {
                Fail("Play Timeline node requires one valid PlayableDirector reference.");
                return false;
            }

            if (playableDirector.playableAsset == null)
            {
                Fail(
                    "Play Timeline node requires the PlayableDirector to reference a Timeline asset.");
                return false;
            }

            if (node.RestartOnEnter)
            {
                if (playableDirector.state == PlayState.Playing)
                {
                    playableDirector.Stop();
                }

                playableDirector.time = 0d;
                playableDirector.Evaluate();
            }

            AsyncNodeExecution execution = new()
            {
                NodeId = node.NodeId,
                Director = playableDirector,
                StopTimelineOnExit = node.StopOnExit,
            };

            execution.DirectorStoppedHandler = _ =>
            {
                if (!ReferenceEquals(_activeAsyncExecution, execution))
                {
                    return;
                }

                execution.CompletionRequested = true;
            };

            playableDirector.stopped += execution.DirectorStoppedHandler;

            _currentNode = node;
            _activeAsyncExecution = execution;
            _state = ConversationSessionState.Running;
            NotifyChanged();
            playableDirector.Play();
            return true;
        }

        /// <summary>
        /// Raises one named Conversations external event for the provided runtime node.
        /// </summary>
        /// <param name="node">Runtime node that should emit the event.</param>
        private void EmitExternalEvent(ConversationNodeData node)
        {
            HandyBus<ConversationExternalEventRaisedEvent>.Raise(
                new ConversationExternalEventRaisedEvent(
                    _conversation.ConversationId,
                    _conversation.Title,
                    NormalizeEventName(node.EventName)));
        }

        /// <summary>
        /// Releases any runtime bindings owned by the currently active asynchronous node.
        /// </summary>
        /// <param name="stopTimeline">Whether active timeline playback should be stopped.</param>
        private void ReleaseActiveRuntimeNodeState(bool stopTimeline)
        {
            if (_activeAsyncExecution == null)
            {
                return;
            }

            if (_activeAsyncExecution.EventBinding != null)
            {
                HandyBus<ConversationExternalEventRaisedEvent>.Deregister(
                    _activeAsyncExecution.EventBinding);
                _activeAsyncExecution.EventBinding = null;
            }

            if (_activeAsyncExecution.Director != null
                && _activeAsyncExecution.DirectorStoppedHandler != null)
            {
                _activeAsyncExecution.Director.stopped -=
                    _activeAsyncExecution.DirectorStoppedHandler;
            }

            if (stopTimeline
                && _activeAsyncExecution.StopTimelineOnExit
                && _activeAsyncExecution.Director != null
                && _activeAsyncExecution.Director.state == PlayState.Playing)
            {
                _activeAsyncExecution.Director.Stop();
            }

            _activeAsyncExecution = null;
        }

        /// <summary>
        /// Resolves one conversant payload by stable identifier.
        /// </summary>
        /// <param name="actorId">Conversant identifier to resolve.</param>
        /// <returns>The resolved conversant payload when present.</returns>
        private ConversationActorData ResolveActor(SerializableGuid actorId)
        {
            return actorId != SerializableGuid.Empty
                && _actorsById.TryGetValue(actorId, out ConversationActorData actor)
                ? actor
                : null;
        }

        /// <summary>
        /// Resolves the conversant currently assigned to one presenter side.
        /// </summary>
        /// <param name="slot">Presenter side that should be resolved.</param>
        /// <returns>The resolved conversant occupying the requested side.</returns>
        private ConversationActorData ResolveParticipantForSlot(
            ConversationParticipantSlot slot)
        {
            if (_currentNode == null || _currentNode.Kind != ConversationNodeKind.SpokenLine)
            {
                return null;
            }

            return slot == ConversationParticipantSlot.Left
                ? ResolveActorForSlot(
                    slot,
                    _currentNode.SpeakerActorId,
                    _currentNode.ResolvedSpeakerSlot,
                    _currentNode.ListenerActorId,
                    _currentNode.ResolvedListenerSlot)
                : ResolveActorForSlot(
                    slot,
                    _currentNode.ListenerActorId,
                    _currentNode.ResolvedListenerSlot,
                    _currentNode.SpeakerActorId,
                    _currentNode.ResolvedSpeakerSlot);
        }

        /// <summary>
        /// Resolves one participant for the requested side with deterministic role precedence.
        /// </summary>
        /// <param name="slot">Presenter side that should be resolved.</param>
        /// <param name="primaryActorId">Role-preferred actor for the requested side.</param>
        /// <param name="primarySlot">Resolved slot owned by the primary actor.</param>
        /// <param name="secondaryActorId">Secondary actor that may also target the same side.</param>
        /// <param name="secondarySlot">Resolved slot owned by the secondary actor.</param>
        /// <returns>The resolved actor occupying the requested side.</returns>
        private ConversationActorData ResolveActorForSlot(
            ConversationParticipantSlot slot,
            SerializableGuid primaryActorId,
            ConversationParticipantSlot primarySlot,
            SerializableGuid secondaryActorId,
            ConversationParticipantSlot secondarySlot)
        {
            if (primarySlot == slot)
            {
                ConversationActorData primaryActor = ResolveActor(primaryActorId);

                if (primaryActor != null)
                {
                    return primaryActor;
                }
            }

            return secondarySlot == slot
                ? ResolveActor(secondaryActorId)
                : null;
        }

        /// <summary>
        /// Resolves one exported string source against the runtime blackboard when needed.
        /// </summary>
        /// <param name="valueData">String source to resolve.</param>
        /// <param name="textId">Authored text id that may map to one localized override.</param>
        /// <returns>The resolved text.</returns>
        private string ResolveStringValue(ConversationStringValueData valueData, string textId)
        {
            if (valueData == null)
            {
                return string.Empty;
            }

            if (valueData.Mode == GraphValueSourceMode.Direct
                && !string.IsNullOrWhiteSpace(textId)
                && _localizedTextsById != null
                && _localizedTextsById.TryGetValue(textId, out string localizedText)
                && !string.IsNullOrWhiteSpace(localizedText))
            {
                return localizedText;
            }

            if (valueData.Mode == GraphValueSourceMode.Blackboard
                && valueData.BlackboardVariable != null
                && valueData.BlackboardVariable.TryGetValue(
                    _runtimeBlackboard,
                    _scopeResolver,
                    out string resolvedValue))
            {
                return resolvedValue ?? string.Empty;
            }

            return valueData.DirectValue;
        }

        /// <summary>
        /// Gets whether the provided runtime kind represents one presentable text line.
        /// </summary>
        /// <param name="kind">Runtime kind that should be inspected.</param>
        /// <returns>True when the kind maps to one active line presentation.</returns>
        private static bool IsPresentableLineKind(ConversationNodeKind kind)
        {
            return kind == ConversationNodeKind.SpokenLine
                || kind == ConversationNodeKind.NarrationLine;
        }

        /// <summary>
        /// Gets whether the provided runtime kind represents one asynchronous utility node.
        /// </summary>
        /// <param name="kind">Runtime kind that should be inspected.</param>
        /// <returns>True when the kind depends on runtime callbacks or per-frame ticks.</returns>
        private static bool IsAsyncNodeKind(ConversationNodeKind kind)
        {
            return kind == ConversationNodeKind.Wait
                || kind == ConversationNodeKind.WaitForEvent
                || kind == ConversationNodeKind.PlayTimeline;
        }

        /// <summary>
        /// Normalizes one named external event used by Conversations utility nodes.
        /// </summary>
        /// <param name="eventName">Authored event name.</param>
        /// <returns>The normalized event name.</returns>
        private static string NormalizeEventName(string eventName)
        {
            return string.IsNullOrWhiteSpace(eventName)
                ? "conversation.event"
                : eventName;
        }

        /// <summary>
        /// Stores the runtime state owned by one active asynchronous utility node.
        /// </summary>
        private sealed class AsyncNodeExecution
        {
            public SerializableGuid NodeId;
            public float ElapsedSeconds;
            public float WaitDurationSeconds;
            public ConversationTimeMode TimeMode;
            public bool CompletionRequested;
            public EventBinding<ConversationExternalEventRaisedEvent> EventBinding;
            public PlayableDirector Director;
            public Action<PlayableDirector> DirectorStoppedHandler;
            public bool StopTimelineOnExit;
        }

        /// <summary>
        /// Raises the session-changed event.
        /// </summary>
        private void NotifyChanged()
        {
            Changed?.Invoke(this);
        }

        #endregion
    }
}