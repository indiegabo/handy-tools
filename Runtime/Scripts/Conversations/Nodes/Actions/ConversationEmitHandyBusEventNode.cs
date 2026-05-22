using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.Nodes.Actions
{
    /// <summary>
    /// Stores one authored named HandyBus event emission inside a Conversations graph.
    /// </summary>
    [System.Serializable]
    [ConversationNodeMenu("Actions/Emit HandyBus Event", "Emit HandyBus Event")]
    public sealed class ConversationEmitHandyBusEventNode : ConversationActionNodeBase
    {
        private const string DefaultEventName = "conversation.event";

        [SerializeField]
        private string _eventName = DefaultEventName;

        /// <summary>
        /// Gets the authored event name emitted by the node.
        /// </summary>
        public string EventName => string.IsNullOrWhiteSpace(_eventName)
            ? DefaultEventName
            : _eventName;

        /// <summary>
        /// Configures the node to emit one named external event.
        /// </summary>
        /// <param name="eventName">Named event that should be raised.</param>
        public void Configure(string eventName)
        {
            _eventName = string.IsNullOrWhiteSpace(eventName)
                ? DefaultEventName
                : eventName;
        }

        /// <summary>
        /// Gets one short node summary for authoring surfaces.
        /// </summary>
        /// <returns>The current authored event-name summary.</returns>
        public override string GetSummary()
        {
            return EventName;
        }
    }
}