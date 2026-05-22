using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.Scenes
{
    /// <summary>
    /// Stores the generated runtime snapshot for one HandyScene so unloaded
    /// scenes can still expose metadata in builds.
    /// </summary>
    [Serializable]
    public sealed class HandySceneRuntimeCatalogEntry
    {
        #region Fields

        [UnityEngine.SerializeField]
        private string _sceneAssetPath = string.Empty;

        [UnityEngine.SerializeField]
        private string _sceneGuid = string.Empty;

        [UnityEngine.SerializeField]
        private string _sceneName = string.Empty;

        [UnityEngine.SerializeField]
        private int _schemaVersion = HandySceneSchema.CurrentVersion;

        [UnityEngine.SerializeField]
        private List<string> _sectionIds = new();

        [UnityEngine.SerializeReference]
        private List<SceneExtender> _sections = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the project-relative path of the scene asset that owns this
        /// runtime snapshot.
        /// </summary>
        public string SceneAssetPath => _sceneAssetPath ?? string.Empty;

        /// <summary>
        /// Gets the Unity asset GUID of the scene.
        /// </summary>
        public string SceneGuid => _sceneGuid ?? string.Empty;

        /// <summary>
        /// Gets the short scene name without extension.
        /// </summary>
        public string SceneName => _sceneName ?? string.Empty;

        /// <summary>
        /// Gets the schema version stored in this runtime snapshot.
        /// </summary>
        public int SchemaVersion => _schemaVersion;

        /// <summary>
        /// Gets the number of aligned section records stored in the snapshot.
        /// </summary>
        public int SectionCount => Math.Min(_sectionIds.Count, _sections.Count);

        #endregion

        #region Public API

        /// <summary>
        /// Reinitializes this snapshot for one scene while clearing the
        /// previously stored section list.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <param name="sceneGuid">Scene asset GUID.</param>
        /// <param name="sceneName">Scene short name without extension.</param>
        /// <param name="schemaVersion">Schema version stored in the entry.</param>
        public void Reset(
            string sceneAssetPath,
            string sceneGuid,
            string sceneName,
            int schemaVersion)
        {
            _sceneAssetPath = sceneAssetPath ?? string.Empty;
            _sceneGuid = sceneGuid ?? string.Empty;
            _sceneName = sceneName ?? string.Empty;
            _schemaVersion = schemaVersion;
            _sectionIds.Clear();
            _sections.Clear();
        }

        /// <summary>
        /// Adds one resolved section snapshot to this catalog entry.
        /// </summary>
        /// <param name="sectionId">Stable section identifier.</param>
        /// <param name="section">Generated runtime section snapshot.</param>
        public void AddSection(string sectionId, SceneExtender section)
        {
            _sectionIds.Add(sectionId ?? string.Empty);
            _sections.Add(section);
        }

        /// <summary>
        /// Gets the stable section identifier at one aligned index.
        /// </summary>
        /// <param name="index">Aligned section index.</param>
        /// <returns>The stored section identifier.</returns>
        public string GetSectionId(int index)
        {
            return index >= 0 && index < SectionCount
                ? _sectionIds[index]
                : string.Empty;
        }

        /// <summary>
        /// Gets the generated section snapshot at one aligned index.
        /// </summary>
        /// <param name="index">Aligned section index.</param>
        /// <returns>The stored section snapshot.</returns>
        public SceneExtender GetSection(int index)
        {
            return index >= 0 && index < SectionCount
                ? _sections[index]
                : null;
        }

        #endregion
    }
}