using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.ScreenShooter
{
    /// <summary>
    /// Centralizes metadata for the ScreenShooter module.
    /// </summary>
    public static class ScreenShooterModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the ScreenShooter module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "screen-shooter",
            "ScreenShooter",
            "Bootstraps a runtime screenshot capturer with configurable trigger and output directory.",
            HandyModuleActivationMode.Optional,
            160
        );

        /// <summary>
        /// Gets the dependency status list for the ScreenShooter module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;
    }
}