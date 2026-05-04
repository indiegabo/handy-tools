using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.SaveSystem
{
    /// <summary>
    /// Opt-in runtime bootstrapper for the SaveSystem module.
    /// </summary>
    public sealed class SaveSystemModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => SaveSystemModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            SaveSystemModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
            SaveSystemBootstrapper.Bootstrap();
        }
    }
}