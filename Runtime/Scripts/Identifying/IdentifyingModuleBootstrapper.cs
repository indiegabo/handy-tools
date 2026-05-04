using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.Identifying
{
    /// <summary>
    /// Runtime bootstrapper for the Identifying module.
    /// </summary>
    public sealed class IdentifyingModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => IdentifyingModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            IdentifyingModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
        }
    }
}