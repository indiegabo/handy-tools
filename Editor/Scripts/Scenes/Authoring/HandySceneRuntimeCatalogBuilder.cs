using System;
using System.Collections.Generic;
using System.IO;
using IndieGabo.HandyTools.Scenes;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.Scenes.Authoring
{
    /// <summary>
    /// Generates the temporary build-only HandyScene runtime catalog and
    /// provides editor-side in-memory snapshot utilities for unloaded-scene
    /// metadata queries.
    /// </summary>
    public static class HandySceneRuntimeCatalogBuilder
    {
        #region Constants

        /// <summary>
        /// Temporary root folder used to stage the build-only runtime catalog.
        /// </summary>
        public const string BuildCatalogRootFolderPath =
            "Assets/__HandyToolsGenerated/ScenesBuild";

        /// <summary>
        /// Temporary asset path used to stage the build-only runtime catalog.
        /// </summary>
        public const string BuildCatalogAssetPath =
            BuildCatalogRootFolderPath +
            "/Resources/HandyTools/Scenes/HandySceneRuntimeCatalog.asset";

        #endregion

        #region Public API

        /// <summary>
        /// Creates one in-memory runtime catalog snapshot for all marked
        /// HandyScenes.
        /// </summary>
        /// <returns>The generated in-memory runtime catalog snapshot.</returns>
        public static HandySceneRuntimeCatalog CreateCatalogSnapshot()
        {
            return CreateCatalogSnapshot(GetHandyScenePaths());
        }

        /// <summary>
        /// Prepares the temporary build-only runtime catalog asset so the player
        /// build can include it in Resources without leaving a persistent
        /// project artifact behind.
        /// </summary>
        /// <returns>True when the temporary build catalog was created.</returns>
        public static bool PrepareBuildCatalogAsset()
        {
            CleanupBuildCatalogAsset();

            IReadOnlyList<string> handyScenePaths = GetHandyScenePaths();

            Debug.Log(
                "[HandyTools][Scenes][Build] Discovered "
                + handyScenePaths.Count
                + " marked HandyScene asset(s) for runtime catalog staging.");

            HandySceneRuntimeCatalog catalog = CreateCatalogSnapshot(handyScenePaths);
            if (catalog == null)
            {
                return false;
            }

            EnsureBuildCatalogFolderExists();
            AssetDatabase.CreateAsset(catalog, BuildCatalogAssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[HandyTools][Scenes][Build] Staged "
                + catalog.Entries.Count
                + " HandyScene runtime snapshot entr"
                + (catalog.Entries.Count == 1 ? "y" : "ies")
                + " from "
                + handyScenePaths.Count
                + " marked HandyScene asset(s). Output root: '"
                + GetAbsoluteAssetPath(BuildCatalogRootFolderPath)
                + "'. Catalog: '"
                + GetAbsoluteAssetPath(BuildCatalogAssetPath)
                + "'. Resource path: '"
                + HandySceneRuntimeCatalog.ResourcePath
                + "'.");

            return true;
        }

        /// <summary>
        /// Removes any temporary build-only runtime catalog asset that may have
        /// been left behind by a previous build preparation pass.
        /// </summary>
        /// <param name="logResult">True to log the cleanup outcome.</param>
        public static void CleanupBuildCatalogAsset(bool logResult = false)
        {
            bool hadBuildCatalogRoot = AssetDatabase.IsValidFolder(
                BuildCatalogRootFolderPath);
            bool hadBuildCatalogAsset =
                AssetDatabase.LoadAssetAtPath<HandySceneRuntimeCatalog>(
                    BuildCatalogAssetPath)
                != null;

            if (AssetDatabase.IsValidFolder(BuildCatalogRootFolderPath))
            {
                AssetDatabase.DeleteAsset(BuildCatalogRootFolderPath);
            }

            DeleteFolderIfEmpty("Assets/__HandyToolsGenerated");
            AssetDatabase.Refresh();

            if (!logResult)
            {
                return;
            }

            bool hadGeneratedArtifacts = hadBuildCatalogRoot || hadBuildCatalogAsset;

            Debug.Log(
                "[HandyTools][Scenes][Build] "
                + (hadGeneratedArtifacts
                    ? "Cleaned up staged HandyScene runtime catalog artifacts."
                    : "No staged HandyScene runtime catalog artifacts were present for cleanup.")
                + " Output root: '"
                + GetAbsoluteAssetPath(BuildCatalogRootFolderPath)
                + "'. Catalog: '"
                + GetAbsoluteAssetPath(BuildCatalogAssetPath)
                + "'.");
        }

        /// <summary>
        /// Creates one editor snapshot entry for the provided scene.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <param name="entry">Generated in-memory snapshot entry.</param>
        /// <returns>True when the snapshot entry could be created.</returns>
        public static bool TryCreateEntry(
            string sceneAssetPath,
            out HandySceneRuntimeCatalogEntry entry)
        {
            return HandySceneEditorSnapshotUtility.TryCreateCatalogEntry(
                sceneAssetPath,
                out entry);
        }

        /// <summary>
        /// Validates that the current set of marked HandyScenes can be
        /// snapshotted for runtime fallback use.
        /// </summary>
        [MenuItem("HandyTools/Scenes/Validate Runtime Snapshots")]
        public static void ValidateRuntimeSnapshotsFromMenu()
        {
            HandySceneRuntimeCatalog catalog = CreateCatalogSnapshot();

            if (catalog == null)
            {
                return;
            }

            try
            {
                Debug.Log(
                    $"Validated {catalog.Entries.Count} HandyScene runtime " +
                    "snapshot entries.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        #endregion

        #region Helpers

        [InitializeOnLoadMethod]
        private static void CleanupStaleBuildCatalogOnDomainLoad()
        {
            CleanupBuildCatalogAsset();
        }

        private static HandySceneRuntimeCatalog CreateCatalogSnapshot(
            IReadOnlyList<string> handyScenePaths)
        {
            return HandySceneEditorSnapshotUtility.CreateCatalogSnapshot(
                handyScenePaths ?? Array.Empty<string>());
        }

        private static void EnsureBuildCatalogFolderExists()
        {
            EnsureFolder("Assets/__HandyToolsGenerated");
            EnsureFolder(BuildCatalogRootFolderPath);
            EnsureFolder(BuildCatalogRootFolderPath + "/Resources");
            EnsureFolder(BuildCatalogRootFolderPath + "/Resources/HandyTools");
            EnsureFolder(BuildCatalogRootFolderPath + "/Resources/HandyTools/Scenes");
        }

        private static IReadOnlyList<string> GetHandyScenePaths()
        {
            List<string> scenePaths = new();
            string[] sceneGuids = AssetDatabase.FindAssets("t:SceneAsset");

            for (int index = 0; index < sceneGuids.Length; index++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[index]);
                if (HandySceneMetadataStore.IsHandyScene(scenePath))
                {
                    scenePaths.Add(scenePath);
                }
            }

            scenePaths.Sort(StringComparer.Ordinal);
            return scenePaths;
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            return string.IsNullOrWhiteSpace(assetPath)
                ? string.Empty
                : Path.GetFullPath(assetPath);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int separatorIndex = folderPath.LastIndexOf('/');
            if (separatorIndex <= 0)
            {
                return;
            }

            string parentFolder = folderPath.Substring(0, separatorIndex);
            string folderName = folderPath[(separatorIndex + 1)..];

            EnsureFolder(parentFolder);

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        private static void DeleteFolderIfEmpty(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            if (AssetDatabase.GetSubFolders(folderPath).Length > 0)
            {
                return;
            }

            string absoluteFolderPath = Path.GetFullPath(folderPath);

            if (Directory.Exists(absoluteFolderPath))
            {
                string[] files = Directory.GetFiles(
                    absoluteFolderPath,
                    "*",
                    SearchOption.TopDirectoryOnly);

                for (int index = 0; index < files.Length; index++)
                {
                    if (!files[index].EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            AssetDatabase.DeleteAsset(folderPath);
        }

        #endregion
    }
}