using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.Scenes
{
    /// <summary>
    /// Reads HandyScene metadata either from the live loaded scene carrier,
    /// from one editor-only on-demand snapshot, or from the generated player
    /// build catalog used for unloaded scenes at runtime.
    /// </summary>
    public static class HandySceneRuntimeReader
    {
        #region Fields

        private static HandySceneRuntimeCatalog _cachedBuildCatalog;

#if UNITY_EDITOR
        private static readonly Dictionary<string, HandySceneRuntimeCatalogEntry>
            EditorSnapshotCache = new(StringComparer.Ordinal);
#endif

        #endregion

        #region Public API

        /// <summary>
        /// Loads all resolved SceneExtender payloads for one HandyScene
        /// reference.
        /// </summary>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <param name="sections">Resolved metadata section instances.</param>
        /// <returns>True when the reference could resolve one metadata source.</returns>
        public static bool TryLoadSections(
            HandySceneReference sceneReference,
            out List<SceneExtender> sections)
        {
            return TryLoadSections(sceneReference?.SceneAssetPath ?? string.Empty, out sections);
        }

        /// <summary>
        /// Resolves one section of the requested type from one HandyScene
        /// reference.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <param name="sceneReference">Serialized HandyScene reference.</param>
        /// <param name="section">Resolved section instance.</param>
        /// <returns>True when the requested section could be resolved.</returns>
        public static bool TryGetSection<TSection>(
            HandySceneReference sceneReference,
            out TSection section)
            where TSection : SceneExtender
        {
            return TryGetSection(sceneReference?.SceneAssetPath ?? string.Empty, out section);
        }

        /// <summary>
        /// Resolves one section of the requested type from one scene asset
        /// path.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <param name="section">Resolved section instance.</param>
        /// <returns>True when the requested section could be resolved.</returns>
        public static bool TryGetSection<TSection>(
            string sceneAssetPath,
            out TSection section)
            where TSection : SceneExtender
        {
            section = null;

            if (!TryLoadSections(sceneAssetPath, out List<SceneExtender> sections))
            {
                return false;
            }

            return TryResolveSection(sections, out section);
        }

        /// <summary>
        /// Resolves one section of the requested type from one already loaded
        /// scene.
        /// </summary>
        /// <typeparam name="TSection">Requested SceneExtender type.</typeparam>
        /// <param name="scene">Loaded scene to inspect.</param>
        /// <param name="section">Resolved section instance.</param>
        /// <returns>True when the requested section could be resolved.</returns>
        public static bool TryGetSection<TSection>(
            Scene scene,
            out TSection section)
            where TSection : SceneExtender
        {
            section = null;

            if (!TryLoadSections(scene, out List<SceneExtender> sections))
            {
                return false;
            }

            return TryResolveSection(sections, out section);
        }

        /// <summary>
        /// Invalidates any cached unloaded-scene snapshot or build-catalog
        /// state for one scene path.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        public static void InvalidateCachedScene(string sceneAssetPath)
        {
            _cachedBuildCatalog = null;

#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(sceneAssetPath))
            {
                EditorSnapshotCache.Clear();
                return;
            }

            EditorSnapshotCache.Remove(sceneAssetPath);
#endif
        }

        /// <summary>
        /// Loads and materializes all resolved SceneExtender payloads for one
        /// HandyScene asset path. Loaded scenes preserve live scene-object
        /// references. Unloaded scenes resolve from the generated runtime
        /// catalog, which only preserves project asset references.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <param name="sections">Resolved metadata section instances.</param>
        /// <returns>
        /// True when one live carrier or generated catalog entry could be
        /// resolved for the requested scene.
        /// </returns>
        public static bool TryLoadSections(
            string sceneAssetPath,
            out List<SceneExtender> sections)
        {
            sections = new List<SceneExtender>();

            if (TryGetLoadedScene(sceneAssetPath, out Scene loadedScene)
                && TryLoadSections(loadedScene, out sections))
            {
                return true;
            }

            if (!TryGetCatalogEntry(
                    sceneAssetPath,
                    out HandySceneRuntimeCatalogEntry entry)
                || entry == null)
            {
                return false;
            }

            for (int index = 0; index < entry.SectionCount; index++)
            {
                SceneExtender section = entry.GetSection(index);
                if (section != null)
                {
                    sections.Add(section);
                }
            }

            return true;
        }

        /// <summary>
        /// Loads all resolved SceneExtender payloads from one already loaded
        /// scene carrier.
        /// </summary>
        /// <param name="scene">Loaded scene that owns the carrier.</param>
        /// <param name="sections">Resolved metadata section instances.</param>
        /// <returns>
        /// True when the loaded scene contains one valid HandyScene carrier.
        /// </returns>
        public static bool TryLoadSections(
            Scene scene,
            out List<SceneExtender> sections)
        {
            sections = new List<SceneExtender>();

            if (!TryGetCarrier(scene, out HandySceneMetadataCarrier carrier)
                || carrier == null
                || !carrier.IsHandyScene)
            {
                return false;
            }

            for (int index = 0; index < carrier.SectionCount; index++)
            {
                SceneExtender section = carrier.GetSection(index);
                if (section != null)
                {
                    sections.Add(section);
                }
            }

            return true;
        }

        #endregion

        #region Helpers

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            _cachedBuildCatalog = null;

#if UNITY_EDITOR
            EditorSnapshotCache.Clear();
#endif
        }

        private static bool TryGetLoadedScene(
            string sceneAssetPath,
            out Scene scene)
        {
            scene = default;

            if (string.IsNullOrWhiteSpace(sceneAssetPath))
            {
                return false;
            }

            scene = SceneManager.GetSceneByPath(sceneAssetPath);
            return scene.IsValid() && scene.isLoaded;
        }

        private static bool TryGetCarrier(
            Scene scene,
            out HandySceneMetadataCarrier carrier)
        {
            carrier = null;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                carrier = roots[rootIndex].GetComponentInChildren<
                    HandySceneMetadataCarrier>(true);

                if (carrier != null)
                {
                    return true;
                }
            }

            carrier = null;
            return false;
        }

        private static bool TryResolveSection<TSection>(
            IReadOnlyList<SceneExtender> sections,
            out TSection section)
            where TSection : SceneExtender
        {
            if (sections != null)
            {
                for (int index = 0; index < sections.Count; index++)
                {
                    if (sections[index] is TSection typedSection)
                    {
                        section = typedSection;
                        return true;
                    }
                }
            }

            section = null;
            return false;
        }

        private static bool TryGetCatalogEntry(
            string sceneAssetPath,
            out HandySceneRuntimeCatalogEntry entry)
        {
            entry = null;

            if (string.IsNullOrWhiteSpace(sceneAssetPath))
            {
                return false;
            }

#if UNITY_EDITOR
            if (TryGetEditorSnapshotEntry(sceneAssetPath, out entry))
            {
                return true;
            }
#endif

            HandySceneRuntimeCatalog catalog = GetOrLoadBuildCatalog();
            return catalog != null && catalog.TryGetEntry(sceneAssetPath, out entry);
        }

        private static HandySceneRuntimeCatalog GetOrLoadBuildCatalog()
        {
            _cachedBuildCatalog ??= Resources.Load<HandySceneRuntimeCatalog>(
                HandySceneRuntimeCatalog.ResourcePath);
            return _cachedBuildCatalog;
        }

#if UNITY_EDITOR
        private static bool TryGetEditorSnapshotEntry(
            string sceneAssetPath,
            out HandySceneRuntimeCatalogEntry entry)
        {
            entry = null;

            if (EditorSnapshotCache.TryGetValue(sceneAssetPath, out entry))
            {
                return entry != null;
            }

            if (!HandySceneEditorSnapshotUtility.TryCreateCatalogEntry(
                    sceneAssetPath,
                    out entry)
                || entry == null)
            {
                return false;
            }

            EditorSnapshotCache[sceneAssetPath] = entry;
            return true;
        }
#endif

        #endregion
    }
}