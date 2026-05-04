#nullable enable
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.GlobalConfig
{
    /// <summary>
    /// Centralized loader for globals JSON content.
    /// 1) Tries Resources("globals").
    /// 2) If missing and in Editor: creates the file and loads it.
    /// 3) If missing and not in Editor: returns "{}".
    /// </summary>
    public static class GlobalsJsonProvider
    {
        /// <summary>
        /// Tries to load the globals JSON from Resources.
        /// </summary>
        private static string TryLoadFromResources()
        {
            var text = Resources.Load<TextAsset>("globals");
            return text != null ? text.text : null ?? "{}";
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
            // 2) In Editor: ensure file exists, refresh, and try again.
            EnsureGlobalsFileExists();

            // Ensure the new/ensured asset is imported, then try Resources again.
            AssetDatabase.Refresh();

            json = TryLoadFromResources();
            if (!string.IsNullOrEmpty(json)) return json;

            // As a last resort in Editor, read the file directly from disk.
            var absPath = GetProjectFilePath();
            if (File.Exists(absPath))
            {
                return File.ReadAllText(absPath) ?? "{}";
            }
#endif
            // 3) Not in Editor (or still missing): return "{}".
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

        /// <summary>
        /// Ensures that the globals file exists with "{}" as default content.
        /// Creates the directory if needed.
        /// </summary>
        private static void EnsureGlobalsFileExists()
        {
            var path = GetProjectFilePath();
            var dir = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(path))
            {
                File.WriteAllText(path, "{}");
                Debug.Log(
                    "[GlobalsJsonProvider] Created missing globals at: " + path
                );
            }
        }
#endif
    }
}
