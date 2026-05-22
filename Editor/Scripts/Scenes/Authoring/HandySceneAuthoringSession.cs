using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using Scene = UnityEngine.SceneManagement.Scene;
using Object = UnityEngine.Object;
using HandyScenesRuntime = IndieGabo.HandyTools.Scenes;

namespace IndieGabo.HandyTools.Editor.Scenes.Authoring
{
    /// <summary>
    /// Opens one HandyScene authoring target against either the loaded scene
    /// instance or an isolated preview scene and exposes the carrier-backed
    /// SerializedObject used by the scene-asset inspector.
    /// </summary>
    public sealed class HandySceneAuthoringSession : IDisposable
    {
        #region Constants

        private const string CarrierObjectName = "__HandySceneMetadata";

        #endregion

        #region Types

        private readonly struct SectionSlot
        {
            /// <summary>
            /// Initializes one aligned section slot.
            /// </summary>
            /// <param name="sectionId">Stable section identifier.</param>
            /// <param name="section">Resolved section payload.</param>
            public SectionSlot(string sectionId, HandyScenesRuntime.SceneExtender section)
            {
                SectionId = sectionId ?? string.Empty;
                Section = section;
            }

            /// <summary>
            /// Gets the stable section identifier.
            /// </summary>
            public string SectionId { get; }

            /// <summary>
            /// Gets the resolved section payload.
            /// </summary>
            public HandyScenesRuntime.SceneExtender Section { get; }
        }

        #endregion

        #region Fields

        private readonly string _sceneAssetPath;
        private readonly bool _usesPreviewScene;

        private Scene _scene;
        private HandyScenesRuntime.HandySceneMetadataCarrier _carrier;
        private SerializedObject _serializedCarrier;
        private bool _hasPendingChanges;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes one authoring session for the provided scene asset.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <param name="scene">Resolved loaded or preview scene.</param>
        /// <param name="usesPreviewScene">
        /// Whether the session owns a preview scene instance.
        /// </param>
        private HandySceneAuthoringSession(
            string sceneAssetPath,
            Scene scene,
            bool usesPreviewScene)
        {
            _sceneAssetPath = sceneAssetPath ?? string.Empty;
            _scene = scene;
            _usesPreviewScene = usesPreviewScene;

            EnsureCarrierExists();
            _serializedCarrier = _carrier != null
                ? new SerializedObject(_carrier)
                : null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the project-relative scene asset path being authored.
        /// </summary>
        public string SceneAssetPath => _sceneAssetPath;

        /// <summary>
        /// Gets the scene instance used by this authoring session.
        /// </summary>
        public Scene Scene => _scene;

        /// <summary>
        /// Gets whether the session is authoring through a preview scene.
        /// </summary>
        public bool UsesPreviewScene => _usesPreviewScene;

        /// <summary>
        /// Gets whether the session is bound to the currently loaded scene.
        /// </summary>
        public bool UsesLoadedScene => !_usesPreviewScene;

        /// <summary>
        /// Gets whether the current authoring mode supports a hard revert by
        /// reloading the isolated editing scene.
        /// </summary>
        public bool CanRevert => _usesPreviewScene;

        /// <summary>
        /// Gets whether the session still references one live scene carrier
        /// that can be safely drawn by the inspector.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_sceneAssetPath)
                    || !_scene.IsValid()
                    || !_scene.isLoaded
                    || _carrier == null
                    || _serializedCarrier == null
                    || _serializedCarrier.targetObject == null)
                {
                    return false;
                }

                if (_usesPreviewScene)
                {
                    return _carrier.gameObject.scene.handle == _scene.handle;
                }

                Scene loadedScene = SceneManager.GetSceneByPath(_sceneAssetPath);
                return loadedScene.IsValid()
                    && loadedScene.isLoaded
                    && loadedScene.handle == _scene.handle
                    && _carrier.gameObject.scene.handle == _scene.handle;
            }
        }

        /// <summary>
        /// Gets whether this session still matches the current editor state for
        /// the target scene, switching away from preview authoring when the
        /// real scene becomes loaded and vice versa.
        /// </summary>
        public bool MatchesCurrentEditorState
        {
            get
            {
                if (!IsValid)
                {
                    return false;
                }

                Scene loadedScene = SceneManager.GetSceneByPath(_sceneAssetPath);
                bool sceneIsLoaded = loadedScene.IsValid() && loadedScene.isLoaded;

                if (_usesPreviewScene)
                {
                    return !sceneIsLoaded;
                }

                return sceneIsLoaded && loadedScene.handle == _scene.handle;
            }
        }

        /// <summary>
        /// Gets whether this session has unsaved changes waiting to be written
        /// back to the scene file.
        /// </summary>
        public bool HasUnsavedChanges => _hasPendingChanges || _scene.isDirty;

        /// <summary>
        /// Gets the active carrier component stored in the scene.
        /// </summary>
        public HandyScenesRuntime.HandySceneMetadataCarrier Carrier => _carrier;

        /// <summary>
        /// Gets the SerializedObject used to draw the carrier in the scene
        /// asset inspector.
        /// </summary>
        public SerializedObject SerializedCarrier => _serializedCarrier;

        #endregion

        #region Public API

        /// <summary>
        /// Opens one authoring session for the provided scene asset.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <returns>The initialized authoring session.</returns>
        public static HandySceneAuthoringSession Open(string sceneAssetPath)
        {
            Scene scene = ResolveScene(sceneAssetPath, out bool usesPreviewScene);
            return new HandySceneAuthoringSession(
                sceneAssetPath,
                scene,
                usesPreviewScene);
        }

        /// <summary>
        /// Opens one authoring session for the provided HandyScene reference.
        /// </summary>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <returns>The initialized authoring session.</returns>
        public static HandySceneAuthoringSession Open(
            HandyScenesRuntime.HandySceneReference sceneReference)
        {
            return Open(sceneReference?.SceneAssetPath ?? string.Empty);
        }

        /// <summary>
        /// Gets the display name that should be used for one stored section.
        /// </summary>
        /// <param name="index">Stored section index.</param>
        /// <returns>The effective inspector display name.</returns>
        public string GetSectionDisplayName(int index)
        {
            string sectionId = _carrier != null
                ? _carrier.GetSectionId(index)
                : string.Empty;

            if (HandySceneSectionTypeCache.TryGetDescriptor(
                    sectionId,
                    out var descriptor,
                    _sceneAssetPath))
            {
                return descriptor.DisplayName;
            }

            HandyScenesRuntime.SceneExtender section = _carrier?.GetSection(index);
            if (section != null)
            {
                return ObjectNames.NicifyVariableName(section.GetType().Name);
            }

            return string.IsNullOrWhiteSpace(sectionId)
                ? "Unnamed Section"
                : sectionId;
        }

        /// <summary>
        /// Updates the carrier SerializedObject before inspector drawing.
        /// </summary>
        /// <returns>
        /// True when the carrier SerializedObject is still valid and was
        /// updated successfully.
        /// </returns>
        public bool Update()
        {
            if (!IsValid)
            {
                return false;
            }

            _serializedCarrier.UpdateIfRequiredOrScript();
            return true;
        }

        /// <summary>
        /// Resolves one stored section by its concrete section type.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <param name="section">Resolved section instance.</param>
        /// <returns>True when the section could be resolved.</returns>
        public bool TryGetSection<TSection>(out TSection section)
            where TSection : HandyScenesRuntime.SceneExtender
        {
            if (_carrier != null)
            {
                for (int index = 0; index < _carrier.SectionCount; index++)
                {
                    if (_carrier.GetSection(index) is TSection typedSection)
                    {
                        section = typedSection;
                        return true;
                    }
                }
            }

            section = null;
            return false;
        }

        /// <summary>
        /// Resolves one stored section or returns null when the section is not
        /// present in the current carrier.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <returns>The resolved section when available.</returns>
        public TSection GetSectionOrNull<TSection>()
            where TSection : HandyScenesRuntime.SceneExtender
        {
            _ = TryGetSection(out TSection section);
            return section;
        }

        /// <summary>
        /// Resolves the serialized property that backs one stored section type.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <param name="sectionProperty">Resolved serialized property.</param>
        /// <returns>True when the section property could be resolved.</returns>
        public bool TryGetSectionProperty<TSection>(out SerializedProperty sectionProperty)
            where TSection : HandyScenesRuntime.SceneExtender
        {
            sectionProperty = null;

            if (!Update()
                || !TryGetSectionIndex(typeof(TSection), out int sectionIndex))
            {
                return false;
            }

            SerializedProperty sectionsProperty =
                _serializedCarrier.FindProperty("_sections");

            if (sectionsProperty == null
                || sectionIndex < 0
                || sectionIndex >= sectionsProperty.arraySize)
            {
                return false;
            }

            sectionProperty = sectionsProperty.GetArrayElementAtIndex(sectionIndex);
            return sectionProperty != null;
        }

        /// <summary>
        /// Gets whether one persisted section is currently active in this
        /// authoring target.
        /// </summary>
        /// <param name="sectionId">Stable section identifier.</param>
        /// <returns>True when the section is active.</returns>
        public bool IsSectionActive(string sectionId)
        {
            if (_carrier == null || string.IsNullOrWhiteSpace(sectionId))
            {
                return false;
            }

            for (int index = 0; index < _carrier.SectionCount; index++)
            {
                if (string.Equals(
                        _carrier.GetSectionId(index),
                        sectionId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Activates one discovered metadata section inside the current
        /// authoring target.
        /// </summary>
        /// <param name="sectionId">Stable section identifier.</param>
        /// <returns>True when the section is active after the call.</returns>
        public bool ActivateSection(string sectionId)
        {
            if (string.IsNullOrWhiteSpace(sectionId)
                || !HandySceneSectionTypeCache.TryGetDescriptor(
                    sectionId,
                    out HandySceneSectionTypeCache.SectionDescriptor descriptor,
                    _sceneAssetPath))
            {
                return false;
            }

            EnsureCarrierExists();

            List<SectionSlot> targetSlots = BuildCurrentSlots();
            for (int index = 0; index < targetSlots.Count; index++)
            {
                if (string.Equals(
                        targetSlots[index].SectionId,
                        descriptor.SectionId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            targetSlots.Add(new SectionSlot(
                descriptor.SectionId,
                CreateSectionInstance(descriptor.Type, descriptor.SectionId)));

            SortSectionSlots(targetSlots);
            ApplySectionSlots(targetSlots);
            return true;
        }

        /// <summary>
        /// Deactivates one currently active metadata section from the current
        /// authoring target.
        /// </summary>
        /// <param name="sectionId">Stable section identifier.</param>
        /// <returns>True when the section state changed.</returns>
        public bool DeactivateSection(string sectionId)
        {
            if (_carrier == null || string.IsNullOrWhiteSpace(sectionId))
            {
                return false;
            }

            List<SectionSlot> targetSlots = BuildCurrentSlots();
            int removedCount = targetSlots.RemoveAll(slot =>
                string.Equals(slot.SectionId, sectionId, StringComparison.Ordinal));

            if (removedCount == 0)
            {
                return false;
            }

            SortSectionSlots(targetSlots);
            ApplySectionSlots(targetSlots);
            return true;
        }

        /// <summary>
        /// Activates every discovered section visible to the current authoring
        /// context.
        /// </summary>
        public void ActivateAllKnownSections()
        {
            EnsureCarrierExists();

            List<SectionSlot> targetSlots = BuildCurrentSlots();
            HashSet<string> seenSectionIds = new(StringComparer.Ordinal);

            for (int index = 0; index < targetSlots.Count; index++)
            {
                seenSectionIds.Add(targetSlots[index].SectionId);
            }

            IReadOnlyList<HandySceneSectionTypeCache.SectionDescriptor> descriptors =
                HandySceneSectionTypeCache.GetDescriptors(_sceneAssetPath);

            for (int index = 0; index < descriptors.Count; index++)
            {
                HandySceneSectionTypeCache.SectionDescriptor descriptor =
                    descriptors[index];

                if (seenSectionIds.Contains(descriptor.SectionId))
                {
                    continue;
                }

                targetSlots.Add(new SectionSlot(
                    descriptor.SectionId,
                    CreateSectionInstance(descriptor.Type, descriptor.SectionId)));
            }

            SortSectionSlots(targetSlots);
            ApplySectionSlots(targetSlots);
        }

        /// <summary>
        /// Deactivates all persisted metadata sections from the current
        /// authoring target.
        /// </summary>
        public void DeactivateAllSections()
        {
            if (_carrier == null)
            {
                return;
            }

            ApplySectionSlots(new List<SectionSlot>());
        }

        /// <summary>
        /// Marks the current carrier as modified after one custom editor tool
        /// mutates a section directly.
        /// </summary>
        public void MarkDirty()
        {
            if (_carrier == null)
            {
                return;
            }

            _hasPendingChanges = true;
            EditorUtility.SetDirty(_carrier);

            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(_scene);
            }
        }

        /// <summary>
        /// Records whether one inspector pass mutated the carrier data.
        /// </summary>
        /// <param name="serializedChangesApplied">
        /// Whether the carrier SerializedObject applied property mutations.
        /// </param>
        public void NotifySerializedChanges(bool serializedChangesApplied)
        {
            if (!serializedChangesApplied)
            {
                return;
            }

            MarkDirty();
        }

        /// <summary>
        /// Writes the current carrier state back to the scene file and keeps
        /// the importer marker normalized.
        /// </summary>
        /// <returns>True when the scene changes were saved successfully.</returns>
        public bool Save()
        {
            if (_carrier == null)
            {
                return false;
            }

            bool hasActiveSections = _carrier.SectionCount > 0;
            bool saveSucceeded;

            if (hasActiveSections)
            {
                _carrier.SchemaVersion = Math.Max(
                    _carrier.SchemaVersion,
                    HandyScenesRuntime.HandySceneSchema.CurrentVersion);
                _carrier.IsHandyScene = true;

                saveSucceeded = _usesPreviewScene
                    ? SavePreviewSceneChanges()
                    : SaveLoadedSceneChanges();
            }
            else
            {
                saveSucceeded = _usesPreviewScene
                    ? ClearPreviewSceneChanges()
                    : ClearLoadedSceneChanges();
            }

            if (!saveSucceeded)
            {
                return false;
            }

            HandyScenesRuntime.HandySceneRuntimeReader.InvalidateCachedScene(_sceneAssetPath);

            _hasPendingChanges = false;

            if (IsValid)
            {
                _serializedCarrier.UpdateIfRequiredOrScript();
            }

            return true;
        }

        /// <summary>
        /// Removes the carrier from the scene file and clears the HandyScene
        /// importer marker.
        /// </summary>
        /// <returns>True when the scene was unmarked successfully.</returns>
        public bool Unmark()
        {
            DeactivateAllSections();
            return Save();
        }

        /// <summary>
        /// Releases any preview scene owned by this session.
        /// </summary>
        public void Dispose()
        {
            if (_usesPreviewScene && _scene.IsValid())
            {
                _ = EditorSceneManager.ClosePreviewScene(_scene);
            }

            _carrier = null;
            _serializedCarrier = null;
        }

        #endregion

        #region Helpers

        private static Scene ResolveScene(
            string sceneAssetPath,
            out bool usesPreviewScene)
        {
            Scene loadedScene = SceneManager.GetSceneByPath(sceneAssetPath);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                usesPreviewScene = false;
                return loadedScene;
            }

            usesPreviewScene = true;
            return EditorSceneManager.OpenPreviewScene(sceneAssetPath);
        }

        private bool SaveLoadedSceneChanges()
        {
            EditorUtility.SetDirty(_carrier);
            EditorSceneManager.MarkSceneDirty(_scene);

            if (!EditorSceneManager.SaveScene(_scene))
            {
                return false;
            }

            return HandySceneMetadataStore.SetHandySceneEnabled(
                _sceneAssetPath,
                isEnabled: true);
        }

        private bool SavePreviewSceneChanges()
        {
            return ExecuteAgainstPersistentScene(loadedScene =>
            {
                HandyScenesRuntime.HandySceneMetadataCarrier persistentCarrier =
                    GetOrCreateCarrierInScene(loadedScene);

                CopyCarrierState(_carrier, persistentCarrier);
                HideCarrierObject(persistentCarrier);

                EditorUtility.SetDirty(persistentCarrier);
                EditorSceneManager.MarkSceneDirty(loadedScene);

                if (!EditorSceneManager.SaveScene(loadedScene))
                {
                    return false;
                }

                return HandySceneMetadataStore.SetHandySceneEnabled(
                    _sceneAssetPath,
                    isEnabled: true);
            });
        }

        private bool ClearLoadedSceneChanges()
        {
            if (_carrier != null)
            {
                GameObject carrierGameObject = _carrier.gameObject;
                Object.DestroyImmediate(carrierGameObject);
                EditorSceneManager.MarkSceneDirty(_scene);

                if (!EditorSceneManager.SaveScene(_scene))
                {
                    return false;
                }
            }

            _carrier = null;
            _serializedCarrier = null;

            return HandySceneMetadataStore.SetHandySceneEnabled(
                _sceneAssetPath,
                isEnabled: false);
        }

        private bool ClearPreviewSceneChanges()
        {
            bool removedFromScene = ExecuteAgainstPersistentScene(loadedScene =>
            {
                HandyScenesRuntime.HandySceneMetadataCarrier persistentCarrier =
                    FindCarrierInScene(loadedScene);

                if (persistentCarrier == null)
                {
                    return true;
                }

                Object.DestroyImmediate(persistentCarrier.gameObject);
                EditorSceneManager.MarkSceneDirty(loadedScene);
                return EditorSceneManager.SaveScene(loadedScene);
            });

            if (!removedFromScene)
            {
                return false;
            }

            if (_carrier != null)
            {
                Object.DestroyImmediate(_carrier.gameObject);
            }

            _carrier = null;
            _serializedCarrier = null;

            return HandySceneMetadataStore.SetHandySceneEnabled(
                _sceneAssetPath,
                isEnabled: false);
        }

        private bool UnmarkPreviewScene()
        {
            bool removedFromScene = ExecuteAgainstPersistentScene(loadedScene =>
            {
                HandyScenesRuntime.HandySceneMetadataCarrier persistentCarrier =
                    FindCarrierInScene(loadedScene);

                if (persistentCarrier != null)
                {
                    Object.DestroyImmediate(persistentCarrier.gameObject);
                }

                EditorSceneManager.MarkSceneDirty(loadedScene);
                return EditorSceneManager.SaveScene(loadedScene);
            });

            return removedFromScene
                && HandySceneMetadataStore.ClearHandySceneMetadata(_sceneAssetPath);
        }

        private bool UnmarkLoadedScene()
        {
            if (_carrier != null)
            {
                GameObject carrierGameObject = _carrier.gameObject;
                Object.DestroyImmediate(carrierGameObject);
                EditorSceneManager.MarkSceneDirty(_scene);

                if (!EditorSceneManager.SaveScene(_scene))
                {
                    return false;
                }
            }

            _carrier = null;
            _serializedCarrier = null;
            return HandySceneMetadataStore.ClearHandySceneMetadata(_sceneAssetPath);
        }

        private bool ExecuteAgainstPersistentScene(Func<Scene, bool> action)
        {
            if (action == null)
            {
                return false;
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene loadedScene = default;

            try
            {
                loadedScene = EditorSceneManager.OpenScene(
                    _sceneAssetPath,
                    OpenSceneMode.Additive);

                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                {
                    return false;
                }

                return action(loadedScene);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError(
                    $"Could not open HandyScene '{_sceneAssetPath}' for preview " +
                    $"persistence. {exception.Message}");
                return false;
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    _ = SceneManager.SetActiveScene(previousActiveScene);
                }

                if (loadedScene.IsValid() && loadedScene.isLoaded)
                {
                    _ = EditorSceneManager.CloseScene(loadedScene, true);
                }
            }
        }

        private void EnsureCarrierExists()
        {
            if (_carrier != null)
            {
                return;
            }

            _carrier = FindCarrierInScene(_scene);
            if (_carrier != null)
            {
                return;
            }

            _carrier = GetOrCreateCarrierInScene(_scene);
            EditorSceneManager.MarkSceneDirty(_scene);
        }

        private List<SectionSlot> BuildCurrentSlots()
        {
            List<SectionSlot> slots = new();

            for (int index = 0; index < _carrier.SectionCount; index++)
            {
                slots.Add(new SectionSlot(
                    _carrier.GetSectionId(index),
                    _carrier.GetSection(index)));
            }

            return slots;
        }

        private void ApplySectionSlots(List<SectionSlot> targetSlots)
        {
            EnsureCarrierExists();

            if (_carrier == null)
            {
                return;
            }

            _carrier.ResetState(
                HandyScenesRuntime.HandySceneSchema.CurrentVersion,
                targetSlots.Count > 0);

            for (int index = 0; index < targetSlots.Count; index++)
            {
                SectionSlot slot = targetSlots[index];
                _carrier.AddSection(slot.SectionId, slot.Section);
            }

            EditorUtility.SetDirty(_carrier);
            EditorSceneManager.MarkSceneDirty(_scene);
            _hasPendingChanges = true;
        }

        private void SortSectionSlots(List<SectionSlot> slots)
        {
            if (slots == null || slots.Count <= 1)
            {
                return;
            }

            Dictionary<string, int> descriptorOrderById = new(StringComparer.Ordinal);
            IReadOnlyList<HandySceneSectionTypeCache.SectionDescriptor> descriptors =
                HandySceneSectionTypeCache.GetDescriptors(_sceneAssetPath);

            for (int index = 0; index < descriptors.Count; index++)
            {
                descriptorOrderById[descriptors[index].SectionId] = index;
            }

            slots.Sort((left, right) =>
            {
                bool hasLeftOrder = descriptorOrderById.TryGetValue(
                    left.SectionId,
                    out int leftOrder);
                bool hasRightOrder = descriptorOrderById.TryGetValue(
                    right.SectionId,
                    out int rightOrder);

                if (hasLeftOrder && hasRightOrder)
                {
                    return leftOrder.CompareTo(rightOrder);
                }

                if (hasLeftOrder)
                {
                    return -1;
                }

                if (hasRightOrder)
                {
                    return 1;
                }

                return string.CompareOrdinal(left.SectionId, right.SectionId);
            });
        }

        private bool TryGetSectionIndex(Type sectionType, out int sectionIndex)
        {
            if (_carrier != null && sectionType != null)
            {
                for (int index = 0; index < _carrier.SectionCount; index++)
                {
                    HandyScenesRuntime.SceneExtender section = _carrier.GetSection(index);
                    if (section != null && sectionType.IsInstanceOfType(section))
                    {
                        sectionIndex = index;
                        return true;
                    }
                }
            }

            sectionIndex = -1;
            return false;
        }

        private HandyScenesRuntime.SceneExtender CreateSectionInstance(
            Type sectionType,
            string sectionId)
        {
            try
            {
                return Activator.CreateInstance(sectionType, true)
                    as HandyScenesRuntime.SceneExtender;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not instantiate HandyScene section '{sectionId}' for " +
                    $"scene '{_sceneAssetPath}'. {exception.Message}");
                return null;
            }
        }

        private static HandyScenesRuntime.HandySceneMetadataCarrier FindCarrierInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                HandyScenesRuntime.HandySceneMetadataCarrier carrier =
                    roots[rootIndex].GetComponentInChildren<
                        HandyScenesRuntime.HandySceneMetadataCarrier>(true);

                if (carrier != null)
                {
                    return carrier;
                }
            }

            return null;
        }

        private static HandyScenesRuntime.HandySceneMetadataCarrier GetOrCreateCarrierInScene(
            Scene scene)
        {
            HandyScenesRuntime.HandySceneMetadataCarrier carrier = FindCarrierInScene(scene);
            if (carrier != null)
            {
                HideCarrierObject(carrier);
                carrier.gameObject.SetActive(false);
                return carrier;
            }

            GameObject carrierGameObject = new(CarrierObjectName);
            carrierGameObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(carrierGameObject, scene);

            carrier = carrierGameObject.AddComponent<HandyScenesRuntime.HandySceneMetadataCarrier>();
            carrier.SchemaVersion = HandyScenesRuntime.HandySceneSchema.CurrentVersion;
            carrier.IsHandyScene = true;

            HideCarrierObject(carrier);
            return carrier;
        }

        private static void CopyCarrierState(
            HandyScenesRuntime.HandySceneMetadataCarrier source,
            HandyScenesRuntime.HandySceneMetadataCarrier destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            EditorUtility.CopySerializedManagedFieldsOnly(source, destination);
        }

        private static void HideCarrierObject(HandyScenesRuntime.HandySceneMetadataCarrier carrier)
        {
            if (carrier == null)
            {
                return;
            }

            carrier.gameObject.hideFlags = HideFlags.HideInHierarchy;
            carrier.hideFlags = HideFlags.HideInInspector;
        }

        #endregion
    }
}