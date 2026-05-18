using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem;
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
        /// <param name="syncWithAvailability">
        /// Whether the editor should keep this define synchronized with the
        /// dependency availability on every script reload.
        /// </param>
        public HandyScriptingDefineDefinition(
            string symbol,
            string displayName,
            string description,
            bool enableOnSetup = false,
            Func<bool> availabilityResolver = null,
            string unavailableReason = null,
            bool syncWithAvailability = false
        )
        {
            Symbol = symbol;
            DisplayName = displayName;
            Description = description;
            EnableOnSetup = enableOnSetup;
            _availabilityResolver = availabilityResolver;
            UnavailableReason = unavailableReason;
            SyncWithAvailability = syncWithAvailability;
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
        /// Gets whether the editor should keep the define aligned with the
        /// dependency availability automatically.
        /// </summary>
        public bool SyncWithAvailability { get; }

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
        private const string _handyDotweenPresentDefine = "HANDY_DOTWEEN_PRESENT";
        private const string _handySimpleBlackboardPresentDefine =
            "HANDY_SIMPLE_BLACKBOARD_PRESENT";
        private const string _handyCharacterControllerProPresentDefine =
            "HANDY_CHARACTER_CONTROLLER_PRO_PRESENT";
        private const string _handyDialogueSystemPresentDefine =
            "HANDY_DIALOGUE_SYSTEM_PRESENT";
        private const string _handyDebugDefine = "HANDY_DEBUG";
        private const string _handyToolsDevelopmentDefine = "HANDY_TOOLS_DEVELOPMENT";

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
                    _handyDotweenPresentDefine,
                    "DOTween Integration",
                    "Compiles DOTween-backed HandyTools APIs such as light tween extensions.",
                    availabilityResolver: IsDotweenInstalled,
                    unavailableReason:
                        "DOTween.dll was not found under Assets/Plugins/Demigiant/DOTween. Install DOTween before enabling this define.",
                    syncWithAvailability: true
                ),
                new HandyScriptingDefineDefinition(
                    _handySimpleBlackboardPresentDefine,
                    "Simple Blackboard Integration",
                    "Compiles HandyTools FSM Simple Blackboard integration when the runtime package is present.",
                    availabilityResolver: IsSimpleBlackboardInstalled,
                    unavailableReason:
                        "Simple Blackboard runtime types were not found in the current project.",
                    syncWithAvailability: true
                ),
                new HandyScriptingDefineDefinition(
                    _handyCharacterControllerProPresentDefine,
                    "Character Controller Pro Integration",
                    "Compiles HandyTools FSM Character Controller Pro integration when the runtime package is present.",
                    availabilityResolver: IsCharacterControllerProInstalled,
                    unavailableReason:
                        "Character Controller Pro runtime types were not found in the current project.",
                    syncWithAvailability: true
                ),
                new HandyScriptingDefineDefinition(
                    _handyDialogueSystemPresentDefine,
                    "Dialogue System Integration",
                    "Compiles HandyTools Cutscenes Dialogue System integration when the runtime package is present.",
                    availabilityResolver: IsDialogueSystemInstalled,
                    unavailableReason:
                        "Dialogue System runtime types were not found in the current project.",
                    syncWithAvailability: true
                ),
                new HandyScriptingDefineDefinition(
                    _handyDebugDefine,
                    "Handy Debug Runtime",
                    "Keeps HandyTools debug-only runtime hooks available outside the editor."
                ),
                new HandyScriptingDefineDefinition(
                    _handyToolsDevelopmentDefine,
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

        private static bool IsSimpleBlackboardInstalled()
        {
            return AreTypesAvailable(
                "Zor.SimpleBlackboard.Components.SimpleBlackboardContainer",
                "Zor.SimpleBlackboard.Core.Blackboard",
                "Zor.SimpleBlackboard.Core.BlackboardPropertyName"
            );
        }

        private static bool IsCharacterControllerProInstalled()
        {
            return AreTypesAvailable(
                "Lightbug.CharacterControllerPro.Core.CharacterActor",
                "Lightbug.CharacterControllerPro.Core.CharacterBody"
            );
        }

        private static bool IsDialogueSystemInstalled()
        {
            return DialogueSystemIntegrationAvailability.IsAvailable();
        }

        private static bool AreTypesAvailable(params string[] fullTypeNames)
        {
            if (fullTypeNames == null || fullTypeNames.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < fullTypeNames.Length; index++)
            {
                if (ResolveType(fullTypeNames[index]) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static Type ResolveType(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return null;
            }

            Type directType = Type.GetType(fullTypeName);
            if (directType != null)
            {
                return directType;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Type resolvedType = assemblies[index].GetType(fullTypeName);
                if (resolvedType != null)
                {
                    return resolvedType;
                }
            }

            return null;
        }
    }
}