using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.Steam
{
    /// <summary>
    /// Performs the project-side starter setup flow for the Steam module.
    /// </summary>
    public static class SteamModuleStarterSetup
    {
        /// <summary>
        /// Ensures that steam_appid.txt exists at the project root for local
        /// startup and debugging flows.
        /// </summary>
        /// <returns>
        /// A user-facing status message describing the outcome.
        /// </returns>
        public static string Run()
        {
            string path = GetSteamAppIdFilePath();
            if (File.Exists(path))
            {
                return "steam_appid.txt is already present in the project root.";
            }

            TextAsset textAsset = Resources.Load<TextAsset>("steam_appid");
            if (textAsset == null)
            {
                throw new InvalidOperationException(
                    "The default steam_appid resource could not be loaded."
                );
            }

            File.WriteAllText(path, textAsset.text);
            return "Created steam_appid.txt in the project root for local Steam startup flows.";
        }

        /// <summary>
        /// Resolves the absolute project-root path to steam_appid.txt.
        /// </summary>
        /// <returns>
        /// The absolute file path used by local Steam initialization.
        /// </returns>
        public static string GetSteamAppIdFilePath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "steam_appid.txt"));
        }
    }
}