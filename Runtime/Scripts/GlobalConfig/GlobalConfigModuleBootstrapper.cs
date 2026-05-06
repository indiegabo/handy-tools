using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.GlobalConfigModule
{
    /// <summary>
    /// Opt-in runtime bootstrapper for the GlobalConfig module.
    /// </summary>
    public sealed class GlobalConfigModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => GlobalConfigModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            GlobalConfigModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
            Globals.LoadFromGlobals();
        }
    }
}