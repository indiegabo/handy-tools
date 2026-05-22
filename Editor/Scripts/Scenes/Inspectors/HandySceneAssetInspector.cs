using IndieGabo.HandyTools.Editor.Scenes.Authoring;
using UnityEditor;
using UnityEngine;

using UnityEditorEditor = UnityEditor.Editor;
using HandyScenesRuntime = IndieGabo.HandyTools.Scenes;

namespace IndieGabo.HandyTools.Editor.Scenes.Inspectors
{
    /// <summary>
    /// Hosts the first HandyScene inspector workflow inside the selected
    /// SceneAsset inspector using Unity's finishedDefaultHeaderGUI hook.
    /// </summary>
    [InitializeOnLoad]
    public static class HandySceneAssetInspector
    {
        #region State

        private static HandySceneAuthoringSession _cachedAuthoringSession;
        private static string _cachedSceneAssetPath = string.Empty;

        #endregion

        #region Constructors

        /// <summary>
        /// Registers the scene-asset inspector hook once the editor assembly is
        /// loaded.
        /// </summary>
        static HandySceneAssetInspector()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= ClearCachedState;
            AssemblyReloadEvents.beforeAssemblyReload += ClearCachedState;
            UnityEditorEditor.finishedDefaultHeaderGUI -=
                HandleFinishedDefaultHeaderGui;
            UnityEditorEditor.finishedDefaultHeaderGUI +=
                HandleFinishedDefaultHeaderGui;
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Draws the HandyScene probe section for persistent SceneAsset
        /// selections.
        /// </summary>
        /// <param name="editor">Editor currently drawing the inspector.</param>
        private static void HandleFinishedDefaultHeaderGui(UnityEditorEditor editor)
        {
            if (!TryGetSceneAssetPath(editor, out string sceneAssetPath))
            {
                return;
            }

            bool previousGuiEnabled = GUI.enabled;

            try
            {
                GUI.enabled = true;
                DrawInspectorForScene(sceneAssetPath);
            }
            finally
            {
                GUI.enabled = previousGuiEnabled;
            }
        }

        #endregion

        #region Helpers

        private static void ClearCachedState()
        {
            if (_cachedAuthoringSession != null)
            {
                _cachedAuthoringSession.Dispose();
            }

            _cachedAuthoringSession = null;
            _cachedSceneAssetPath = string.Empty;
        }

        private static void DrawInspectorForScene(string sceneAssetPath)
        {
            bool hasActiveSections = HandySceneMetadataStore.IsHandyScene(sceneAssetPath);

            if (!hasActiveSections)
            {
                ClearCachedState();
                DrawInactiveHandySceneSection(sceneAssetPath);
            }
            else
            {
                DrawActiveHandySceneSection(sceneAssetPath);
            }
        }

        /// <summary>
        /// Resolves the selected SceneAsset path for one persistent inspector
        /// target.
        /// </summary>
        /// <param name="editor">Editor currently drawing the inspector.</param>
        /// <param name="sceneAssetPath">Resolved scene asset path.</param>
        /// <returns>
        /// True when the editor is drawing one persistent SceneAsset target.
        /// </returns>
        private static bool TryGetSceneAssetPath(
            UnityEditorEditor editor,
            out string sceneAssetPath
        )
        {
            sceneAssetPath = string.Empty;

            if (editor == null || editor.targets == null || editor.targets.Length != 1)
            {
                return false;
            }

            if (editor.target is not SceneAsset)
            {
                return false;
            }

            if (!EditorUtility.IsPersistent(editor.target))
            {
                return false;
            }

            sceneAssetPath = AssetDatabase.GetAssetPath(editor.target);
            return !string.IsNullOrWhiteSpace(sceneAssetPath) &&
                sceneAssetPath.EndsWith(".unity");
        }

        /// <summary>
        /// Draws the conversion affordance for one regular Unity scene.
        /// </summary>
        /// <param name="sceneAssetPath">Path of the selected SceneAsset.</param>
        private static void DrawInactiveHandySceneSection(string sceneAssetPath)
        {
            EditorGUILayout.Space(4f);

            var descriptors = HandySceneSectionTypeCache.GetDescriptors(sceneAssetPath);

            for (int index = 0; index < descriptors.Count; index++)
            {
                HandySceneSectionTypeCache.SectionDescriptor descriptor = descriptors[index];

                if (GUILayout.Button($"Activate {descriptor.DisplayName}"))
                {
                    HandyScenesRuntime.HandySceneReference sceneReference =
                        HandyScenesRuntime.HandySceneReference.FromSceneAssetPath(
                            sceneAssetPath);

                    if (HandySceneEditor.ActivateSection(
                            sceneReference,
                            descriptor.SectionId))
                    {
                        ClearCachedState();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        /// <summary>
        /// Draws the active HandyScene authoring surface for one marked scene.
        /// </summary>
        /// <param name="sceneAssetPath">Path of the selected SceneAsset.</param>
        private static void DrawActiveHandySceneSection(string sceneAssetPath)
        {
            HandySceneAuthoringSession authoringSession =
                GetOrCreateAuthoringSession(sceneAssetPath);

            if (!authoringSession.Update())
            {
                ClearCachedState();
                authoringSession = GetOrCreateAuthoringSession(sceneAssetPath);

                if (!authoringSession.Update())
                {
                    return;
                }
            }

            EditorGUILayout.Space(4f);

            SerializedProperty sectionsProperty =
                authoringSession.SerializedCarrier.FindProperty("_sections");

            for (int index = 0; index < authoringSession.Carrier.SectionCount; index++)
            {
                DrawSectionProperty(
                    authoringSession,
                    sectionsProperty,
                    index,
                    authoringSession.GetSectionDisplayName(index),
                    authoringSession.Carrier.GetSectionId(index));
            }

            DrawInactiveActivationButtons(sceneAssetPath, authoringSession);

            bool serializedChangesApplied =
                authoringSession.SerializedCarrier.ApplyModifiedProperties();
            authoringSession.NotifySerializedChanges(serializedChangesApplied);

            DrawActionButtons(authoringSession);
        }

        private static HandySceneAuthoringSession GetOrCreateAuthoringSession(
            string sceneAssetPath)
        {
            if (_cachedAuthoringSession != null
                && string.Equals(
                    _cachedSceneAssetPath,
                    sceneAssetPath,
                    System.StringComparison.Ordinal)
                && _cachedAuthoringSession.MatchesCurrentEditorState)
            {
                return _cachedAuthoringSession;
            }

            ClearCachedState();

            _cachedAuthoringSession = HandySceneAuthoringSession.Open(sceneAssetPath);
            _cachedSceneAssetPath = sceneAssetPath;
            return _cachedAuthoringSession;
        }

        private static void DrawSectionProperty(
            HandySceneAuthoringSession authoringSession,
            SerializedProperty sectionsProperty,
            int index,
            string displayName,
            string sectionId)
        {
            if (sectionsProperty == null || index >= sectionsProperty.arraySize)
            {
                return;
            }

            using EditorGUILayout.VerticalScope scope =
                new(EditorStyles.helpBox);

            SerializedProperty sectionProperty =
                sectionsProperty.GetArrayElementAtIndex(index);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    displayName,
                    EditorStyles.boldLabel);

                if (GUILayout.Button("Deactivate", GUILayout.Width(90f)))
                {
                    FlushSerializedChanges(authoringSession);

                    if (authoringSession.DeactivateSection(sectionId)
                        && authoringSession.Save())
                    {
                        ClearCachedState();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            DrawSectionChildProperties(sectionProperty);
        }

        private static void DrawInactiveActivationButtons(
            string sceneAssetPath,
            HandySceneAuthoringSession authoringSession)
        {
            var descriptors = HandySceneSectionTypeCache.GetDescriptors(sceneAssetPath);
            bool drewAnyButton = false;

            for (int index = 0; index < descriptors.Count; index++)
            {
                HandySceneSectionTypeCache.SectionDescriptor descriptor = descriptors[index];

                if (authoringSession.IsSectionActive(descriptor.SectionId))
                {
                    continue;
                }

                drewAnyButton = true;

                if (GUILayout.Button($"Activate {descriptor.DisplayName}"))
                {
                    FlushSerializedChanges(authoringSession);

                    if (authoringSession.ActivateSection(descriptor.SectionId)
                        && authoringSession.Save())
                    {
                        ClearCachedState();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (!drewAnyButton)
            {
                return;
            }

            EditorGUILayout.Space(4f);
        }

        private static void FlushSerializedChanges(
            HandySceneAuthoringSession authoringSession)
        {
            if (authoringSession?.SerializedCarrier == null)
            {
                return;
            }

            bool serializedChangesApplied =
                authoringSession.SerializedCarrier.ApplyModifiedProperties();
            authoringSession.NotifySerializedChanges(serializedChangesApplied);
        }

        private static void DrawSectionChildProperties(SerializedProperty sectionProperty)
        {
            if (sectionProperty == null || sectionProperty.managedReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "This section is currently empty.",
                    MessageType.Warning);
                return;
            }

            using EditorGUI.IndentLevelScope scope = new();

            SerializedProperty iterator = sectionProperty.Copy();
            SerializedProperty endProperty = sectionProperty.GetEndProperty();
            int childDepth = sectionProperty.depth + 1;
            bool enterChildren = true;
            bool drewAnyChild = false;

            while (iterator.NextVisible(enterChildren)
                && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;

                if (iterator.depth != childDepth)
                {
                    continue;
                }

                drewAnyChild = true;
                EditorGUILayout.PropertyField(iterator, includeChildren: true);
            }

            if (!drewAnyChild)
            {
                EditorGUILayout.HelpBox(
                    "This section has no serialized child fields.",
                    MessageType.None);
            }
        }

        private static void DrawActionButtons(HandySceneAuthoringSession authoringSession)
        {
            using EditorGUILayout.HorizontalScope scope = new();

            using (new EditorGUI.DisabledScope(!authoringSession.HasUnsavedChanges))
            {
                if (GUILayout.Button("Apply Changes"))
                {
                    if (authoringSession.Save())
                    {
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(!authoringSession.CanRevert))
                {
                    if (GUILayout.Button("Revert"))
                    {
                        ClearCachedState();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        #endregion
    }
}