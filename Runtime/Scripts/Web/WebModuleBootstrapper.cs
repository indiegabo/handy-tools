using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.WebModule
{
    /// <summary>
    /// Runtime bootstrapper for the Web module.
    /// </summary>
    public sealed class WebModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => WebModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            WebModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
        }
    }
}