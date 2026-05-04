using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.ProjectSetup
{
    /// <summary>
    /// Describes one HandyTools-managed scripting define symbol.
    /// </summary>
    internal sealed class HandyScriptingDefineDefinition
    {
        private readonly Func<bool> _availabilityResolver;

        /// <summary>
        /// Initializes one scripting define definition.
        /// </summary>
        /// <param name="symbol">Exact scripting define symbol.</param>
        /// <param name="displayName">Human-readable name.</param>
        /// <param name="description">User-facing description.</param>
        /// <param name="enableOnSetup">
        /// Whether the setup flow should enable the define when it is
        /// available.
        /// </param>
        /// <param name="availabilityResolver">
        /// Optional availability resolver for the current project.
        /// </param>
        /// <param name="unavailableReason">
        /// Optional message shown when the define cannot be enabled.
        /// </param>
        public HandyScriptingDefineDefinition(
            string symbol,
            string displayName,
            string description,
            bool enableOnSetup = false,
            Func<bool> availabilityResolver = null,
            string unavailableReason = null
        )
        {
            Symbol = symbol;
            DisplayName = displayName;
            Description = description;
            EnableOnSetup = enableOnSetup;
            _availabilityResolver = availabilityResolver;
            UnavailableReason = unavailableReason;
        }

        /// <summary>
        /// Gets the exact scripting define symbol.
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Gets the human-readable label shown in the configuration window.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the user-facing description of the define.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets whether setup should enable this define by default.
        /// </summary>
        public bool EnableOnSetup { get; }

        /// <summary>
        /// Gets the message shown when the define is unavailable.
        /// </summary>
        public string UnavailableReason { get; }

        /// <summary>
        /// Gets whether the define can be enabled in the current project.
        /// </summary>
        public bool IsAvailable => _availabilityResolver?.Invoke() ?? true;
    }

    /// <summary>
    /// Central registry for HandyTools-managed scripting define symbols.
    /// </summary>
    internal static class HandyScriptingDefineRegistry
    {
        private const string HandyDotweenPresentDefine = "HANDY_DOTWEEN_PRESENT";
        private const string HandyDebugDefine = "HANDY_DEBUG";
        private const string HandyToolsDevelopmentDefine = "HANDY_TOOLS_DEVELOPMENT";
        private const string UnitaskDotweenSupportDefine = "UNITASK_DOTWEEN_SUPPORT";

        private static readonly IReadOnlyList<HandyScriptingDefineDefinition> _definitions =
            CreateDefinitions();

        /// <summary>
        /// Gets all scripting defines managed by HandyTools editor tooling.
        /// </summary>
        public static IReadOnlyList<HandyScriptingDefineDefinition> Definitions =>
            _definitions;

        /// <summary>
        /// Resolves one definition by symbol.
        /// </summary>
        /// <param name="symbol">Exact scripting define symbol.</param>
        /// <returns>The matching definition, or null when none exists.</returns>
        public static HandyScriptingDefineDefinition Find(string symbol)
        {
            foreach (HandyScriptingDefineDefinition definition in _definitions)
            {
                if (definition.Symbol == symbol)
                {
                    return definition;
                }
            }

            return null;
        }

        private static IReadOnlyList<HandyScriptingDefineDefinition> CreateDefinitions()
        {
            return new[]
            {
                new HandyScriptingDefineDefinition(
                    HandyDotweenPresentDefine,
                    "DOTween Integration",
                    "Compiles DOTween-backed HandyTools APIs such as light tween extensions.",
                    availabilityResolver: IsDotweenInstalled,
                    unavailableReason:
                        "DOTween.dll was not found under Assets/Plugins/Demigiant/DOTween. Install DOTween before enabling this define."
                ),
                new HandyScriptingDefineDefinition(
                    UnitaskDotweenSupportDefine,
                    "UniTask DOTween Support",
                    "Enables UniTask integration code paths that depend on DOTween support.",
                    enableOnSetup: true,
                    availabilityResolver: IsDotweenInstalled,
                    unavailableReason:
                        "DOTween.dll was not found under Assets/Plugins/Demigiant/DOTween. Install DOTween before enabling this define."
                ),
                new HandyScriptingDefineDefinition(
                    HandyDebugDefine,
                    "Handy Debug Runtime",
                    "Keeps HandyTools debug-only runtime hooks available outside the editor."
                ),
                new HandyScriptingDefineDefinition(
                    HandyToolsDevelopmentDefine,
                    "HandyTools Development",
                    "Enables HandyTools development-only editor actions and internal workflows."
                ),
            };
        }

        private static bool IsDotweenInstalled()
        {
            string dotweenDllPath = Path.Combine(
                Application.dataPath,
                "Plugins",
                "Demigiant",
                "DOTween",
                "DOTween.dll"
            );

            return File.Exists(dotweenDllPath);
        }
    }
}