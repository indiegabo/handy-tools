using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.Pooling
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Pooling module.
    /// </summary>
    public static class PoolingModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the Pooling module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "pooling",
            "Pooling",
            "Provides reusable runtime object pooling helpers and initializers.",
            HandyModuleActivationMode.Optional,
            180,
            isActiveByDefault: true
        );

        /// <summary>
        /// Gets the dependency status list for the Pooling module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the Pooling module is currently active.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}