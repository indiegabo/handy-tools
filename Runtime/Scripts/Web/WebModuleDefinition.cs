using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.WebModule
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Web module.
    /// </summary>
    public static class WebModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        /// <summary>
        /// Gets the stable runtime descriptor for the Web module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "web",
            "Web",
            "Provides HandyTools web request helpers and integrations.",
            HandyModuleActivationMode.Optional,
            170,
            isActiveByDefault: true
        );

        /// <summary>
        /// Gets the dependency status list for the Web module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the Web module is currently active.
        /// </summary>
        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}