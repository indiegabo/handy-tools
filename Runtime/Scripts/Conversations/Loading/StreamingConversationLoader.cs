using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndieGabo.HandyTools.ConversationsModule.Cache;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.Loading
{
    /// <summary>
    /// Loads Conversations runtime payloads from StreamingAssets-compatible JSON exports.
    /// </summary>
    public sealed class StreamingConversationLoader : IConversationLoader
    {
        #region Fields

        private readonly IConversationCatalogProvider _catalogProvider;

        private readonly IConversationCache _cache;

        private readonly Dictionary<SerializableGuid, Task<ConversationLoadResult>> _inFlightLoads =
            new();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes one streaming conversation loader.
        /// </summary>
        /// <param name="catalogProvider">Catalog provider used to resolve payload paths.</param>
        /// <param name="cache">Runtime cache used to retain loaded payloads.</param>
        public StreamingConversationLoader(
            IConversationCatalogProvider catalogProvider,
            IConversationCache cache)
        {
            _catalogProvider = catalogProvider ?? throw new ArgumentNullException(nameof(catalogProvider));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        #endregion

        #region Public API

        /// <inheritdoc />
        public Task<ConversationLoadResult> LoadAsync(
            SerializableGuid conversationId,
            CancellationToken cancellationToken = default)
        {
            return LoadInternalAsync(
                new ConversationLoadRequest(conversationId, acquireReference: true),
                cancellationToken);
        }

        /// <inheritdoc />
        public Task<ConversationLoadResult> PreloadAsync(
            SerializableGuid conversationId,
            CancellationToken cancellationToken = default)
        {
            return LoadInternalAsync(
                new ConversationLoadRequest(conversationId, acquireReference: false),
                cancellationToken);
        }

        /// <inheritdoc />
        public void Release(SerializableGuid conversationId)
        {
            _cache.Release(conversationId);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Loads one conversation payload according to the request options.
        /// </summary>
        /// <param name="request">Load request options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The load result.</returns>
        private async Task<ConversationLoadResult> LoadInternalAsync(
            ConversationLoadRequest request,
            CancellationToken cancellationToken)
        {
            if (request.ConversationId == SerializableGuid.Empty)
            {
                return ConversationLoadResult.Failure(
                    request.ConversationId,
                    "Conversation id cannot be empty.");
            }

            bool cacheHit = request.AcquireReference
                ? _cache.TryAcquire(request.ConversationId, out ConversationData cachedData)
                : _cache.TryGet(request.ConversationId, out cachedData);

            if (cacheHit)
            {
                return ConversationLoadResult.Success(
                    request.ConversationId,
                    cachedData,
                    null,
                    loadedFromCache: true);
            }

            bool joinedExistingLoad = true;
            Task<ConversationLoadResult> loadTask;

            lock (_inFlightLoads)
            {
                if (!_inFlightLoads.TryGetValue(request.ConversationId, out loadTask))
                {
                    joinedExistingLoad = false;
                    loadTask = LoadFromSourceAsync(request, cancellationToken);
                    _inFlightLoads.Add(request.ConversationId, loadTask);
                }
            }

            try
            {
                ConversationLoadResult result = await loadTask;

                if (joinedExistingLoad && result.Succeeded && request.AcquireReference)
                {
                    _cache.TryAcquire(request.ConversationId, out _);
                }

                return result;
            }
            finally
            {
                if (!joinedExistingLoad)
                {
                    lock (_inFlightLoads)
                    {
                        _inFlightLoads.Remove(request.ConversationId);
                    }
                }
            }
        }

        /// <summary>
        /// Loads one conversation payload from exported JSON files.
        /// </summary>
        /// <param name="request">Load request options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The load result.</returns>
        private async Task<ConversationLoadResult> LoadFromSourceAsync(
            ConversationLoadRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                ConversationRuntimeCatalog catalog = await _catalogProvider.LoadCatalogAsync(
                    cancellationToken);
                ConversationRuntimeCatalog.Entry catalogEntry =
                    ConversationCatalogLookup.FindCatalogEntry(
                    catalog,
                    request.ConversationId);

                if (catalogEntry == null)
                {
                    return ConversationLoadResult.Failure(
                        request.ConversationId,
                        $"Conversation '{request.ConversationId}' is missing from the runtime catalog.");
                }

                string payloadJson = await ConversationStreamingJsonIO.ReadTextAsync(
                    _catalogProvider.ResolvePayloadPath(catalogEntry),
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(payloadJson))
                {
                    return ConversationLoadResult.Failure(
                        request.ConversationId,
                        $"Conversation payload '{catalogEntry.PayloadPath}' is empty.");
                }

                ConversationData conversationData = JsonUtility.FromJson<ConversationData>(
                    payloadJson);

                if (conversationData == null)
                {
                    return ConversationLoadResult.Failure(
                        request.ConversationId,
                        $"Conversation payload '{catalogEntry.PayloadPath}' could not be deserialized.");
                }

                _cache.Store(
                    request.ConversationId,
                    conversationData,
                    request.AcquireReference);

                return ConversationLoadResult.Success(
                    request.ConversationId,
                    conversationData,
                    catalogEntry,
                    loadedFromCache: false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return ConversationLoadResult.Failure(
                    request.ConversationId,
                    exception.Message);
            }
        }

        #endregion
    }

    /// <summary>
    /// Loads Conversations runtime payloads from Addressables-managed JSON text assets.
    /// </summary>
    internal sealed class AddressableConversationLoader : IConversationLoader
    {
        #region Fields

        private readonly IConversationCatalogProvider _catalogProvider;

        private readonly IConversationCache _cache;

        private readonly Dictionary<SerializableGuid, Task<ConversationLoadResult>> _inFlightLoads =
            new();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes one Addressables-backed conversation loader.
        /// </summary>
        /// <param name="catalogProvider">Catalog provider used to resolve payload keys.</param>
        /// <param name="cache">Runtime cache used to retain loaded payloads.</param>
        public AddressableConversationLoader(
            IConversationCatalogProvider catalogProvider,
            IConversationCache cache)
        {
            _catalogProvider = catalogProvider ?? throw new ArgumentNullException(nameof(catalogProvider));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        #endregion

        #region Public API

        /// <inheritdoc />
        public Task<ConversationLoadResult> LoadAsync(
            SerializableGuid conversationId,
            CancellationToken cancellationToken = default)
        {
            return LoadInternalAsync(
                new ConversationLoadRequest(conversationId, acquireReference: true),
                cancellationToken);
        }

        /// <inheritdoc />
        public Task<ConversationLoadResult> PreloadAsync(
            SerializableGuid conversationId,
            CancellationToken cancellationToken = default)
        {
            return LoadInternalAsync(
                new ConversationLoadRequest(conversationId, acquireReference: false),
                cancellationToken);
        }

        /// <inheritdoc />
        public void Release(SerializableGuid conversationId)
        {
            _cache.Release(conversationId);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Loads one conversation payload according to the request options.
        /// </summary>
        /// <param name="request">Load request options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The load result.</returns>
        private async Task<ConversationLoadResult> LoadInternalAsync(
            ConversationLoadRequest request,
            CancellationToken cancellationToken)
        {
            if (request.ConversationId == SerializableGuid.Empty)
            {
                return ConversationLoadResult.Failure(
                    request.ConversationId,
                    "Conversation id cannot be empty.");
            }

            bool cacheHit = request.AcquireReference
                ? _cache.TryAcquire(request.ConversationId, out ConversationData cachedData)
                : _cache.TryGet(request.ConversationId, out cachedData);

            if (cacheHit)
            {
                return ConversationLoadResult.Success(
                    request.ConversationId,
                    cachedData,
                    null,
                    loadedFromCache: true);
            }

            bool joinedExistingLoad = true;
            Task<ConversationLoadResult> loadTask;

            lock (_inFlightLoads)
            {
                if (!_inFlightLoads.TryGetValue(request.ConversationId, out loadTask))
                {
                    joinedExistingLoad = false;
                    loadTask = LoadFromSourceAsync(request, cancellationToken);
                    _inFlightLoads.Add(request.ConversationId, loadTask);
                }
            }

            try
            {
                ConversationLoadResult result = await loadTask;

                if (joinedExistingLoad && result.Succeeded && request.AcquireReference)
                {
                    _cache.TryAcquire(request.ConversationId, out _);
                }

                return result;
            }
            finally
            {
                if (!joinedExistingLoad)
                {
                    lock (_inFlightLoads)
                    {
                        _inFlightLoads.Remove(request.ConversationId);
                    }
                }
            }
        }

        /// <summary>
        /// Loads one conversation payload from Addressables-managed text assets.
        /// </summary>
        /// <param name="request">Load request options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The load result.</returns>
        private async Task<ConversationLoadResult> LoadFromSourceAsync(
            ConversationLoadRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                ConversationRuntimeCatalog catalog = await _catalogProvider.LoadCatalogAsync(
                    cancellationToken);
                ConversationRuntimeCatalog.Entry catalogEntry =
                    ConversationCatalogLookup.FindCatalogEntry(
                        catalog,
                        request.ConversationId);

                if (catalogEntry == null)
                {
                    return ConversationLoadResult.Failure(
                        request.ConversationId,
                        $"Conversation '{request.ConversationId}' is missing from the runtime catalog.");
                }

                if (string.IsNullOrWhiteSpace(catalogEntry.PayloadKey))
                {
                    return ConversationLoadResult.Failure(
                        request.ConversationId,
                        $"Conversation '{request.ConversationId}' does not define an Addressables payload key.");
                }

                string payloadJson = await ConversationAddressablesReflection.LoadTextAsync(
                    catalogEntry.PayloadKey,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(payloadJson))
                {
                    return ConversationLoadResult.Failure(
                        request.ConversationId,
                        $"Conversation payload '{catalogEntry.PayloadKey}' is empty.");
                }

                ConversationData conversationData = JsonUtility.FromJson<ConversationData>(
                    payloadJson);

                if (conversationData == null)
                {
                    return ConversationLoadResult.Failure(
                        request.ConversationId,
                        $"Conversation payload '{catalogEntry.PayloadKey}' could not be deserialized.");
                }

                _cache.Store(
                    request.ConversationId,
                    conversationData,
                    request.AcquireReference);

                return ConversationLoadResult.Success(
                    request.ConversationId,
                    conversationData,
                    catalogEntry,
                    loadedFromCache: false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return ConversationLoadResult.Failure(
                    request.ConversationId,
                    exception.Message);
            }
        }

        #endregion
    }

    /// <summary>
    /// Tries one primary loader first and falls back to a secondary loader when the primary fails.
    /// </summary>
    internal sealed class FallbackConversationLoader : IConversationLoader
    {
        #region Fields

        private readonly IConversationLoader _primaryLoader;

        private readonly IConversationLoader _secondaryLoader;

        private readonly IConversationCache _cache;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes one fallback loader chain.
        /// </summary>
        /// <param name="primaryLoader">Primary loader attempted first.</param>
        /// <param name="secondaryLoader">Fallback loader attempted after primary failure.</param>
        /// <param name="cache">Shared cache used by both backends.</param>
        public FallbackConversationLoader(
            IConversationLoader primaryLoader,
            IConversationLoader secondaryLoader,
            IConversationCache cache)
        {
            _primaryLoader = primaryLoader ?? throw new ArgumentNullException(nameof(primaryLoader));
            _secondaryLoader = secondaryLoader ?? throw new ArgumentNullException(nameof(secondaryLoader));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        #endregion

        #region Public API

        /// <inheritdoc />
        public Task<ConversationLoadResult> LoadAsync(
            SerializableGuid conversationId,
            CancellationToken cancellationToken = default)
        {
            return LoadWithFallbackAsync(
                conversationId,
                cancellationToken,
                static (loader, id, token) => loader.LoadAsync(id, token));
        }

        /// <inheritdoc />
        public Task<ConversationLoadResult> PreloadAsync(
            SerializableGuid conversationId,
            CancellationToken cancellationToken = default)
        {
            return LoadWithFallbackAsync(
                conversationId,
                cancellationToken,
                static (loader, id, token) => loader.PreloadAsync(id, token));
        }

        /// <inheritdoc />
        public void Release(SerializableGuid conversationId)
        {
            _cache.Release(conversationId);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Executes one load operation against the primary backend and retries against the fallback when needed.
        /// </summary>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="operation">Operation to execute against the selected backend.</param>
        /// <returns>The resolved load result.</returns>
        private async Task<ConversationLoadResult> LoadWithFallbackAsync(
            SerializableGuid conversationId,
            CancellationToken cancellationToken,
            Func<IConversationLoader, SerializableGuid, CancellationToken, Task<ConversationLoadResult>> operation)
        {
            ConversationLoadResult primaryResult = await operation(
                _primaryLoader,
                conversationId,
                cancellationToken);

            if (primaryResult.Succeeded)
            {
                return primaryResult;
            }

            ConversationLoadResult secondaryResult = await operation(
                _secondaryLoader,
                conversationId,
                cancellationToken);

            if (secondaryResult.Succeeded)
            {
                return secondaryResult;
            }

            if (string.IsNullOrWhiteSpace(primaryResult.FailureReason))
            {
                return secondaryResult;
            }

            if (string.IsNullOrWhiteSpace(secondaryResult.FailureReason))
            {
                return primaryResult;
            }

            return ConversationLoadResult.Failure(
                conversationId,
                $"Primary backend failed: {primaryResult.FailureReason} "
                + $"Fallback backend failed: {secondaryResult.FailureReason}");
        }

        #endregion
    }

    /// <summary>
    /// Resolves catalog entries by stable conversation identifier for all catalog-backed loaders.
    /// </summary>
    internal static class ConversationCatalogLookup
    {
        /// <summary>
        /// Resolves one catalog entry by stable conversation identifier.
        /// </summary>
        /// <param name="catalog">Runtime catalog.</param>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <returns>The matching catalog entry when present.</returns>
        public static ConversationRuntimeCatalog.Entry FindCatalogEntry(
            ConversationRuntimeCatalog catalog,
            SerializableGuid conversationId)
        {
            if (catalog?.Entries == null)
            {
                return null;
            }

            for (int index = 0; index < catalog.Entries.Count; index++)
            {
                ConversationRuntimeCatalog.Entry entry = catalog.Entries[index];

                if (entry != null && entry.ConversationId == conversationId)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}