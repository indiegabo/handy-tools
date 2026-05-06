using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.SteamModule
{
    /// <summary>
    /// Opt-in runtime bootstrapper for the Steam module.
    /// </summary>
    public sealed class SteamModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => SteamModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            SteamModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
            HandySteamManager.Bootstrap();
        }
    }
}