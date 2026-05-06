using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.LoggerModule
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Logging module.
    /// </summary>
    public static class LoggingModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the Logging module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "logging",
            "Logging",
            "Provides colored diagnostic logging for runtime and editor workflows.",
            HandyModuleActivationMode.Optional,
            -1000
        );

        /// <summary>
        /// Gets the dependency status list for the Logging module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the Logging module is currently active.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}