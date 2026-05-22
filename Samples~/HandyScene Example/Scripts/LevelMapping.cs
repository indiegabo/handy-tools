using System;
using IndieGabo.HandyTools.Scenes;
using UnityEngine;

namespace IndieGabo.HandyTools.Scenes.Samples
{
    /// <summary>
    /// Defines one simple level-facing metadata payload for a HandyScene.
    /// </summary>
    [Serializable]
    [HandySceneSection(
        "sample.level-mapping",
        DisplayName = "Level Mapping",
        Order = 0)]
    public sealed class LevelMapping : SceneExtender
    {
        #region Fields

        [SerializeField]
        private string _levelCode = "Level-01";

        [SerializeField]
        private int _recommendedPower = 5;

        [SerializeField]
        private GameObject _recommendedPowerObject;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the stable level code associated with the scene.
        /// </summary>
        public string LevelCode => _levelCode;

        /// <summary>
        /// Gets the recommended player power for the level.
        /// </summary>
        public int RecommendedPower => _recommendedPower;

        /// <summary>
        /// Gets the recommended player power object for the level.
        /// </summary>
        public GameObject RecommendedPowerObject => _recommendedPowerObject;

        #endregion
    }
}