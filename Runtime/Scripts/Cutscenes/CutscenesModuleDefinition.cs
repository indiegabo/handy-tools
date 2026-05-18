using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.CutscenesModule
{
    public static class CutscenesModuleDefinition
    {
        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            Array.Empty<HandyModuleDependencyStatus>();

        public static HandyModuleDescriptor Descriptor { get; } = new(
            "cutscenes",
            "Cutscenes",
            "Scene-authored cutscene graphs with optional Dialogue System integration.",
            HandyModuleActivationMode.Optional,
            175
        );

        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        public static bool IsActive => HandyModuleSettings.Instance.IsModuleActive(Descriptor);
    }
}