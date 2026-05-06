using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.RenderingModule
{
    /// <summary>
    /// Runtime bootstrapper for the Rendering module.
    /// </summary>
    public sealed class RenderingModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => RenderingModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            RenderingModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
        }
    }
}