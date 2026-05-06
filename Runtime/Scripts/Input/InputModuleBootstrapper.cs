using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.HandyInputSystemModule
{
    /// <summary>
    /// Opt-in runtime bootstrapper for the Input module.
    /// </summary>
    public sealed class InputModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => InputModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            InputModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
            ProjectInputConfig.Bootstrap();
        }
    }
}