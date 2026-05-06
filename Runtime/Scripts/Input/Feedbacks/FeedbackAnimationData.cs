using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.HandyInputSystemModule.Feedbacks
{
    [System.Serializable]
    /// <summary>
    /// Defines the visual assets used by one input feedback animation.
    /// </summary>
    public class FeedbackAnimationData
    {
        /// <summary>
        /// Ordered sprite sequence displayed by the animation.
        /// </summary>
        public List<Sprite> sprites = new();

        /// <summary>
        /// Optional prefab instantiated by the animation flow.
        /// </summary>
        public GameObject prefab;
    }

}