using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.ConversationsModule.Loading
{
    /// <summary>
    /// Describes one runtime conversation load request.
    /// </summary>
    public readonly struct ConversationLoadRequest
    {
        #region Constructor

        /// <summary>
        /// Initializes one runtime load request.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="acquireReference">Whether the resulting payload should hold one active cache reference.</param>
        public ConversationLoadRequest(
            SerializableGuid conversationId,
            bool acquireReference)
        {
            ConversationId = conversationId;
            AcquireReference = acquireReference;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the stable conversation identifier.
        /// </summary>
        public SerializableGuid ConversationId { get; }

        /// <summary>
        /// Gets whether the caller expects one active cache reference on success.
        /// </summary>
        public bool AcquireReference { get; }

        #endregion
    }
}