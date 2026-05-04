using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.ScreenShooter
{
    /// <summary>
    /// Opt-in runtime bootstrapper for the ScreenShooter module.
    /// </summary>
    public sealed class ScreenShooterModuleBootstrapper : IHandyModuleBootstrapper
    {
        /// <inheritdoc />
        public HandyModuleDescriptor Descriptor => ScreenShooterModuleDefinition.Descriptor;

        /// <inheritdoc />
        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            ScreenShooterModuleDefinition.Dependencies;

        /// <inheritdoc />
        public void Bootstrap()
        {
            ScreenShooterBootstrapper.Bootstrap();
        }
    }
}