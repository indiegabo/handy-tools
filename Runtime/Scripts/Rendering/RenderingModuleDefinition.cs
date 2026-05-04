using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.Rendering
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Rendering module.
    /// </summary>
    public static class RenderingModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the Rendering module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "rendering",
            "Rendering",
            "Provides rendering-specific extension helpers such as URP light transitions.",
            HandyModuleActivationMode.Optional,
            195,
            isActiveByDefault: true
        );

        /// <summary>
        /// Gets the dependency status list for the Rendering module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the Rendering module is currently active.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}