using System.IO;
using IndieGabo.HandyTools.Editor.PackageCreation;
using IndieGabo.HandyTools.Logger;
using UnityEditor;

namespace IndieGabo.HandyTools.Editor.StarterPackages
{
    /// <summary>
    /// Provides shared resolution and creation helpers for module-specific
    /// starter unitypackages.
    /// </summary>
    public static class HandyStarterPackageUtility
    {
#if HANDY_TOOLS_DEVELOPMENT
        /// <summary>
        /// Rebuilds a module-specific starter unitypackage from the provided
        /// project-owned source folder.
        /// </summary>
        /// <param name="projectFolder">
        /// Project-relative source folder that contains the starter assets.
        /// </param>
        /// <param name="developmentPackageRelativePath">
        /// Project-relative folder where the exported unitypackage should be
        /// written during development.
        /// </param>
        /// <param name="packageName">
        /// Output unitypackage file name without extension.
        /// </param>
        public static void CreatePackage(
            string projectFolder,
            string developmentPackageRelativePath,
            string packageName
        )
        {
            string packagePath = $"{developmentPackageRelativePath}/{packageName}.unitypackage";
            if (File.Exists(packagePath))
            {
                AssetDatabase.DeleteAsset(packagePath);
                AssetDatabase.Refresh();
            }

            UnityPackageCreator.TurnFolderIntoPackage(
                projectFolder,
                developmentPackageRelativePath,
                packageName
            );
        }
#endif

        /// <summary>
        /// Tries to resolve a starter unitypackage path across the provided
        /// candidate folders.
        /// </summary>
        /// <param name="packageName">
        /// Starter package file name without extension.
        /// </param>
        /// <param name="candidateFolders">
        /// Candidate project-relative folders where the unitypackage may live.
        /// </param>
        /// <param name="path">
        /// Receives the resolved project-relative path when the package is
        /// available.
        /// </param>
        /// <returns>
        /// True when the unitypackage asset could be resolved.
        /// </returns>
        public static bool TryGetPackageAssetPath(
            string packageName,
            string[] candidateFolders,
            out string path
        )
        {
            path = ResolvePackageAssetPath(packageName, candidateFolders);
            return !string.IsNullOrEmpty(path);
        }

        /// <summary>
        /// Resolves the unitypackage asset path across development and
        /// installed-package layouts.
        /// </summary>
        /// <param name="packageName">
        /// Starter package file name without extension.
        /// </param>
        /// <param name="candidateFolders">
        /// Candidate project-relative folders where the unitypackage may live.
        /// </param>
        /// <returns>
        /// A project-relative path to the starter unitypackage, or an
        /// empty string when the asset cannot be resolved.
        /// </returns>
        private static string ResolvePackageAssetPath(
            string packageName,
            string[] candidateFolders
        )
        {
            foreach (string folder in candidateFolders)
            {
                string candidatePath = $"{folder}/{packageName}.unitypackage";
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            string[] guids = AssetDatabase.FindAssets(packageName);
            if (guids.Length > 0)
            {
                string resolvedPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    return resolvedPath;
                }
            }

            HandyLogger.Warning(
                nameof(HandyStarterPackageUtility),
                $"The starter package could not be resolved for: {packageName}"
            );
            return string.Empty;
        }
    }
}