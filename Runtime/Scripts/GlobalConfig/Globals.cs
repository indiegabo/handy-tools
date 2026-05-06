#nullable enable
using System;
using IndieGabo.HandyTools.GlobalConfigModule.JsonTree;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.GlobalConfigModule
{
    /// <summary>
    /// Static facade over a single JsonValueTree instance that mirrors the
    /// contents of Assets/Resources/globals (no extension). The tree is
    /// loaded into memory before the first scene loads at runtime.
    /// </summary>
    public static class Globals
    {
        #region State

        /// <summary>
        /// Global in-memory JSON value tree that represents globals.
        /// </summary>
        private static readonly JsonValueTree _tree = new();

        #endregion

        #region Initialization

        /// <summary>
        /// Loads globals JSON text from provider into the in-memory tree.
        /// Safe to call multiple times; replaces the internal state.
        /// </summary>
        public static void LoadFromGlobals()
        {
            var json = GlobalsJsonProvider.LoadJsonOrEmpty();
            _tree.LoadFromJson(json);
        }

        #endregion

        #region Queries

        /// <summary>
        /// Attempts to get a value at a dotted path. Returns true on success.
        /// </summary>
        /// <typeparam name="T">Expected value type.</typeparam>
        /// <param name="path">Dotted path (e.g., "game.audio.volume").</param>
        /// <param name="value">Out value when found and convertible.</param>
        public static bool TryGet<T>(string path, out T value) =>
            _tree.TryGetValue(path, out value);

        /// <summary>
        /// Gets a value or a provided fallback if missing or incompatible.
        /// </summary>
        /// <typeparam name="T">Expected value type.</typeparam>
        /// <param name="path">Dotted path (e.g., "ui.theme.primary").</param>
        /// <param name="fallback">Returned when missing or wrong type.</param>
        public static T GetOrDefault<T>(string path, T fallback = default!)
        {
            return _tree.TryGetValue(path, out T v) ? v : fallback;
        }

        #endregion

        #region Mutations

        /// <summary>
        /// Sets a value at the dotted path, creating intermediate nodes
        /// as needed. Passing null writes a NullNode.
        /// </summary>
        /// <param name="path">Dotted path to set.</param>
        /// <param name="value">New value or null.</param>
        public static void Set(string path, object? value) =>
            _tree.SetValue(path, value);

        #endregion

        #region Export

        /// <summary>
        /// Serializes the in-memory tree to a JSON string.
        /// </summary>
        /// <param name="indented">Pretty print when true.</param>
        public static string ExportJson(bool indented = true) =>
            _tree.ToJson(indented);

        #endregion

        #region Editor

#if UNITY_EDITOR
        /// <summary>
        /// Writes the in-memory tree to the project globals file and
        /// refreshes the AssetDatabase. Creates the directory if missing.
        /// </summary>
        public static void SaveGlobalsToDisk()
        {
            var json = ExportJson(true);
            var path = GlobalsJsonProvider.GetProjectFilePath();

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }
#endif

        #endregion

        #region Advanced

        /// <summary>
        /// Provides direct access to the underlying JsonValueTree for
        /// advanced scenarios. Mutations affect the global state.
        /// </summary>
        public static JsonValueTree GetTree() => _tree;

        #endregion
    }
}
