using UnityEngine;
using UnityEditor;
using System.IO;
using IndieGabo.HandyTools.Logger;
using IndieGabo.HandyTools.Editor.Essentials;
using IndieGabo.HandyTools.HandyInputSystem;
using System;
using System.Threading.Tasks;

namespace IndieGabo.HandyTools.Editor.ProjectSetup
{
    /// <summary>
    /// Performs one-time HandyTools project setup tasks and keeps managed
    /// scripting defines in a valid state for the current project.
    /// </summary>
    [InitializeOnLoad]
    public static class Setuper
    {
        static Setuper()
        {
            HandyScriptingDefineUtility.RemoveUnavailableDefines();

            string anchorFilePath = AnchorFilePath;
            if (File.Exists(anchorFilePath)) return;
            Setup();
        }

        /// <summary>
        /// Executes the default HandyTools project setup flow.
        /// </summary>
        [MenuItem("HandyTools/Complete Setup", false, 1000)]
        public static void Setup()
        {
            File.WriteAllText(AnchorFilePath, "");
            EssentialsPackage.ImportPackage();
            AssetDatabase.Refresh();
            InjectPlayerManagerFromEssentials();
            CopySteamAppIdFile();
            HandyScriptingDefineUtility.ApplySetupDefaults();
            EditorUtility.RequestScriptReload();
        }

        /// <summary>
        /// Injects the default PlayerManager prefab from the essentials package
        /// into the project input configuration.
        /// </summary>
        private static async void InjectPlayerManagerFromEssentials()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            PlayerManager playerManager = FindPlayerManager();
            if (playerManager == null)
            {
                HandyLogger.Warning(
                    $"{nameof(Setuper)}",
                    $"The {nameof(PlayerManager)} asset could not be found. You will need to "
                    + "manually add it to the project"
                );
                return;
            }

            var inputConfig = ProjectInputConfig.Get();
            inputConfig.PlayerManagerPrefab = playerManager;
            EditorUtility.SetDirty(inputConfig);
            AssetDatabase.SaveAssetIfDirty(inputConfig);
        }

        /// <summary>
        /// Finds the PlayerManager prefab imported from the essentials package.
        /// </summary>
        /// <returns>
        /// The resolved PlayerManager prefab, or null when it cannot be found.
        /// </returns>
        public static PlayerManager FindPlayerManager()
        {
            string folder = Path.Combine(EssentialsPackage.PackageFolder, "Input");
            string[] guids = AssetDatabase.FindAssets(
                $"Player Manager",
                new string[] { folder }
            );

            if (guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<PlayerManager>(path);
        }

        /// <summary>
        /// Copies the default Steam App ID file to the project root when it is
        /// not present yet.
        /// </summary>
        public static void CopySteamAppIdFile()
        {
            string path = Path.Combine(Application.dataPath, "..", "steam_appid.txt");
            if (File.Exists(path)) return;

            string contents = Resources.Load<TextAsset>("steam_appid").text;
            File.WriteAllText(path, contents);
        }

        /// <summary>
        /// Gets the anchor file that marks the project as already initialized.
        /// </summary>
        public static string AnchorFilePath =>
                Path.GetFullPath(Path.Combine(
                    Application.dataPath, "..", ".handy-anchor"
                ));
    }
}