using System;
using IndieGabo.HandyTools.ConversationsModule.Cache;

namespace IndieGabo.HandyTools.ConversationsModule.Loading
{
    /// <summary>
    /// Creates the default runtime loading services for the configured Conversations loading strategy.
    /// </summary>
    public static class ConversationLoaderFactory
    {
        #region Public API

        /// <summary>
        /// Creates the default runtime cache using the provided settings.
        /// </summary>
        /// <param name="settings">Project runtime settings.</param>
        /// <returns>The created runtime cache.</returns>
        public static IConversationCache CreateCache(ConversationRuntimeSettings settings)
        {
            settings ??= ConversationRuntimeSettings.Instance;
            return new ConversationCacheLRU(settings.CacheCapacity);
        }

        /// <summary>
        /// Creates the default catalog provider using the provided settings.
        /// </summary>
        /// <param name="settings">Project runtime settings.</param>
        /// <returns>The created catalog provider.</returns>
        public static IConversationCatalogProvider CreateCatalogProvider(
            ConversationRuntimeSettings settings)
        {
            settings ??= ConversationRuntimeSettings.Instance;

            return settings.LoadingStrategy switch
            {
                ConversationLoadingStrategy.StreamingAssetsOnly =>
                    CreateStreamingCatalogProvider(settings),
                ConversationLoadingStrategy.AddressablesOnly =>
                    CreateAddressablesCatalogProvider(settings.LoadingStrategy),
                ConversationLoadingStrategy.AddressablesWithStreamingFallback =>
                    new FallbackConversationCatalogProvider(
                        CreateAddressablesCatalogProvider(settings.LoadingStrategy),
                        CreateStreamingCatalogProvider(settings)),
                ConversationLoadingStrategy.StreamingWithAddressablesFallback =>
                    new FallbackConversationCatalogProvider(
                        CreateStreamingCatalogProvider(settings),
                        CreateAddressablesCatalogProvider(settings.LoadingStrategy)),
                _ => throw BuildUnsupportedStrategyException(settings.LoadingStrategy),
            };
        }

        /// <summary>
        /// Creates the default runtime loader using the provided settings, provider, and cache.
        /// </summary>
        /// <param name="settings">Project runtime settings.</param>
        /// <param name="catalogProvider">Catalog provider to use.</param>
        /// <param name="cache">Runtime cache to use.</param>
        /// <returns>The created runtime loader.</returns>
        public static IConversationLoader CreateLoader(
            ConversationRuntimeSettings settings,
            IConversationCatalogProvider catalogProvider,
            IConversationCache cache)
        {
            settings ??= ConversationRuntimeSettings.Instance;
            catalogProvider ??= CreateCatalogProvider(settings);
            cache ??= CreateCache(settings);

            return settings.LoadingStrategy switch
            {
                ConversationLoadingStrategy.StreamingAssetsOnly =>
                    CreateConcreteLoader(
                        catalogProvider ?? CreateStreamingCatalogProvider(settings),
                        cache),
                ConversationLoadingStrategy.AddressablesOnly =>
                    CreateConcreteLoader(
                        catalogProvider ?? CreateAddressablesCatalogProvider(settings.LoadingStrategy),
                        cache),
                ConversationLoadingStrategy.AddressablesWithStreamingFallback =>
                    CreateFallbackLoader(
                        settings,
                        catalogProvider,
                        cache,
                        addressablesPrimary: true),
                ConversationLoadingStrategy.StreamingWithAddressablesFallback =>
                    CreateFallbackLoader(
                        settings,
                        catalogProvider,
                        cache,
                        addressablesPrimary: false),
                _ => throw BuildUnsupportedStrategyException(settings.LoadingStrategy),
            };
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates one streaming catalog provider from the requested settings.
        /// </summary>
        /// <param name="settings">Project runtime settings.</param>
        /// <returns>The created streaming catalog provider.</returns>
        private static StreamingConversationCatalogProvider CreateStreamingCatalogProvider(
            ConversationRuntimeSettings settings)
        {
            return new StreamingConversationCatalogProvider(
                settings?.StreamingAssetsRootOverride);
        }

        /// <summary>
        /// Creates one Addressables catalog provider when the package backend is available.
        /// </summary>
        /// <param name="loadingStrategy">Strategy requesting the Addressables backend.</param>
        /// <returns>The created Addressables catalog provider.</returns>
        private static AddressableConversationCatalogProvider CreateAddressablesCatalogProvider(
            ConversationLoadingStrategy loadingStrategy)
        {
            if (!ConversationAddressablesReflection.IsAvailable)
            {
                throw BuildUnsupportedStrategyException(loadingStrategy);
            }

            return new AddressableConversationCatalogProvider();
        }

        /// <summary>
        /// Creates one concrete loader for the provided provider backend.
        /// </summary>
        /// <param name="catalogProvider">Catalog provider that controls payload resolution.</param>
        /// <param name="cache">Shared runtime cache.</param>
        /// <returns>The created concrete loader.</returns>
        private static IConversationLoader CreateConcreteLoader(
            IConversationCatalogProvider catalogProvider,
            IConversationCache cache)
        {
            return catalogProvider switch
            {
                StreamingConversationCatalogProvider =>
                    new StreamingConversationLoader(catalogProvider, cache),
                AddressableConversationCatalogProvider =>
                    new AddressableConversationLoader(catalogProvider, cache),
                _ => throw new NotSupportedException(
                    $"Conversation catalog provider '{catalogProvider?.GetType().FullName ?? "<null>"}' "
                    + "does not have a matching runtime loader implementation."),
            };
        }

        /// <summary>
        /// Creates one fallback loader chain for one hybrid strategy.
        /// </summary>
        /// <param name="settings">Project runtime settings.</param>
        /// <param name="catalogProvider">Optional catalog provider passed by the caller.</param>
        /// <param name="cache">Shared runtime cache.</param>
        /// <param name="addressablesPrimary">Whether Addressables should be attempted before StreamingAssets.</param>
        /// <returns>The created fallback loader chain.</returns>
        private static IConversationLoader CreateFallbackLoader(
            ConversationRuntimeSettings settings,
            IConversationCatalogProvider catalogProvider,
            IConversationCache cache,
            bool addressablesPrimary)
        {
            IConversationCatalogProvider primaryProvider;
            IConversationCatalogProvider secondaryProvider;

            if (catalogProvider is FallbackConversationCatalogProvider fallbackCatalogProvider)
            {
                primaryProvider = fallbackCatalogProvider.PrimaryProvider;
                secondaryProvider = fallbackCatalogProvider.SecondaryProvider;
            }
            else
            {
                primaryProvider = addressablesPrimary
                    ? CreateAddressablesCatalogProvider(settings.LoadingStrategy)
                    : CreateStreamingCatalogProvider(settings);
                secondaryProvider = addressablesPrimary
                    ? CreateStreamingCatalogProvider(settings)
                    : CreateAddressablesCatalogProvider(settings.LoadingStrategy);
            }

            return new FallbackConversationLoader(
                CreateConcreteLoader(primaryProvider, cache),
                CreateConcreteLoader(secondaryProvider, cache),
                cache);
        }

        /// <summary>
        /// Builds one explicit unsupported-strategy exception so runtime configuration failures are not silent.
        /// </summary>
        /// <param name="loadingStrategy">Unsupported loading strategy.</param>
        /// <returns>The created exception.</returns>
        private static NotSupportedException BuildUnsupportedStrategyException(
            ConversationLoadingStrategy loadingStrategy)
        {
            return new NotSupportedException(
                $"Conversation loading strategy '{loadingStrategy}' is not available because the Unity Addressables package backend is not accessible in the current domain.");
        }

        #endregion
    }
}