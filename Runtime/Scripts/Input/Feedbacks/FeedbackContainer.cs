
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystem.Feedbacks
{
    /// <summary>
    /// A ScriptableObject containing a dictionary of <see cref="FeedbackEntry"/>.
    /// </summary>
    public class FeedbackContainer : HandyScriptableObject
    {
        [NonSerialized]
        private Dictionary<Guid, FeedbackEntry> _feedbackCache;

        [BoxGroup("Feedback")]
        [SerializeField]
        private InputActionAsset _actionAsset;

        [BoxGroup("Feedback")]
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
            return TryGetEntry(actionID, out entry);
        }

        public bool TrySprite(Guid actionID, string controlSchemeName, out Sprite sprite)
        {
            sprite = null;
            if (!TryGetEntry(actionID, out FeedbackEntry entry))
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
            if (!TryGetEntry(actionID, out FeedbackEntry entry))
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
            if (!TryGetEntry(actionID, out FeedbackEntry entry))
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
            if (!TryGetEntry(actionID, out FeedbackEntry entry))
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
                _feedbackCache = null;
            }
            UnityEditor.EditorUtility.SetDirty(this);
            return entry;
        }
#endif

        private bool TryGetEntry(Guid actionID, out FeedbackEntry entry)
        {
            EnsureFeedbackCache();
            return _feedbackCache.TryGetValue(actionID, out entry);
        }

        private void EnsureFeedbackCache()
        {
            if (_feedbackCache != null)
            {
                return;
            }

            _feedbackCache = new Dictionary<Guid, FeedbackEntry>(_feedbacks.Count);
            foreach (KeyValuePair<string, FeedbackEntry> pair in _feedbacks)
            {
                if (Guid.TryParse(pair.Key, out Guid actionId))
                {
                    _feedbackCache[actionId] = pair.Value;
                }
            }
        }
    }
}