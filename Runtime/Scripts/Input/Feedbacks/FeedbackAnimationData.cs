using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.HandyInputSystem.Feedbacks
{
    [System.Serializable]
    public class FeedbackAnimationData
    {
        public List<Sprite> sprites = new();
        public GameObject prefab;
    }

}