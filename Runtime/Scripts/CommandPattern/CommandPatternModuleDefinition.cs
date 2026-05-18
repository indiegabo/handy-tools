using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Command Pattern module.
    /// </summary>
    public static class CommandPatternModuleDefinition
    {
        #region Static Data

        private static readonly IReadOnlyList<HandyModuleDependencyStatus>
            _dependencies = Array.Empty<HandyModuleDependencyStatus>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the stable runtime descriptor for the Command Pattern module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "command-pattern",
            "Command Pattern",
            "Provides command orchestration, scheduling, undo and redo, and runtime diagnostics.",
            HandyModuleActivationMode.Optional,
            172,
            isActiveByDefault: true);

        /// <summary>
        /// Gets the dependency status list for the Command Pattern module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        /// <summary>
        /// Gets whether the Command Pattern module is currently active.
        /// </summary>
        public static bool IsActive =>
            HandyModuleSettings.Instance.IsModuleActive(Descriptor);

        #endregion
    }
}