using UnityEngine;
using UnityEditor;
using System.IO;
using IndieGabo.HandyTools.Editor.PackageCreation;
using IndieGabo.HandyTools.Logger;

namespace IndieGabo.HandyTools.Editor.Essentials
{
    public static class EssentialsPackage
    {

#pragma warning disable CS0414 // Rethrow to preserve stack details

        public readonly static string PackageFolder = "Assets/_Project";
        static readonly string PackageRelativePath = "Assets/HandyTools/Resources";
        static readonly string PackageName = "HandyEssentials";

#pragma warning restore CS0414 // Rethrow to preserve stack details

#if HANDY_TOOLS_DEVELOPMENT
        [MenuItem("HandyTools/Development/Create Essentials Package", false, 9999)]
        public static void CreatePackage()
        {
            string[] guids = AssetDatabase.FindAssets(PackageName);
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.Refresh();
            }

            UnityPackageCreator.TurnFolderIntoPackage(
                PackageFolder,
                PackageRelativePath,
                PackageName
            );
        }
#endif

        [MenuItem("HandyTools/Essential Package/Import", false, 101)]
        public static void ImportPackage()
        {
            string[] guids = AssetDatabase.FindAssets(PackageName);
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                AssetDatabase.ImportPackage(path, false);
            }
            else
            {
                HandyLogger.Error(
                    $"{nameof(EssentialsPackage)}",
                    $"The Essentials package could not be found."
                );
            }
        }
    }
}