using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.SaveSystem
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the SaveSystem module.
    /// </summary>
    public static class SaveSystemModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the SaveSystem module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "save-system",
            "Save System",
            "Provides slot management, persistence settings, and save file encryption options.",
            HandyModuleActivationMode.Optional,
            100
        );

        /// <summary>
        /// Gets the dependency status list for the SaveSystem module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the SaveSystem module is currently active.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}