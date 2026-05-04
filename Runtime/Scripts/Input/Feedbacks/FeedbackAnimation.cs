using IndieGabo.HandyTools.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystem.Feedbacks
{
    [System.Serializable]
    public class FeedbackAnimation
    {
        [SerializeField]
        private AnimationRegistry _registry = new();

        public string name;

        /// <summary>
        /// Attempts to retrieve animation data for the specified control scheme.
        /// </summary>
        /// <param name="controlScheme">The control scheme to retrieve data for.</param>
        /// <param name="animationData">The animation data retrieved, if successful.</param>
        /// <returns>True if animation data was successfully retrieved; otherwise, false.</returns>
        public bool TryGetAnimationData(InputControlScheme controlScheme, out FeedbackAnimationData animationData)
        {
            return TryGetAnimationData(controlScheme.name, out animationData);
        }

        /// <summary>
        /// Attempts to retrieve animation data for the specified control scheme name.
        /// </summary>
        /// <param name="controlSchemeName">The name of the control scheme to retrieve data for.</param>
        /// <param name="animationData">The animation data retrieved, if successful.</param>
        /// <returns>True if animation data was successfully retrieved; otherwise, false.</returns>
        public bool TryGetAnimationData(string controlSchemeName, out FeedbackAnimationData animationData)
        {
            return _registry.TryGetValue(controlSchemeName, out animationData);
        }

        [System.Serializable]
        private class AnimationRegistry
            : SerializedDictionary<string, FeedbackAnimationData>
        { }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only.
        /// </summary>
        /// <param name="controlSchemeName"></param>
        /// <returns></returns>
        public FeedbackAnimationData GetAnimationData(string controlSchemeName)
        {
            if (!_registry.TryGetValue(
                controlSchemeName,
                out FeedbackAnimationData animationData
            ))
            {
                animationData = new FeedbackAnimationData();
                _registry.Add(controlSchemeName, animationData);
            }

            return animationData;
        }
#endif
    }

}