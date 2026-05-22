using System;
using IndieGabo.HandyTools.Scenes;
using UnityEngine;

namespace IndieGabo.HandyTools.Scenes.Samples
{
    /// <summary>
    /// Defines one traversal-oriented metadata payload for a HandyScene.
    /// </summary>
    [Serializable]
    [HandySceneSection(
        "sample.travel-profile",
        DisplayName = "Travel Profile",
        Order = 10)]
    public sealed class SceneTravelProfile : SceneExtender
    {
        #region Fields

        [SerializeField]
        private string _entrySpawnId = "spawn.main_gate";

        [SerializeField]
        private bool _allowFastTravel = true;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the default spawn identifier used when the scene is entered.
        /// </summary>
        public string EntrySpawnId => _entrySpawnId;

        /// <summary>
        /// Gets whether the scene can be reached through fast travel.
        /// </summary>
        public bool AllowFastTravel => _allowFastTravel;

        #endregion
    }
}