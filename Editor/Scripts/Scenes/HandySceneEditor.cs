using System;
using IndieGabo.HandyTools.Editor.Scenes.Authoring;
using IndieGabo.HandyTools.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndieGabo.HandyTools.Editor.Scenes
{
    /// <summary>
    /// Exposes the public editor-side HandyScene API used by custom section
    /// tooling to mark scenes and open authoring sessions.
    /// </summary>
    public static class HandySceneEditor
    {
        #region Public API

        /// <summary>
        /// Creates one new HandyScene asset at the provided project-relative
        /// path and initializes its persisted carrier data.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative `.unity` path to create.</param>
        /// <param name="sceneReference">Reference to the created HandyScene.</param>
        /// <returns>True when the HandyScene was created successfully.</returns>
        public static bool CreateHandyScene(
            string sceneAssetPath,
            out HandySceneReference sceneReference)
        {
            sceneReference = null;

            if (!IsCreatableScenePath(sceneAssetPath)
                || !TryCreateSceneAsset(sceneAssetPath)
                || !InitializeCreatedScene(sceneAssetPath))
            {
                CleanupFailedCreatedScene(sceneAssetPath);
                return false;
            }

            AssetDatabase.ImportAsset(sceneAssetPath);
            sceneReference = HandySceneReference.FromSceneAssetPath(sceneAssetPath);
            return sceneReference != null && sceneReference.IsAssigned;
        }

        /// <summary>
        /// Gets whether the provided reference points to one marked HandyScene.
        /// </summary>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <returns>True when the target scene is marked as a HandyScene.</returns>
        public static bool IsHandyScene(HandySceneReference sceneReference)
        {
            return TryGetSceneAssetPath(sceneReference, out string sceneAssetPath)
                && HandySceneMetadataStore.IsHandyScene(sceneAssetPath);
        }

        /// <summary>
        /// Marks the referenced scene as a HandyScene.
        /// </summary>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <returns>True when the scene was marked successfully.</returns>
        public static bool MarkAsHandyScene(HandySceneReference sceneReference)
        {
            if (!TryOpenAuthoringSession(
                    sceneReference,
                    out HandySceneAuthoringSession session))
            {
                return false;
            }

            using (session)
            {
                session.ActivateAllKnownSections();
                return session.Save();
            }
        }

        /// <summary>
        /// Activates one section on the referenced scene.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <returns>True when the section was activated successfully.</returns>
        public static bool ActivateSection<TSection>(HandySceneReference sceneReference)
            where TSection : SceneExtender
        {
            return ActivateSection(sceneReference, typeof(TSection));
        }

        /// <summary>
        /// Activates one section on the referenced scene.
        /// </summary>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <param name="sectionId">Stable section identifier.</param>
        /// <returns>True when the section was activated successfully.</returns>
        public static bool ActivateSection(
            HandySceneReference sceneReference,
            string sectionId)
        {
            if (!TryOpenAuthoringSession(
                    sceneReference,
                    out HandySceneAuthoringSession session))
            {
                return false;
            }

            using (session)
            {
                return session.ActivateSection(sectionId) && session.Save();
            }
        }

        /// <summary>
        /// Deactivates one section on the referenced scene.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <returns>True when the section was deactivated successfully.</returns>
        public static bool DeactivateSection<TSection>(HandySceneReference sceneReference)
            where TSection : SceneExtender
        {
            return DeactivateSection(sceneReference, typeof(TSection));
        }

        /// <summary>
        /// Deactivates one section on the referenced scene.
        /// </summary>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <param name="sectionId">Stable section identifier.</param>
        /// <returns>True when the section was deactivated successfully.</returns>
        public static bool DeactivateSection(
            HandySceneReference sceneReference,
            string sectionId)
        {
            if (!TryOpenAuthoringSession(
                    sceneReference,
                    out HandySceneAuthoringSession session))
            {
                return false;
            }

            using (session)
            {
                return session.DeactivateSection(sectionId) && session.Save();
            }
        }

        /// <summary>
        /// Unmarks the referenced scene as a HandyScene.
        /// </summary>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <returns>True when the scene was unmarked successfully.</returns>
        public static bool UnmarkAsHandyScene(HandySceneReference sceneReference)
        {
            if (!TryOpenAuthoringSession(
                    sceneReference,
                    out HandySceneAuthoringSession session))
            {
                return false;
            }

            using (session)
            {
                session.DeactivateAllSections();
                return session.Save();
            }
        }

        /// <summary>
        /// Opens one authoring session for the referenced HandyScene.
        /// </summary>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <returns>The opened authoring session when available.</returns>
        public static HandySceneAuthoringSession OpenAuthoringSession(
            HandySceneReference sceneReference)
        {
            return TryOpenAuthoringSession(sceneReference, out HandySceneAuthoringSession session)
                ? session
                : null;
        }

        /// <summary>
        /// Attempts to open one authoring session for the referenced
        /// HandyScene.
        /// </summary>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <param name="session">Opened authoring session.</param>
        /// <returns>True when the scene is assigned and marked as a HandyScene.</returns>
        public static bool TryOpenAuthoringSession(
            HandySceneReference sceneReference,
            out HandySceneAuthoringSession session)
        {
            session = null;

            if (!TryGetSceneAssetPath(sceneReference, out string sceneAssetPath))
            {
                return false;
            }

            session = HandySceneAuthoringSession.Open(sceneAssetPath);
            return session != null;
        }

        #endregion

        #region Helpers

        private static bool InitializeCreatedScene(string sceneAssetPath)
        {
            using HandySceneAuthoringSession session =
                HandySceneAuthoringSession.Open(sceneAssetPath);

            if (session == null)
            {
                return false;
            }

            session.ActivateAllKnownSections();
            return session.Save();
        }

        private static bool TryCreateSceneAsset(string sceneAssetPath)
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene transientScene = default;

            try
            {
                transientScene = EditorSceneManager.NewScene(
                    NewSceneSetup.DefaultGameObjects,
                    NewSceneMode.Additive);

                return transientScene.IsValid()
                    && transientScene.isLoaded
                    && EditorSceneManager.SaveScene(transientScene, sceneAssetPath);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not create the HandyScene '{sceneAssetPath}'. " +
                    exception.Message);
                return false;
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    _ = SceneManager.SetActiveScene(previousActiveScene);
                }

                if (transientScene.IsValid() && transientScene.isLoaded)
                {
                    _ = EditorSceneManager.CloseScene(transientScene, true);
                }
            }
        }

        private static void CleanupFailedCreatedScene(string sceneAssetPath)
        {
            if (string.IsNullOrWhiteSpace(sceneAssetPath)
                || AssetDatabase.LoadMainAssetAtPath(sceneAssetPath) == null)
            {
                return;
            }

            _ = AssetDatabase.DeleteAsset(sceneAssetPath);
            AssetDatabase.Refresh();
        }

        private static bool IsCreatableScenePath(string sceneAssetPath)
        {
            return !string.IsNullOrWhiteSpace(sceneAssetPath)
                && sceneAssetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                && sceneAssetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                && AssetDatabase.LoadMainAssetAtPath(sceneAssetPath) == null;
        }

        private static bool TryGetSceneAssetPath(
            HandySceneReference sceneReference,
            out string sceneAssetPath)
        {
            sceneAssetPath = sceneReference?.SceneAssetPath ?? string.Empty;
            return !string.IsNullOrWhiteSpace(sceneAssetPath)
                && sceneAssetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ActivateSection(
            HandySceneReference sceneReference,
            Type sectionType)
        {
            if (!TryResolveSectionDescriptor(
                    sceneReference,
                    sectionType,
                    out HandySceneSectionTypeCache.SectionDescriptor descriptor))
            {
                return false;
            }

            return ActivateSection(sceneReference, descriptor.SectionId);
        }

        private static bool DeactivateSection(
            HandySceneReference sceneReference,
            Type sectionType)
        {
            if (!TryResolveSectionDescriptor(
                    sceneReference,
                    sectionType,
                    out HandySceneSectionTypeCache.SectionDescriptor descriptor))
            {
                return false;
            }

            return DeactivateSection(sceneReference, descriptor.SectionId);
        }

        private static bool TryResolveSectionDescriptor(
            HandySceneReference sceneReference,
            Type sectionType,
            out HandySceneSectionTypeCache.SectionDescriptor descriptor)
        {
            descriptor = default;

            if (sectionType == null
                || !TryGetSceneAssetPath(sceneReference, out string sceneAssetPath))
            {
                return false;
            }

            var descriptors = HandySceneSectionTypeCache.GetDescriptors(sceneAssetPath);
            for (int index = 0; index < descriptors.Count; index++)
            {
                HandySceneSectionTypeCache.SectionDescriptor candidate = descriptors[index];
                if (candidate.Type == sectionType)
                {
                    descriptor = candidate;
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}