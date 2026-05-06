using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.GameplayModule
{
    /// <summary>
    /// Opt-in runtime bootstrapper for the Gameplay module.
    /// </summary>
    public sealed class GameplayModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => GameplayModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            GameplayModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
            GameplayServiceBootstrapper.Bootstrap();
        }
    }
}