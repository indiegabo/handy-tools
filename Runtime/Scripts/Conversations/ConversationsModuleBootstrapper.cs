using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Cache;
using IndieGabo.HandyTools.ConversationsModule.Blackboard;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Loading;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Registers the Conversations graph family when the optional module is active.
    /// </summary>
    public sealed class ConversationsModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => ConversationsModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            ConversationsModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
            ConversationGraphFamily.Register();
            GraphBlackboardValueRegistry.RegisterFamilyWrapper(
                ConversationGraphFamily.Id,
                typeof(ConversationActorIdBlackboardValue));

            RegisterRuntimeServices();
        }

        /// <summary>
        /// Registers the default runtime services needed by exported Conversations payloads.
        /// </summary>
        private static void RegisterRuntimeServices()
        {
            ConversationRuntimeSettings settings = ConversationRuntimeSettings.Instance;

            if (ServiceLocator.TryGet<IConversationLoader>(out IConversationLoader existingLoader)
                && existingLoader != null)
            {
                _ = ServiceLocator.Deregister<IConversationLoader>(existingLoader);
            }

            if (ServiceLocator.TryGet<IConversationCatalogProvider>(
                    out IConversationCatalogProvider existingCatalogProvider)
                && existingCatalogProvider != null)
            {
                _ = ServiceLocator.Deregister<IConversationCatalogProvider>(
                    existingCatalogProvider);
            }

            if (ServiceLocator.TryGet<IConversationCache>(out IConversationCache existingCache)
                && existingCache != null)
            {
                _ = ServiceLocator.Deregister<IConversationCache>(existingCache);
            }

            IConversationCache cache = ConversationLoaderFactory.CreateCache(settings);
            IConversationCatalogProvider catalogProvider =
                ConversationLoaderFactory.CreateCatalogProvider(settings);
            IConversationLoader loader = ConversationLoaderFactory.CreateLoader(
                settings,
                catalogProvider,
                cache);

            ServiceLocator.Register<IConversationCache>(cache);
            ServiceLocator.Register<IConversationCatalogProvider>(catalogProvider);
            ServiceLocator.Register<IConversationLoader>(loader);
        }
    }
}