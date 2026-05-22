using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.ConversationsModule.Events
{
    /// <summary>
    /// Raised when one Conversations utility node emits a named external event.
    /// </summary>
    public readonly struct ConversationExternalEventRaisedEvent : IEvent
    {
        /// <summary>
        /// Initializes one named Conversations external event payload.
        /// </summary>
        /// <param name="conversationId">Stable identifier of the conversation that raised the event.</param>
        /// <param name="conversationTitle">Title of the conversation that raised the event.</param>
        /// <param name="eventName">Raised event name.</param>
        public ConversationExternalEventRaisedEvent(
            SerializableGuid conversationId,
            string conversationTitle,
            string eventName)
        {
            ConversationId = conversationId;
            ConversationTitle = conversationTitle ?? string.Empty;
            EventName = eventName ?? string.Empty;
        }

        /// <summary>
        /// Gets the conversation identifier that raised the event.
        /// </summary>
        public SerializableGuid ConversationId { get; }

        /// <summary>
        /// Gets the conversation title that raised the event.
        /// </summary>
        public string ConversationTitle { get; }

        /// <summary>
        /// Gets the raised event name.
        /// </summary>
        public string EventName { get; }
    }
}