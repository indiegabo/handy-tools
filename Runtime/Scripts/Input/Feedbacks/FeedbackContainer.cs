
using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Logger;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystem.Feedbacks
{
    /// <summary>
    /// A ScriptableObject containing a dictionary of <see cref="FeedbackEntry"/>.
    /// </summary>
    public class FeedbackContainer : HandyScriptableObject
    {
        [SerializeField]
        private InputActionAsset _actionAsset;

        [SerializeField]
        private FeedbackDictionary _feedbacks;

        /// <summary>
        /// The <see cref="InputActionAsset"/> associated with this container.
        /// </summary>
        public InputActionAsset ActionAsset => _actionAsset;

        /// <summary>
        /// Try to get an <see cref="FeedbackEntry"/> for an <see cref="InputAction"/>.
        /// </summary>
        /// <param name="actionID"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool TryEntry(
            Guid actionID,
             out FeedbackEntry entry
        )
        {
            return _feedbacks.TryGetValue(actionID.ToString(), out entry);
        }

        public bool TrySprite(Guid actionID, string controlSchemeName, out Sprite sprite)
        {
            sprite = null;
            if (!_feedbacks.TryGetValue(actionID.ToString(), out FeedbackEntry entry))
            {
                return false;
            }

            return entry.TrySprite(controlSchemeName, out sprite);
        }

        public bool TrySpriteOrFallback(
            Guid actionID, 
            string controlSchemeName, 
            out Sprite sprite
        )
        {
            sprite = null;
            if (!_feedbacks.TryGetValue(actionID.ToString(), out FeedbackEntry entry))
            {
                return false;
            }

            return entry.TrySpriteOrFallback(controlSchemeName, out sprite);
        }

        public bool TryAnimationData(
            string animationName,
            Guid actionID, 
            string controlSchemeName, 
            out FeedbackAnimationData animation
        )
        {
            animation = null;
            if (!_feedbacks.TryGetValue(actionID.ToString(), out FeedbackEntry entry))
            {
                return false;
            }

            return entry.TryAnimationData(animationName, controlSchemeName, out animation);
        }

        public bool TryAnimationDataOrFallback(
            string animationName,
            Guid actionID, 
            string controlSchemeName, 
            out FeedbackAnimationData animation
        )
        {
            animation = null;
            if (!_feedbacks.TryGetValue(actionID.ToString(), out FeedbackEntry entry))
            {
                return false;
            }

            return entry.TryAnimationDataOrFallback(
                animationName, 
                controlSchemeName, 
                out animation
            );
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only.
        /// </summary>
        public void Initialize(InputActionAsset actionAsset)
        {
            _actionAsset = actionAsset;
        }

        /// <summary>
        /// Editor-only.
        /// </summary>
        public FeedbackEntry GetOrCreateEntry(
            Guid actionID
        )
        {
            string guid = actionID.ToString();
            if (!_feedbacks.TryGetValue(guid, out FeedbackEntry entry))
            {
                entry = new FeedbackEntry();
                _feedbacks.Add(guid, entry);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            return entry;
        }
#endif
    }
}