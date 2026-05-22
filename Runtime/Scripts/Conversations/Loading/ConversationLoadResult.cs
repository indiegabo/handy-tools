using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.ConversationsModule.Loading
{
    /// <summary>
    /// Stores the outcome of one runtime conversation load operation.
    /// </summary>
    public sealed class ConversationLoadResult
    {
        #region Constructor

        /// <summary>
        /// Initializes one load result.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="conversationData">Loaded runtime payload when the request succeeded.</param>
        /// <param name="catalogEntry">Catalog entry used to resolve the payload.</param>
        /// <param name="succeeded">Whether the request succeeded.</param>
        /// <param name="loadedFromCache">Whether the payload came from cache.</param>
        /// <param name="failureReason">Human-readable failure reason.</param>
        public ConversationLoadResult(
            SerializableGuid conversationId,
            ConversationData conversationData,
            ConversationRuntimeCatalog.Entry catalogEntry,
            bool succeeded,
            bool loadedFromCache,
            string failureReason)
        {
            ConversationId = conversationId;
            ConversationData = conversationData;
            CatalogEntry = catalogEntry;
            Succeeded = succeeded;
            LoadedFromCache = loadedFromCache;
            FailureReason = failureReason ?? string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the stable conversation identifier requested by the caller.
        /// </summary>
        public SerializableGuid ConversationId { get; }

        /// <summary>
        /// Gets the loaded runtime payload when available.
        /// </summary>
        public ConversationData ConversationData { get; }

        /// <summary>
        /// Gets the catalog entry used to resolve the payload when available.
        /// </summary>
        public ConversationRuntimeCatalog.Entry CatalogEntry { get; }

        /// <summary>
        /// Gets whether the request succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets whether the payload was resolved from the cache.
        /// </summary>
        public bool LoadedFromCache { get; }

        /// <summary>
        /// Gets the failure reason when the request did not succeed.
        /// </summary>
        public string FailureReason { get; }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates one successful load result.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="conversationData">Loaded runtime payload.</param>
        /// <param name="catalogEntry">Catalog entry used to resolve the payload.</param>
        /// <param name="loadedFromCache">Whether the payload came from cache.</param>
        /// <returns>The successful load result.</returns>
        public static ConversationLoadResult Success(
            SerializableGuid conversationId,
            ConversationData conversationData,
            ConversationRuntimeCatalog.Entry catalogEntry,
            bool loadedFromCache)
        {
            return new ConversationLoadResult(
                conversationId,
                conversationData,
                catalogEntry,
                succeeded: true,
                loadedFromCache,
                string.Empty);
        }

        /// <summary>
        /// Creates one failed load result.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="failureReason">Human-readable failure reason.</param>
        /// <returns>The failed load result.</returns>
        public static ConversationLoadResult Failure(
            SerializableGuid conversationId,
            string failureReason)
        {
            return new ConversationLoadResult(
                conversationId,
                null,
                null,
                succeeded: false,
                loadedFromCache: false,
                failureReason);
        }

        #endregion
    }
}