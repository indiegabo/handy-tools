#nullable enable
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.GlobalConfigModule
{
    /// <summary>
    /// Centralized loader for globals JSON content.
    /// 1) Tries Resources("globals").
    /// 2) In Editor, falls back to the project file when it already exists.
    /// 3) If missing everywhere, returns "{}" without creating files.
    /// </summary>
    public static class GlobalsJsonProvider
    {
        /// <summary>
        /// Tries to load the globals JSON from Resources.
        /// </summary>
        private static string? TryLoadFromResources()
        {
            TextAsset text = Resources.Load<TextAsset>("globals");
            return text?.text;
        }

        /// <summary>
        /// Loads the globals JSON according to the 3-step algorithm.
        /// </summary>
        public static string LoadJsonOrEmpty()
        {
            // 1) Try from Resources first.
            var json = TryLoadFromResources();
            if (!string.IsNullOrEmpty(json)) return json;

#if UNITY_EDITOR
            // 2) In Editor: read the project file directly when it already exists.
            var absPath = GetProjectFilePath();
            if (File.Exists(absPath))
            {
                return File.ReadAllText(absPath) ?? "{}";
            }
#endif

            // 3) Still missing: return an empty JSON object.
            return "{}";
        }

#if UNITY_EDITOR
        /// <summary>
        /// Absolute path to Assets/Resources/globals (no extension).
        /// </summary>
        public static string GetProjectFilePath()
        {
            return Path.Combine(Application.dataPath, "Resources", "globals.json");
        }
#endif
    }
}
