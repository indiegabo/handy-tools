using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Receives string-based animation events from state behaviours and routes
    /// them to configured UnityEvents on the Animator owner.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimationEventReceiver : MonoBehaviour
    {
        #region Inspector

        [SerializeField]
        private List<AnimationEventResponseBinding>
            _animationEvents = new();

        #endregion

        #region State

        private readonly Dictionary<string, List<AnimationEventResponseBinding>>
            _bindingsByEventName = new(StringComparer.Ordinal);

        private bool _cacheBuilt;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Rebuilds the receiver cache whenever the component becomes active.
        /// </summary>
        private void OnEnable()
        {
            RebuildCache();
        }

        /// <summary>
        /// Marks the cached lookup as stale after inspector edits.
        /// </summary>
        private void OnValidate()
        {
            _cacheBuilt = false;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Invokes every configured UnityEvent that matches the provided name.
        /// </summary>
        /// <param name="eventName">
        /// Name raised by the active animation state behaviour.
        /// </param>
        public void OnAnimationEventTriggered(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            EnsureCacheBuilt();

            if (!_bindingsByEventName.TryGetValue(eventName, out var bindings))
            {
                return;
            }

            for (int index = 0; index < bindings.Count; index++)
            {
                bindings[index].OnAnimationEvent?.Invoke();
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Rebuilds the event-name lookup used during runtime dispatch.
        /// </summary>
        private void RebuildCache()
        {
            _bindingsByEventName.Clear();

            for (int index = 0; index < _animationEvents.Count; index++)
            {
                AnimationEventResponseBinding binding = _animationEvents[index];
                if (binding == null || string.IsNullOrWhiteSpace(binding.EventName))
                {
                    continue;
                }

                if (!_bindingsByEventName.TryGetValue(
                        binding.EventName,
                        out List<AnimationEventResponseBinding> bindings
                    ))
                {
                    bindings = new List<AnimationEventResponseBinding>();
                    _bindingsByEventName.Add(binding.EventName, bindings);
                }

                bindings.Add(binding);
            }

            _cacheBuilt = true;
        }

        /// <summary>
        /// Ensures the runtime lookup cache exists before dispatching events.
        /// </summary>
        private void EnsureCacheBuilt()
        {
            if (_cacheBuilt)
            {
                return;
            }

            RebuildCache();
        }

        #endregion
    }
}