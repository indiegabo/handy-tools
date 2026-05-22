using System;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.Scenes.Authoring
{
    /// <summary>
    /// Reads and writes HandyScene metadata through the scene importer userData
    /// field.
    /// </summary>
    public static class HandySceneMetadataStore
    {
        #region Constants

        private const string PayloadPrefix = "HandyTools.Scenes::";

        #endregion

        #region Public API

        /// <summary>
        /// Gets whether the provided scene path contains one valid HandyScene
        /// marker.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <returns>True when the scene is marked as a HandyScene.</returns>
        public static bool IsHandyScene(string sceneAssetPath)
        {
            return TryGetStoredPayload(sceneAssetPath, out _);
        }

        /// <summary>
        /// Attempts to resolve the serialized HandyScene payload stored on the
        /// scene importer.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <param name="storedPayload">Resolved payload without the prefix.</param>
        /// <returns>True when the scene importer contains a HandyScene payload.</returns>
        public static bool TryGetStoredPayload(
            string sceneAssetPath,
            out string storedPayload)
        {
            storedPayload = string.Empty;

            if (!TryGetSceneImporter(sceneAssetPath, out AssetImporter importer))
            {
                return false;
            }

            string userData = importer.userData ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userData)
                || !userData.StartsWith(PayloadPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            storedPayload = userData.Substring(PayloadPrefix.Length);
            return true;
        }

        /// <summary>
        /// Marks one scene as a HandyScene, creating the metadata envelope when
        /// needed.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <returns>True when the scene metadata was written successfully.</returns>
        public static bool MarkAsHandyScene(string sceneAssetPath)
        {
            if (!TryGetSceneImporter(sceneAssetPath, out AssetImporter importer))
            {
                return false;
            }

            if (string.Equals(importer.userData, PayloadPrefix, StringComparison.Ordinal))
            {
                return true;
            }

            return TryWriteSceneImporterUserData(
                sceneAssetPath,
                importer,
                PayloadPrefix)
                && IsHandyScene(sceneAssetPath);
        }

        /// <summary>
        /// Removes the HandyScene payload owned by this prototype from one
        /// scene importer.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <returns>True when the payload was cleared successfully.</returns>
        public static bool ClearHandySceneMetadata(string sceneAssetPath)
        {
            if (!TryGetSceneImporter(sceneAssetPath, out AssetImporter importer))
            {
                return false;
            }

            string userData = importer.userData ?? string.Empty;
            if (!userData.StartsWith(PayloadPrefix, StringComparison.Ordinal))
            {
                return true;
            }

            return TryWriteSceneImporterUserData(
                sceneAssetPath,
                importer,
                string.Empty)
                && !IsHandyScene(sceneAssetPath);
        }

        /// <summary>
        /// Synchronizes the internal HandyScene importer marker with the
        /// current metadata activation state.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <param name="isEnabled">
        /// Whether the scene currently owns at least one active metadata
        /// section.
        /// </param>
        /// <returns>True when the importer marker matches the requested state.</returns>
        public static bool SetHandySceneEnabled(string sceneAssetPath, bool isEnabled)
        {
            return isEnabled
            ? MarkAsHandyScene(sceneAssetPath)
            : ClearHandySceneMetadata(sceneAssetPath);
        }

        private static bool TryWriteSceneImporterUserData(
            string sceneAssetPath,
            AssetImporter importer,
            string userData)
        {
            if (importer == null)
            {
                return false;
            }

            try
            {
                importer.userData = userData ?? string.Empty;
                EditorUtility.SetDirty(importer);
                _ = AssetDatabase.WriteImportSettingsIfDirty(sceneAssetPath);
                AssetDatabase.ImportAsset(
                    sceneAssetPath,
                    ImportAssetOptions.ForceSynchronousImport);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not update the HandyScene importer metadata for " +
                    $"'{sceneAssetPath}'. {exception.Message}");
                return false;
            }
        }

        private static bool TryGetSceneImporter(
            string sceneAssetPath,
            out AssetImporter importer)
        {
            importer = null;

            if (string.IsNullOrWhiteSpace(sceneAssetPath)
                || !sceneAssetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            importer = AssetImporter.GetAtPath(sceneAssetPath);
            return importer != null;
        }

        #endregion
    }
}