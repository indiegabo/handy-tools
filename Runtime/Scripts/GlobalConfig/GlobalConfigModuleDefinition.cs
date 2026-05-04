using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.GlobalConfig
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the GlobalConfig module.
    /// </summary>
    public static class GlobalConfigModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the GlobalConfig module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "global-config",
            "Globals",
            "Provides editable global JSON data loaded from Resources/globals.",
            HandyModuleActivationMode.Optional,
            130
        );

        /// <summary>
        /// Gets the dependency status list for the GlobalConfig module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the GlobalConfig module is currently active.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}