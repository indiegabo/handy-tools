#nullable enable
#if UNITY_EDITOR
using System.IO;
using IndieGabo.HandyTools.GlobalConfig;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.GlobalConfig
{
    /// <summary>
    /// Editor-only utilities for the GlobalConfig system.
    /// Ensures file existence and sync with AssetDatabase.
    /// </summary>
    public static class GlobalsFileUtility
    {
        #region Ensure

        /// <summary>
        /// Ensures that Assets/Resources/globals exists with "{}" content
        /// if missing. Returns the absolute path.
        /// </summary>
        public static string EnsureGlobalsFileExists()
        {
            var absPath = GlobalsJsonProvider.GetProjectFilePath();
            var dir = Path.GetDirectoryName(absPath);

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(absPath))
            {
                File.WriteAllText(absPath, "{}");
                Debug.Log(
                    "[GlobalsFileUtility] Created Assets/Resources/globals"
                );
                AssetDatabase.Refresh();
            }

            return absPath;
        }

        #endregion
    }
}
#endif
