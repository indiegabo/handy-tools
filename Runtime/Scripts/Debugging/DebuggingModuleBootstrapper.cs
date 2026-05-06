using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.DebuggingModule
{
    /// <summary>
    /// Opt-in runtime bootstrapper for the Debugging module.
    /// </summary>
    public sealed class DebuggingModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => DebuggingModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            DebuggingModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
            DebugPanelBootstrapper.Bootstrap();
        }
    }
}