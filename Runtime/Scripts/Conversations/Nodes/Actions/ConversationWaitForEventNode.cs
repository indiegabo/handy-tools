using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.Nodes.Actions
{
    /// <summary>
    /// Stores one authored named-event wait action inside a Conversations graph.
    /// </summary>
    [System.Serializable]
    [ConversationNodeMenu("Actions/Wait For Event", "Wait For Event")]
    public sealed class ConversationWaitForEventNode : ConversationActionNodeBase
    {
        private const string DefaultEventName = "conversation.event";

        [SerializeField]
        private string _eventName = DefaultEventName;

        /// <summary>
        /// Gets the authored event name awaited by the node.
        /// </summary>
        public string EventName => string.IsNullOrWhiteSpace(_eventName)
            ? DefaultEventName
            : _eventName;

        /// <summary>
        /// Configures the node to wait for one named external event.
        /// </summary>
        /// <param name="eventName">Named event that should unblock the node.</param>
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