using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.ConversationsModule.Cache
{
    /// <summary>
    /// Stores runtime conversation payloads using a small reference-aware LRU policy.
    /// </summary>
    public sealed class ConversationCacheLRU : IConversationCache
    {
        #region Fields

        private readonly Dictionary<SerializableGuid, ConversationCacheEntry> _entries = new();

        private readonly int _capacity;

        private long _accessSequence;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes one runtime conversation cache.
        /// </summary>
        /// <param name="capacity">Maximum number of idle payloads the cache should keep.</param>
        public ConversationCacheLRU(int capacity = 64)
        {
            _capacity = System.Math.Max(1, capacity);
        }

        #endregion

        #region Public API

        /// <inheritdoc />
        public bool TryGet(SerializableGuid conversationId, out ConversationData conversationData)
        {
            conversationData = null;

            if (!_entries.TryGetValue(conversationId, out ConversationCacheEntry entry)
                || entry.ConversationData == null)
            {
                return false;
            }

            entry.Touch(NextAccessSequence());
            conversationData = entry.ConversationData;
            return true;
        }

        /// <inheritdoc />
        public bool TryAcquire(SerializableGuid conversationId, out ConversationData conversationData)
        {
            conversationData = null;

            if (!_entries.TryGetValue(conversationId, out ConversationCacheEntry entry)
                || entry.ConversationData == null)
            {
                return false;
            }

            entry.Acquire(NextAccessSequence());
            conversationData = entry.ConversationData;
            return true;
        }

        /// <inheritdoc />
        public void Store(
            SerializableGuid conversationId,
            ConversationData conversationData,
            bool acquireReference)
        {
            if (conversationId == SerializableGuid.Empty || conversationData == null)
            {
                return;
            }

            long accessSequence = NextAccessSequence();

            if (!_entries.TryGetValue(conversationId, out ConversationCacheEntry entry))
            {
                entry = new ConversationCacheEntry(conversationId, conversationData);
                _entries.Add(conversationId, entry);
            }
            else
            {
                entry.Replace(conversationData, accessSequence);
            }

            if (acquireReference)
            {
                entry.Acquire(accessSequence);
            }
            else
            {
                entry.Touch(accessSequence);
            }

            EvictIdleEntriesIfNeeded();
        }

        /// <inheritdoc />
        public void Release(SerializableGuid conversationId)
        {
            if (!_entries.TryGetValue(conversationId, out ConversationCacheEntry entry))
            {
                return;
            }

            entry.Release(NextAccessSequence());
            EvictIdleEntriesIfNeeded();
        }

        /// <inheritdoc />
        public void Clear()
        {
            _entries.Clear();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets the next monotonic access sequence used by the cache.
        /// </summary>
        /// <returns>The next access sequence value.</returns>
        private long NextAccessSequence()
        {
            return ++_accessSequence;
        }

        /// <summary>
        /// Removes least-recently used idle entries while the cache exceeds capacity.
        /// </summary>
        private void EvictIdleEntriesIfNeeded()
        {
            while (_entries.Count > _capacity)
            {
                ConversationCacheEntry evictionCandidate = null;

                foreach (ConversationCacheEntry entry in _entries.Values)
                {
                    if (entry.ReferenceCount > 0)
                    {
                        continue;
                    }

                    if (evictionCandidate == null
                        || entry.LastAccessSequence < evictionCandidate.LastAccessSequence)
                    {
                        evictionCandidate = entry;
                    }
                }

                if (evictionCandidate == null)
                {
                    return;
                }

                _entries.Remove(evictionCandidate.ConversationId);
            }
        }

        #endregion
    }
}