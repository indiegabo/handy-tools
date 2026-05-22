using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.Scenes
{
    /// <summary>
    /// Stores HandyScene metadata directly inside the scene YAML so Unity can
    /// serialize native asset and scene object references without a parallel
    /// companion asset.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class HandySceneMetadataCarrier : MonoBehaviour
    {
        #region Fields

        [SerializeField]
        private int _schemaVersion = HandySceneSchema.CurrentVersion;

        [SerializeField]
        private bool _isHandyScene = true;

        [SerializeField]
        private List<string> _sectionIds = new();

        [SerializeReference]
        private List<SceneExtender> _sections = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the metadata schema version stored on the scene.
        /// </summary>
        public int SchemaVersion
        {
            get => _schemaVersion;
            set => _schemaVersion = value;
        }

        /// <summary>
        /// Gets or sets whether the owning scene is marked as a HandyScene.
        /// </summary>
        public bool IsHandyScene
        {
            get => _isHandyScene;
            set => _isHandyScene = value;
        }

        /// <summary>
        /// Gets the number of resolved section payloads stored by this carrier.
        /// </summary>
        public int SectionCount => Math.Min(_sectionIds.Count, _sections.Count);

        /// <summary>
        /// Gets the stable identifiers of the stored sections.
        /// </summary>
        public IReadOnlyList<string> SectionIds => _sectionIds;

        /// <summary>
        /// Gets the resolved section payloads stored by this carrier.
        /// </summary>
        public IReadOnlyList<SceneExtender> Sections => _sections;

        #endregion

        #region Public API

        /// <summary>
        /// Resets the scene-level metadata state while preserving the existing
        /// section list capacity for the caller to repopulate it.
        /// </summary>
        /// <param name="schemaVersion">Schema version to store.</param>
        /// <param name="isHandyScene">Whether the owning scene is enabled.</param>
        public void ResetState(int schemaVersion, bool isHandyScene)
        {
            _schemaVersion = schemaVersion;
            _isHandyScene = isHandyScene;
            _sectionIds.Clear();
            _sections.Clear();
        }

        /// <summary>
        /// Adds one resolved metadata section to the carrier.
        /// </summary>
        /// <param name="sectionId">Stable section identifier.</param>
        /// <param name="section">Resolved section payload.</param>
        public void AddSection(string sectionId, SceneExtender section)
        {
            _sectionIds.Add(sectionId ?? string.Empty);
            _sections.Add(section);
        }

        /// <summary>
        /// Gets the stable identifier of one stored section.
        /// </summary>
        /// <param name="index">Stored section index.</param>
        /// <returns>The stored section identifier.</returns>
        public string GetSectionId(int index)
        {
            return index >= 0 && index < SectionCount
                ? _sectionIds[index]
                : string.Empty;
        }

        /// <summary>
        /// Gets one stored section payload.
        /// </summary>
        /// <param name="index">Stored section index.</param>
        /// <returns>The stored section payload.</returns>
        public SceneExtender GetSection(int index)
        {
            return index >= 0 && index < SectionCount
                ? _sections[index]
                : null;
        }

        #endregion
    }
}