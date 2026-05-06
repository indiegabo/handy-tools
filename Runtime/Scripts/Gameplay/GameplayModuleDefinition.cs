using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.GameplayModule
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Gameplay module.
    /// </summary>
    public static class GameplayModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the Gameplay module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "gameplay",
            "Gameplay",
            "Provides the global gameplay lifecycle service and session time tracking.",
            HandyModuleActivationMode.Optional,
            40
        );

        /// <summary>
        /// Gets the dependency status list for the Gameplay module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the Gameplay module is currently active.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}