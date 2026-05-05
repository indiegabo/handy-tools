
using System.IO;
using System.Linq;
using IndieGabo.HandyTools.Logger;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.PackageCreation
{
    /// <summary>
    /// Exports Unity project folders into distributable unitypackage files.
    /// </summary>
    public static class UnityPackageCreator
    {
        /// <summary>
        /// Exports the contents of one folder into a unitypackage file.
        /// </summary>
        /// <param name="folderPath">Folder that contains the assets to export.</param>
        /// <param name="relativeOutputPath">Relative path where the package will be written.</param>
        /// <param name="outputFilename">Output package file name without extension.</param>
        public static void TurnFolderIntoPackage(
            string folderPath,
            string relativeOutputPath,
            string outputFilename
        )
        {
            // 1. Check if folder exists
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError("Error: Folder '" + folderPath + "' does not exist.");
                return;
            }

            // 2. Construct output path (assuming the folder is within the project)
            string outputPath = Path.Combine(
                relativeOutputPath,
                outputFilename + ".unitypackage"
            );

            // 3. Get all assets within the folder
            string[] assetPaths = AssetDatabase.FindAssets("*", new[] { folderPath })
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .ToArray();

            // 5. Create the package
            AssetDatabase.ExportPackage(assetPaths, outputPath, ExportPackageOptions.Recurse);
            AssetDatabase.Refresh();

            HandyLogger.Success(
                $"{nameof(UnityPackageCreator)}",
                $"Package created at: {outputPath}"
            );
        }
    }
}