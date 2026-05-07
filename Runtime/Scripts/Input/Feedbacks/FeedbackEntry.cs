using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystemModule.Feedbacks
{
    /// <summary>
    /// Visual feedbacks for an action mapped in an <see cref="InputActionAsset"/>.     
    /// </summary>
    [System.Serializable]
    public class FeedbackEntry : ISerializationCallbackReceiver
    {
        public readonly static string FallbackSchemeControlName = "Fallback";

        [SerializeField]
        private SpriteDictionary _sprites = new();

        [SerializeField]
        private List<FeedbackAnimation> _animationsList = new();

        private AnimationDictionary _animations = new();

        /// <summary>
        /// Attempts to retrieve a sprite for the specified control scheme.
        /// </summary>
        /// <param name="controlScheme">The control scheme to retrieve the sprite for.</param>
        /// <param name="sprite">The sprite retrieved, if successful.</param>
        /// <returns>True if the sprite was successfully retrieved; otherwise, false.</returns>
        public bool TrySprite(InputControlScheme controlScheme, out Sprite sprite)
        {
            return TrySprite(controlScheme.name, out sprite);
        }

        /// <summary>
        /// Attempts to retrieve a sprite for the specified control scheme.
        /// </summary>
        /// <param name="controlSchemeName">The name of the control scheme to retrieve the sprite for.</param>
        /// <param name="sprite">The sprite retrieved, if successful.</param>
        /// <returns>True if the sprite was successfully retrieved; otherwise, false.</returns>
        public bool TrySprite(string controlSchemeName, out Sprite sprite)
        {
            return _sprites.TryGetValue(controlSchemeName, out sprite);
        }

        /// <summary>
        /// Attempts to retrieve a sprite for the specified control scheme, 
        /// falling back to the default scheme if the specified scheme is not found.
        /// </summary>
        /// <param name="controlScheme">The control scheme to retrieve the sprite for.</param>
        /// <param name="sprite">The sprite retrieved, if successful.</param>
        /// <returns>True if the sprite was successfully retrieved; otherwise, false.</returns>
        public bool TrySpriteOrFallback(InputControlScheme controlScheme, out Sprite sprite)
        {
            return TrySpriteOrFallback(controlScheme.name, out sprite);
        }

        /// <summary>
        /// Attempts to retrieve a sprite for the specified control scheme, 
        /// falling back to the default scheme if the specified scheme is not found.
        /// </summary>
        /// <param name="controlScheme">The control scheme to retrieve the sprite for.</param>
        /// <param name="sprite">The sprite retrieved, if successful.</param>
        /// <returns>True if the sprite was successfully retrieved; otherwise, false.</returns>
        public bool TrySpriteOrFallback(string controlSchemeName, out Sprite sprite)
        {
            if (!TrySprite(controlSchemeName, out sprite))
            {
                return TrySprite(FallbackSchemeControlName, out sprite);
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve animation data for the specified animation name and 
        /// control scheme.
        /// </summary>
        /// <param name="animationName">The name of the animation to retrieve data for.</param>
        /// <param name="controlScheme">The control scheme to retrieve data for.</param>
        /// <param name="animationData">The animation data retrieved, if successful.</param>
        /// <returns>True if animation data was successfully retrieved; otherwise, false.</returns>
        public bool TryAnimationData(
            string animationName,
            InputControlScheme controlScheme,
            out FeedbackAnimationData animationData
        )
        {
            return TryAnimationData(animationName, controlScheme.name, out animationData);
        }

        /// <summary>
        /// Attempts to retrieve animation data for the specified animation name and 
        /// control scheme.
        /// </summary>
        /// <param name="animationName">The name of the animation to retrieve data from.</param>
        /// <param name="controlSchemeName">The name of the control scheme to retrieve data for.</param>
        /// <param name="animationData">The retrieved animation data, or null if not found.</param>
        /// <returns>True if the animation data was found, false otherwise.</returns>
        public bool TryAnimationData(
            string animationName,
            string controlSchemeName,
            out FeedbackAnimationData animationData
        )
        {
            if (!_animations.TryGetValue(
                animationName,
                out FeedbackAnimation feedbackAnimation)
            )
            {
                animationData = null;
                return false;
            }

            return feedbackAnimation.TryGetAnimationData(controlSchemeName, out animationData);
        }

        /// <summary>
        /// Attempts to retrieve animation data for the specified animation name and control scheme.
        /// If the specified control scheme is not found, it falls back to the default scheme.
        /// </summary>
        /// <param name="animationName">The name of the animation to retrieve data from.</param>
        /// <param name="controlSchemeName">The name of the control scheme to retrieve data for.</param>
        /// <param name="animationData">The retrieved animation data, or null if not found.</param>
        /// <returns>True if the animation data was found, false otherwise.</returns>
        public bool TryAnimationDataOrFallback(
            string animationName,
            InputControlScheme controlScheme,
            out FeedbackAnimationData animationData
        )
        {
            return TryAnimationDataOrFallback(
                animationName,
                controlScheme.name,
                out animationData
            );
        }

        /// <summary>
        /// Attempts to retrieve an animation by its name.
        /// </summary>
        /// <param name="animationName">The name of the animation to retrieve.</param>
        /// <param name="animation">The retrieved animation, or null if not found.</param>
        /// <returns>True if the animation was found, false otherwise.</returns>
        public bool TryAnimation(string animationName, out FeedbackAnimation animation)
        {
            return _animations.TryGetValue(animationName, out animation);
        }

        /// <summary>
        /// Attempts to retrieve animation data for the specified animation name and control scheme.
        /// If the specified control scheme is not found, it falls back to the default scheme.
        /// </summary>
        /// <param name="animationName">The name of the animation to retrieve data from.</param>
        /// <param name="controlSchemeName">The name of the control scheme to retrieve data for.</param>
        /// <param name="animationData">The retrieved animation data, or null if not found.</param>
        /// <returns>True if the animation data was found, false otherwise.</returns>
        public bool TryAnimationDataOrFallback(
            string animationName,
            string controlSchemeName,
            out FeedbackAnimationData animationData
        )
        {
            if (!TryAnimationData(animationName, controlSchemeName, out animationData))
            {
                return TryAnimationData(animationName, FallbackSchemeControlName, out animationData);
            }

            return true;
        }

        public void OnBeforeSerialize() { }

        /// <summary>
        /// Called after deserialization. 
        /// Reinitializes the _animations dictionary with data from _animationsList.
        /// </summary>
        public void OnAfterDeserialize()
        {
            _animations.Clear();
            foreach (var animation in _animationsList)
            {
                _animations.Add(animation.name, animation);
            }
        }

        /// <summary>
        /// Key is the control scheme name.
        /// </summary>
        [System.Serializable]
        private class SpriteDictionary : SerializedDictionary<string, Sprite> { }

        /// <summary>
        /// Key is the animation name.
        /// </summary>
        [System.Serializable]
        private class AnimationDictionary : SerializedDictionary<string, FeedbackAnimation> { }


#if UNITY_EDITOR
        /// <summary>
        /// Editor-only.
        /// </summary>
        public List<FeedbackAnimation> AnimationsList => _animationsList;

        /// <summary>
        /// Editor-only. <br />
        /// Note that what gets created is the key for the control scheme in the
        /// dictionary. The Sprite value is created as null.
        /// </summary>
        public Sprite GetOrCreateSprite(string controlSchemeName)
        {
            if (!_sprites.TryGetValue(controlSchemeName, out Sprite sprite))
            {
                _sprites.Add(controlSchemeName, null);
            }

            return sprite;
        }

        /// <summary>
        /// Editor-only.
        /// </summary>
        public void SetSprite(string controlSchemeName, Sprite sprite)
        {
            _sprites[controlSchemeName] = sprite;
        }

        /// <summary>
        /// Editor-only.
        /// </summary>
        /// <param name="animationName"></param>
        /// <returns></returns>
        public FeedbackAnimation GetAnimation(string animationName)
        {
            if (!_animations.TryGetValue(
                animationName,
                out FeedbackAnimation animationData
            ))
            {
                _animations.Add(animationName, new FeedbackAnimation());
            }

            return animationData;
        }
#endif
    }
}