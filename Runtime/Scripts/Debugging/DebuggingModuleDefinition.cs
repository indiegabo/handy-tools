using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.DebuggingModule
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Debugging module.
    /// </summary>
    public static class DebuggingModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the Debugging module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "debugging",
            "Debugging",
            "Provides the runtime debug panel and project-level debugging settings.",
            HandyModuleActivationMode.Optional,
            500
        );

        /// <summary>
        /// Gets the dependency status list for the Debugging module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the Debugging module is currently active.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}