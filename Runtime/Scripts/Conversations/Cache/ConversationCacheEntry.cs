using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.ConversationsModule.Cache
{
    /// <summary>
    /// Stores one cached conversation payload together with lightweight reuse metadata.
    /// </summary>
    public sealed class ConversationCacheEntry
    {
        #region Constructor

        /// <summary>
        /// Initializes one cache entry.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="conversationData">Runtime payload stored by the cache.</param>
        public ConversationCacheEntry(
            SerializableGuid conversationId,
            ConversationData conversationData)
        {
            ConversationId = conversationId;
            ConversationData = conversationData;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the stable conversation identifier represented by the entry.
        /// </summary>
        public SerializableGuid ConversationId { get; }

        /// <summary>
        /// Gets the cached runtime payload.
        /// </summary>
        public ConversationData ConversationData { get; private set; }

        /// <summary>
        /// Gets the current number of active references held for the payload.
        /// </summary>
        public int ReferenceCount { get; private set; }

        /// <summary>
        /// Gets the monotonically increasing access sequence used for LRU eviction.
        /// </summary>
        public long LastAccessSequence { get; private set; }

        #endregion

        #region Public API

        /// <summary>
        /// Replaces the cached runtime payload.
        /// </summary>
        /// <param name="conversationData">Updated runtime payload.</param>
        /// <param name="accessSequence">Monotonic sequence used to update recency.</param>
        public void Replace(ConversationData conversationData, long accessSequence)
        {
            ConversationData = conversationData;
            Touch(accessSequence);
        }

        /// <summary>
        /// Records one non-owning cache access.
        /// </summary>
        /// <param name="accessSequence">Monotonic sequence used to update recency.</param>
        public void Touch(long accessSequence)
        {
            LastAccessSequence = accessSequence;
        }

        /// <summary>
        /// Records one owning cache access.
        /// </summary>
        /// <param name="accessSequence">Monotonic sequence used to update recency.</param>
        public void Acquire(long accessSequence)
        {
            ReferenceCount++;
            LastAccessSequence = accessSequence;
        }

        /// <summary>
        /// Releases one active reference when present.
        /// </summary>
        /// <param name="accessSequence">Monotonic sequence used to update recency.</param>
        public void Release(long accessSequence)
        {
            if (ReferenceCount > 0)
            {
                ReferenceCount--;
            }

            LastAccessSequence = accessSequence;
        }

        #endregion
    }
}