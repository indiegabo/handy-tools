using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule;
using IndieGabo.HandyTools.Utils;
using UnityEngine;
using UnityEngine.Playables;

namespace IndieGabo.HandyTools.ConversationsModule.RuntimeData
{
    /// <summary>
    /// Identifies the runtime node kinds currently supported by the exported Conversations payload.
    /// </summary>
    public enum ConversationNodeKind
    {
        /// <summary>
        /// Entry node that defines the initial route of one conversation.
        /// </summary>
        Entry,

        /// <summary>
        /// Spoken-line node that emits authored text tied to one conversant presentation.
        /// </summary>
        SpokenLine,

        /// <summary>
        /// Narration-line node that emits authored text without speaker presentation.
        /// </summary>
        NarrationLine,

        /// <summary>
        /// Choice node that would fan out through one or more exported choice ids.
        /// </summary>
        Choice,

        /// <summary>
        /// Branch node that would evaluate one deterministic condition set.
        /// </summary>
        Branch,

        /// <summary>
        /// Blackboard-mutation node that would write one exported value payload.
        /// </summary>
        SetBlackboard,

        /// <summary>
        /// Wait node that advances after one authored duration elapses.
        /// </summary>
        Wait,

        /// <summary>
        /// Named-event emission node that raises one HandyBus event immediately.
        /// </summary>
        EmitHandyBusEvent,

        /// <summary>
        /// Named-event wait node that advances after one matching event arrives.
        /// </summary>
        WaitForEvent,

        /// <summary>
        /// Timeline playback node that advances after one PlayableDirector stops.
        /// </summary>
        PlayTimeline,
    }

    /// <summary>
    /// Stores one exported node record inside one runtime conversation payload.
    /// </summary>
    [Serializable]
    public sealed class ConversationNodeData
    {
        [SerializeField]
        private SerializableGuid _nodeId;

        [SerializeField]
        private ConversationNodeKind _kind;

        [SerializeField]
        private SerializableGuid _nextNodeId;

        [SerializeField]
        private SerializableGuid _speakerActorId;

        [SerializeField]
        private SerializableGuid _listenerActorId;

        [SerializeField]
        private ConversationParticipantSlot _speakerSlot = ConversationParticipantSlot.Auto;

        [SerializeField]
        private ConversationParticipantSlot _listenerSlot = ConversationParticipantSlot.Auto;

        [SerializeField]
        private ConversationStringValueData _lineText;

        [SerializeField]
        private string _textId = string.Empty;

        [SerializeField]
        private List<SerializableGuid> _choiceIds = new();

        [SerializeField]
        private float _waitDurationSeconds;

        [SerializeField]
        private ConversationTimeMode _timeMode = ConversationTimeMode.Scaled;

        [SerializeField]
        private string _eventName = string.Empty;

        [SerializeField]
        private PlayableDirector _playableDirector;

        [SerializeField]
        private bool _restartOnEnter = true;

        [SerializeField]
        private bool _stopOnExit = true;

        /// <summary>
        /// Initializes an empty node DTO.
        /// </summary>
        public ConversationNodeData()
        {
        }

        /// <summary>
        /// Initializes one exported node DTO.
        /// </summary>
        /// <param name="nodeId">Stable runtime node identifier.</param>
        /// <param name="kind">Runtime node kind represented by the export.</param>
        /// <param name="nextNodeId">Direct downstream node id when the node has one linear route.</param>
        /// <param name="speakerActorId">Optional speaking conversant identifier for spoken-line nodes.</param>
        /// <param name="listenerActorId">Optional listening conversant identifier for spoken-line nodes.</param>
        /// <param name="speakerSlot">Optional presenter-slot selection for the speaking conversant.</param>
        /// <param name="listenerSlot">Optional presenter-slot selection for the listening conversant.</param>
        /// <param name="lineText">Optional text payload for spoken-line or narration-line nodes.</param>
        /// <param name="textId">Stable identifier for authored text payloads when the node owns one.</param>
        /// <param name="choiceIds">Optional choice ids owned by this node.</param>
        /// <param name="waitDurationSeconds">Optional wait duration in seconds for wait nodes.</param>
        /// <param name="timeMode">Optional time source used by wait nodes.</param>
        /// <param name="eventName">Optional named event emitted or awaited by event nodes.</param>
        /// <param name="playableDirector">Optional timeline director used by play-timeline nodes.</param>
        /// <param name="restartOnEnter">Optional flag that restarts the timeline on enter.</param>
        /// <param name="stopOnExit">Optional flag that stops the timeline on early exit.</param>
        public ConversationNodeData(
            SerializableGuid nodeId,
            ConversationNodeKind kind,
            SerializableGuid nextNodeId,
            SerializableGuid speakerActorId = default,
            SerializableGuid listenerActorId = default,
            ConversationParticipantSlot speakerSlot = ConversationParticipantSlot.Auto,
            ConversationParticipantSlot listenerSlot = ConversationParticipantSlot.Auto,
            ConversationStringValueData lineText = null,
            string textId = null,
            IEnumerable<SerializableGuid> choiceIds = null,
            float waitDurationSeconds = 0f,
            ConversationTimeMode timeMode = ConversationTimeMode.Scaled,
            string eventName = null,
            PlayableDirector playableDirector = null,
            bool restartOnEnter = true,
            bool stopOnExit = true)
        {
            _nodeId = nodeId;
            _kind = kind;
            _nextNodeId = nextNodeId;
            _speakerActorId = speakerActorId;
            _listenerActorId = listenerActorId;
            _speakerSlot = speakerSlot;
            _listenerSlot = listenerSlot;
            _lineText = lineText;
            _textId = textId ?? string.Empty;
            _choiceIds = choiceIds == null
                ? new List<SerializableGuid>()
                : new List<SerializableGuid>(choiceIds);
            _waitDurationSeconds = waitDurationSeconds;
            _timeMode = timeMode;
            _eventName = eventName ?? string.Empty;
            _playableDirector = playableDirector;
            _restartOnEnter = restartOnEnter;
            _stopOnExit = stopOnExit;
        }

        /// <summary>
        /// Gets the stable identifier of the exported node.
        /// </summary>
        public SerializableGuid NodeId => _nodeId;

        /// <summary>
        /// Gets the runtime node kind represented by the export.
        /// </summary>
        public ConversationNodeKind Kind => _kind;

        /// <summary>
        /// Gets the single downstream node id for linear node kinds.
        /// </summary>
        public SerializableGuid NextNodeId => _nextNodeId;

        /// <summary>
        /// Gets the speaking conversant identifier when the node carries one speaker binding.
        /// </summary>
        public SerializableGuid SpeakerActorId => _speakerActorId;

        /// <summary>
        /// Gets the listening conversant identifier when the node carries one listener binding.
        /// </summary>
        public SerializableGuid ListenerActorId => _listenerActorId;

        /// <summary>
        /// Gets the authored presenter-slot selection for the speaking conversant.
        /// </summary>
        public ConversationParticipantSlot SpeakerSlot => _speakerSlot;

        /// <summary>
        /// Gets the authored presenter-slot selection for the listening conversant.
        /// </summary>
        public ConversationParticipantSlot ListenerSlot => _listenerSlot;

        /// <summary>
        /// Gets the resolved presenter slot for the speaking conversant.
        /// </summary>
        public ConversationParticipantSlot ResolvedSpeakerSlot =>
            ConversationParticipantSlotUtility.ResolveSlot(
                _speakerSlot,
                ConversationParticipantSlot.Left);

        /// <summary>
        /// Gets the resolved presenter slot for the listening conversant.
        /// </summary>
        public ConversationParticipantSlot ResolvedListenerSlot =>
            ConversationParticipantSlotUtility.ResolveSlot(
                _listenerSlot,
                ConversationParticipantSlot.Right);

        /// <summary>
        /// Gets the exported text payload when the node carries spoken or narration content.
        /// </summary>
        public ConversationStringValueData LineText => _lineText;

        /// <summary>
        /// Gets the stable identifier for authored text payloads when the node owns one.
        /// </summary>
        public string TextId => _textId ?? string.Empty;

        /// <summary>
        /// Gets the exported choice ids owned by the node.
        /// </summary>
        public IReadOnlyList<SerializableGuid> ChoiceIds => _choiceIds;

        /// <summary>
        /// Gets the exported wait duration in seconds.
        /// </summary>
        public float WaitDurationSeconds => _waitDurationSeconds;

        /// <summary>
        /// Gets the exported time source used by wait nodes.
        /// </summary>
        public ConversationTimeMode TimeMode => _timeMode;

        /// <summary>
        /// Gets the exported named event emitted or awaited by event nodes.
        /// </summary>
        public string EventName => _eventName ?? string.Empty;

        /// <summary>
        /// Gets the exported playable director used by play-timeline nodes.
        /// </summary>
        public PlayableDirector PlayableDirector => _playableDirector;

        /// <summary>
        /// Gets whether play-timeline nodes restart playback from time zero on enter.
        /// </summary>
        public bool RestartOnEnter => _restartOnEnter;

        /// <summary>
        /// Gets whether play-timeline nodes stop playback when exiting early.
        /// </summary>
        public bool StopOnExit => _stopOnExit;
    }
}