using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.Identifying
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Identifying module.
    /// </summary>
    public static class IdentifyingModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the Identifying module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "identifying",
            "Identifying",
            "Provides scene-object GUID components and GUID-backed references.",
            HandyModuleActivationMode.Optional,
            190,
            isActiveByDefault: true
        );

        /// <summary>
        /// Gets the dependency status list for the Identifying module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the Identifying module is currently active.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}