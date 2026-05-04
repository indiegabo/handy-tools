#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define HANDY_STEAM_UNSUPPORTED_PLATFORM
#endif

using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Modules;

namespace IndieGabo.HandyTools.Steam
{
    /// <summary>
    /// Centralizes metadata and dependency rules for the Steam module.
    /// </summary>
    public static class SteamModuleDefinition
    {
        private static readonly HandyModuleDependency _platformDependency = new(
            "steam-supported-platform",
            "Desktop Steamworks Platform",
            "Steamworks is available only on Windows, Linux, and macOS standalone targets."
        );

        private static readonly IReadOnlyList<HandyModuleDependencyStatus> _dependencies =
            CreateDependencies();

        /// <summary>
        /// Gets the stable runtime descriptor for the Steam module.
        /// </summary>
        public static HandyModuleDescriptor Descriptor { get; } = new(
            "steam",
            "Steam",
            "Bootstraps the persistent Steamworks.NET manager for desktop standalone targets.",
            HandyModuleActivationMode.Optional,
            150
        );

        /// <summary>
        /// Gets the dependency status list for the Steam module.
        /// </summary>
        public static IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            _dependencies;

        private static IReadOnlyList<HandyModuleDependencyStatus> CreateDependencies()
        {
#if HANDY_STEAM_UNSUPPORTED_PLATFORM
            return new[]
            {
                new HandyModuleDependencyStatus(
                    _platformDependency,
                    false,
                    "Switch the build target to Windows, Linux, or macOS standalone to enable Steam."
                ),
            };
#else
            return new[]
            {
                new HandyModuleDependencyStatus(
                    _platformDependency,
                    true,
                    "Current compilation target supports Steamworks.NET."
                ),
            };
#endif
        }
    }
}