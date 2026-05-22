using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.RuntimeData
{
    /// <summary>
    /// Stores one exported runtime payload for one authored conversation.
    /// </summary>
    [Serializable]
    public sealed class ConversationData
    {
        [SerializeField]
        private SerializableGuid _conversationId;

        [SerializeField]
        private string _title = string.Empty;

        [SerializeField]
        private SerializableGuid _entryNodeId;

        [SerializeField]
        private List<ConversationNodeData> _nodes = new();

        [SerializeField]
        private List<ConversationChoiceData> _choices = new();

        [SerializeField]
        private List<ConversationActorData> _actors = new();

        /// <summary>
        /// Initializes an empty conversation payload.
        /// </summary>
        public ConversationData()
        {
        }

        /// <summary>
        /// Initializes one exported runtime payload.
        /// </summary>
        /// <param name="conversationId">Stable authored conversation identifier.</param>
        /// <param name="title">Authored title retained for runtime diagnostics and menus.</param>
        /// <param name="entryNodeId">Stable entry node identifier for playback startup.</param>
        /// <param name="nodes">Exported runtime nodes.</param>
        /// <param name="choices">Exported runtime choice records.</param>
        /// <param name="actors">Exported conversant records.</param>
        public ConversationData(
            SerializableGuid conversationId,
            string title,
            SerializableGuid entryNodeId,
            IEnumerable<ConversationNodeData> nodes,
            IEnumerable<ConversationChoiceData> choices,
            IEnumerable<ConversationActorData> actors)
        {
            _conversationId = conversationId;
            _title = title ?? string.Empty;
            _entryNodeId = entryNodeId;
            _nodes = nodes == null
                ? new List<ConversationNodeData>()
                : new List<ConversationNodeData>(nodes);
            _choices = choices == null
                ? new List<ConversationChoiceData>()
                : new List<ConversationChoiceData>(choices);
            _actors = actors == null
                ? new List<ConversationActorData>()
                : new List<ConversationActorData>(actors);
        }

        /// <summary>
        /// Gets the stable identifier of the exported conversation.
        /// </summary>
        public SerializableGuid ConversationId => _conversationId;

        /// <summary>
        /// Gets the authored title retained for runtime-facing labels and diagnostics.
        /// </summary>
        public string Title => _title ?? string.Empty;

        /// <summary>
        /// Gets the stable entry node identifier used to begin playback.
        /// </summary>
        public SerializableGuid EntryNodeId => _entryNodeId;

        /// <summary>
        /// Gets the exported runtime nodes for the conversation.
        /// </summary>
        public IReadOnlyList<ConversationNodeData> Nodes => _nodes;

        /// <summary>
        /// Gets the exported runtime choices for the conversation.
        /// </summary>
        public IReadOnlyList<ConversationChoiceData> Choices => _choices;

        /// <summary>
        /// Gets the exported conversant records for the conversation.
        /// </summary>
        public IReadOnlyList<ConversationActorData> Actors => _actors;
    }
}