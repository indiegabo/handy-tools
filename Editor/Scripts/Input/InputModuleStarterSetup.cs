using System;
using System.IO;
using IndieGabo.HandyTools.Editor.StarterPackages;
using IndieGabo.HandyTools.HandyInputSystem;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.Input
{
    /// <summary>
    /// Performs the project-side starter setup flow for the Input module.
    /// </summary>
    public static class InputModuleStarterSetup
    {
        public const string ProjectFolder = "Assets/_Project/Input";

        private const string _packageName = "HandyInputStarter";
        private const string _developmentPackageRelativePath =
            "Assets/HandyTools/Runtime/Resources";
        private const string _installedPackageRelativePath =
            "Packages/com.indiegabo.handy-tools/Runtime/Resources";

        private static readonly string[] _candidatePackageFolders =
        {
            _developmentPackageRelativePath,
            _installedPackageRelativePath,
        };

        private static bool _isImportPending;
        private static bool _requestScriptReloadAfterCompletion;

#if HANDY_TOOLS_DEVELOPMENT
        /// <summary>
        /// Rebuilds the distributable Input starter unitypackage from the
        /// project-owned starter folder.
        /// </summary>
        [MenuItem("HandyTools/Development/Create Input Starter Package", false, 9999)]
        public static void CreateStarterPackage()
        {
            HandyStarterPackageUtility.CreatePackage(
                ProjectFolder,
                _developmentPackageRelativePath,
                _packageName
            );
        }
#endif

        /// <summary>
        /// Imports or resolves the default Input starter assets and assigns
        /// the PlayerManager prefab to the module configuration.
        /// </summary>
        /// <param name="requestScriptReloadAfterCompletion">
        /// When true, requests a script reload after the setup flow finishes.
        /// </param>
        /// <returns>
        /// A user-facing status message describing the action that ran.
        /// </returns>
        public static string Run(bool requestScriptReloadAfterCompletion = false)
        {
            _requestScriptReloadAfterCompletion |= requestScriptReloadAfterCompletion;

            PlayerManager playerManager = FindPlayerManager();
            if (playerManager != null)
            {
                AssignPlayerManager(playerManager);
                MaybeRequestScriptReload();
                return $"Assigned the existing {nameof(PlayerManager)} prefab from "
                    + $"{ProjectFolder}.";
            }

            if (_isImportPending)
            {
                return "Input starter setup is already importing the default project assets.";
            }

            if (!HandyStarterPackageUtility.TryGetPackageAssetPath(
                _packageName,
                _candidatePackageFolders,
                out string packagePath
            ))
            {
                throw new InvalidOperationException(
                    "The Input starter package could not be resolved."
                );
            }

            RegisterImportCallbacks();
            _isImportPending = true;
            AssetDatabase.ImportPackage(packagePath, false);

            return "Importing the default Input starter assets into Assets/_Project/Input. "
                + "The Player Manager prefab will be assigned automatically when the import completes.";
        }

        /// <summary>
        /// Finds the PlayerManager prefab created by the Input starter setup.
        /// </summary>
        /// <returns>
        /// The resolved PlayerManager prefab, or null when it cannot be
        /// found in the project starter folder.
        /// </returns>
        public static PlayerManager FindPlayerManager()
        {
            string[] guids = AssetDatabase.FindAssets(
                "Player Manager",
                new[] { ProjectFolder }
            );

            if (guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<PlayerManager>(path);
        }

        private static void RegisterImportCallbacks()
        {
            AssetDatabase.importPackageCompleted += OnImportCompleted;
            AssetDatabase.importPackageCancelled += OnImportCancelled;
            AssetDatabase.importPackageFailed += OnImportFailed;
        }

        private static void UnregisterImportCallbacks()
        {
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageCancelled -= OnImportCancelled;
            AssetDatabase.importPackageFailed -= OnImportFailed;
        }

        private static void OnImportCompleted(string packageName)
        {
            if (!_isImportPending)
            {
                return;
            }

            _ = packageName;
            _isImportPending = false;
            UnregisterImportCallbacks();

            PlayerManager playerManager = FindPlayerManager();
            if (playerManager == null)
            {
                Debug.LogWarning(
                    $"{nameof(InputModuleStarterSetup)} could not resolve the "
                    + $"{nameof(PlayerManager)} prefab after importing the starter assets."
                );
                MaybeRequestScriptReload();
                return;
            }

            AssignPlayerManager(playerManager);
            MaybeRequestScriptReload();
        }

        private static void OnImportCancelled(string packageName)
        {
            if (!_isImportPending)
            {
                return;
            }

            _ = packageName;
            _isImportPending = false;
            UnregisterImportCallbacks();
            MaybeRequestScriptReload();
        }

        private static void OnImportFailed(string packageName, string error)
        {
            if (!_isImportPending)
            {
                return;
            }

            _ = packageName;
            _isImportPending = false;
            UnregisterImportCallbacks();
            Debug.LogError(error);
            MaybeRequestScriptReload();
        }

        private static void AssignPlayerManager(PlayerManager playerManager)
        {
            ProjectInputConfig.ReloadInstance();
            ProjectInputConfig inputConfig = ProjectInputConfig.GetOrCreateForEditor();
            inputConfig.PlayerManagerPrefab = playerManager;
            EditorUtility.SetDirty(inputConfig);
            AssetDatabase.SaveAssetIfDirty(inputConfig);
            AssetDatabase.SaveAssets();
        }

        private static void MaybeRequestScriptReload()
        {
            if (!_requestScriptReloadAfterCompletion)
            {
                return;
            }

            _requestScriptReloadAfterCompletion = false;
            EditorUtility.RequestScriptReload();
        }
    }
}