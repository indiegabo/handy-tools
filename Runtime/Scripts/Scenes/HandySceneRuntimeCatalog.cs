using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.Scenes
{
    /// <summary>
    /// Stores the generated runtime snapshots emitted only for player builds
    /// so unloaded HandyScenes can still expose metadata at runtime.
    /// </summary>
    public sealed class HandySceneRuntimeCatalog : ScriptableObject
    {
        #region Constants

        /// <summary>
        /// Resource path used to load the generated runtime catalog.
        /// </summary>
        public const string ResourcePath = "HandyTools/Scenes/HandySceneRuntimeCatalog";

        #endregion

        #region Fields

        [SerializeField]
        private int _schemaVersion = HandySceneSchema.CurrentVersion;

        [SerializeField]
        private List<HandySceneRuntimeCatalogEntry> _entries = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the schema version stored by the catalog asset.
        /// </summary>
        public int SchemaVersion
        {
            get => _schemaVersion;
            set => _schemaVersion = value;
        }

        /// <summary>
        /// Gets the generated scene entries stored in the catalog.
        /// </summary>
        public IReadOnlyList<HandySceneRuntimeCatalogEntry> Entries => _entries;

        #endregion

        #region Public API

        /// <summary>
        /// Resets the catalog contents before one rebuild pass repopulates the
        /// generated scene entries.
        /// </summary>
        /// <param name="schemaVersion">Schema version to store.</param>
        public void ResetEntries(int schemaVersion)
        {
            _schemaVersion = schemaVersion;
            _entries.Clear();
        }

        /// <summary>
        /// Adds one generated scene entry to the catalog.
        /// </summary>
        /// <param name="entry">Generated scene entry.</param>
        public void AddEntry(HandySceneRuntimeCatalogEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            _entries.Add(entry);
        }

        /// <summary>
        /// Removes the generated entry for one scene asset path.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        public void RemoveEntry(string sceneAssetPath)
        {
            if (string.IsNullOrWhiteSpace(sceneAssetPath))
            {
                return;
            }

            for (int index = _entries.Count - 1; index >= 0; index--)
            {
                HandySceneRuntimeCatalogEntry entry = _entries[index];
                if (entry != null
                    && string.Equals(
                        entry.SceneAssetPath,
                        sceneAssetPath,
                        StringComparison.Ordinal))
                {
                    _entries.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Sorts the catalog entries into a stable path-based order.
        /// </summary>
        public void SortEntries()
        {
            _entries.Sort(CompareEntries);
        }

        /// <summary>
        /// Attempts to resolve one generated entry by scene asset path.
        /// </summary>
        /// <param name="sceneAssetPath">Project-relative scene asset path.</param>
        /// <param name="entry">Resolved generated entry.</param>
        /// <returns>True when one matching scene entry exists.</returns>
        public bool TryGetEntry(
            string sceneAssetPath,
            out HandySceneRuntimeCatalogEntry entry)
        {
            for (int index = 0; index < _entries.Count; index++)
            {
                HandySceneRuntimeCatalogEntry candidate = _entries[index];
                if (candidate != null
                    && string.Equals(
                        candidate.SceneAssetPath,
                        sceneAssetPath,
                        StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        #endregion

        #region Helpers

        private static int CompareEntries(
            HandySceneRuntimeCatalogEntry left,
            HandySceneRuntimeCatalogEntry right)
        {
            return string.CompareOrdinal(
                left?.SceneAssetPath ?? string.Empty,
                right?.SceneAssetPath ?? string.Empty);
        }

        #endregion
    }
}