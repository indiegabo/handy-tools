using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.ConversationsModule.Cache
{
    /// <summary>
    /// Stores runtime conversation payloads for reuse across repeated load requests.
    /// </summary>
    public interface IConversationCache
    {
        /// <summary>
        /// Attempts to retrieve one cached payload without changing its reference count.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="conversationData">Resolved payload when present.</param>
        /// <returns>True when the cache contains the requested payload.</returns>
        bool TryGet(SerializableGuid conversationId, out ConversationData conversationData);

        /// <summary>
        /// Attempts to retrieve one cached payload and increments its active reference count.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="conversationData">Resolved payload when present.</param>
        /// <returns>True when the cache contains the requested payload.</returns>
        bool TryAcquire(SerializableGuid conversationId, out ConversationData conversationData);

        /// <summary>
        /// Stores one runtime payload and optionally counts the store as an active reference.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="conversationData">Runtime payload to store.</param>
        /// <param name="acquireReference">Whether the stored payload should start with one active reference.</param>
        void Store(
            SerializableGuid conversationId,
            ConversationData conversationData,
            bool acquireReference);

        /// <summary>
        /// Releases one active reference held for the requested conversation.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        void Release(SerializableGuid conversationId);

        /// <summary>
        /// Clears all cached payloads.
        /// </summary>
        void Clear();
    }
}