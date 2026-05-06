using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Animation Events
    /// module.
    /// </summary>
    public static class AnimationEventsModuleDefinition
    {
        #region Static Data

        private static readonly IReadOnlyList<HandyModuleDependencyStatus>
            _dependencies = Array.Empty<HandyModuleDependencyStatus>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the stable runtime descriptor for the Animation Events module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "animation-events",
            "Animation Events",
            "Provides state-scoped animation event dispatchers and HandyBus "
                + "integrations.",
            HandyModuleActivationMode.Optional,
            190,
            isActiveByDefault: true
        );

        /// <summary>
        /// Gets the dependency status list for the Animation Events module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the Animation Events module is currently active.
        /// </summary>
        public static bool IsActive =>
            HandyModuleSettings.Instance.IsModuleActive(Descriptor);

        #endregion
    }
}