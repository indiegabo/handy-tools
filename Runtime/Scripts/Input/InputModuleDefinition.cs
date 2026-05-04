using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.HandyInputSystem
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Input module.
    /// </summary>
    public static class InputModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the Input module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "input",
            "Input",
            "Provides player input bootstrapping and shared multiplayer input configuration.",
            HandyModuleActivationMode.Optional,
            30
        );

        /// <summary>
        /// Gets the dependency status list for the Input module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the Input module is currently active.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}