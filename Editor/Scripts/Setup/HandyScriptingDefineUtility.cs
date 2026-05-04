using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace IndieGabo.HandyTools.Editor.ProjectSetup
{
    /// <summary>
    /// Applies HandyTools-managed scripting define changes to the selected
    /// build target.
    /// </summary>
    internal static class HandyScriptingDefineUtility
    {
        /// <summary>
        /// Gets the named build target currently selected in the editor.
        /// </summary>
        public static NamedBuildTarget SelectedBuildTarget =>
            NamedBuildTarget.FromBuildTargetGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup
            );

        /// <summary>
        /// Gets whether the provided define is enabled for the selected build
        /// target.
        /// </summary>
        /// <param name="definition">Define definition to inspect.</param>
        /// <returns>True when the define is present.</returns>
        public static bool IsEnabled(HandyScriptingDefineDefinition definition)
        {
            return IsEnabled(definition.Symbol);
        }

        /// <summary>
        /// Gets whether the provided symbol is enabled for the selected build
        /// target.
        /// </summary>
        /// <param name="symbol">Exact scripting define symbol.</param>
        /// <returns>True when the define is present.</returns>
        public static bool IsEnabled(string symbol)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(
                SelectedBuildTarget
            );

            return ParseSymbols(defines).Contains(symbol);
        }

        /// <summary>
        /// Enables or disables one define for the selected build target.
        /// </summary>
        /// <param name="definition">Define definition to change.</param>
        /// <param name="enabled">Whether the define should be present.</param>
        public static void SetEnabled(
            HandyScriptingDefineDefinition definition,
            bool enabled
        )
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (enabled && !definition.IsAvailable)
            {
                return;
            }

            SetEnabled(definition.Symbol, enabled);
        }

        /// <summary>
        /// Enables or disables one define for the selected build target.
        /// </summary>
        /// <param name="symbol">Exact scripting define symbol.</param>
        /// <param name="enabled">Whether the define should be present.</param>
        public static void SetEnabled(string symbol, bool enabled)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(
                SelectedBuildTarget
            );
            string updatedDefines = UpdateSymbolList(defines, symbol, enabled);

            if (updatedDefines == defines)
            {
                return;
            }

            PlayerSettings.SetScriptingDefineSymbols(
                SelectedBuildTarget,
                updatedDefines
            );
        }

        /// <summary>
        /// Enables default setup defines and removes invalid states.
        /// </summary>
        public static void ApplySetupDefaults()
        {
            RemoveUnavailableDefines();

            foreach (HandyScriptingDefineDefinition definition
                in HandyScriptingDefineRegistry.Definitions)
            {
                if (!definition.EnableOnSetup || !definition.IsAvailable)
                {
                    continue;
                }

                SetEnabled(definition, true);
            }
        }

        /// <summary>
        /// Removes defines that cannot be compiled in the current project.
        /// </summary>
        public static void RemoveUnavailableDefines()
        {
            foreach (HandyScriptingDefineDefinition definition
                in HandyScriptingDefineRegistry.Definitions)
            {
                if (definition.IsAvailable)
                {
                    continue;
                }

                SetEnabled(definition, false);
            }
        }

        /// <summary>
        /// Adds or removes a symbol from a semicolon-separated define list.
        /// </summary>
        /// <param name="defines">Current define list.</param>
        /// <param name="symbol">Symbol to change.</param>
        /// <param name="enabled">Whether the symbol should exist.</param>
        /// <returns>The updated define list.</returns>
        public static string UpdateSymbolList(
            string defines,
            string symbol,
            bool enabled
        )
        {
            List<string> symbols = ParseSymbols(defines);
            bool containsSymbol = symbols.Contains(symbol);

            if (enabled)
            {
                if (!containsSymbol)
                {
                    symbols.Add(symbol);
                }
            }
            else if (containsSymbol)
            {
                symbols.RemoveAll(existingSymbol => existingSymbol == symbol);
            }

            return string.Join(";", symbols);
        }

        /// <summary>
        /// Splits a semicolon-separated define list into individual symbols.
        /// </summary>
        /// <param name="defines">Raw define list.</param>
        /// <returns>A mutable list of define symbols.</returns>
        public static List<string> ParseSymbols(string defines)
        {
            return new List<string>(
                defines.Split(';', StringSplitOptions.RemoveEmptyEntries)
            );
        }
    }
}