using System;
using UnityEngine;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Defines one string-based event emitted by a state each time the
    /// configured threshold is reached within a loop.
    /// </summary>
    [Serializable]
    public sealed class AnimationStateEventTrigger
    {
        #region Inspector

        [SerializeField] private string _eventName;

        [SerializeField]
        [Range(0f, 1f)]
        private float _triggerTime;

        #endregion

        #region State

        [NonSerialized] private bool _hasTriggered;

        [NonSerialized] private int _currentLoop;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the event name forwarded to the local receiver.
        /// </summary>
        public string EventName => _eventName ?? string.Empty;

        /// <summary>
        /// Gets the normalized time threshold that emits the event.
        /// </summary>
        public float TriggerTime => Mathf.Clamp01(_triggerTime);

        #endregion

        #region Public API

        /// <summary>
        /// Resets the per-state runtime trigger flag.
        /// </summary>
        public void ResetRuntimeState()
        {
            _hasTriggered = false;
            _currentLoop = -1;
        }

        /// <summary>
        /// Emits the configured event once when the normalized time crosses the
        /// configured threshold within the current loop.
        /// </summary>
        /// <param name="receiver">Receiver attached to the animator owner.</param>
        /// <param name="currentTime">Current full normalized time for the state.</param>
        /// <returns>
        /// True when the trigger threshold was crossed during this call.
        /// </returns>
        public bool TryTrigger(AnimationEventReceiver receiver, float currentTime)
        {
            int loop = Mathf.Max(0, Mathf.FloorToInt(currentTime));
            float normalizedLoopTime = Mathf.Repeat(currentTime, 1f);

            if (_currentLoop != loop)
            {
                _currentLoop = loop;
                _hasTriggered = false;
            }

            if (_hasTriggered || normalizedLoopTime < TriggerTime)
            {
                return false;
            }

            _hasTriggered = true;

            if (!string.IsNullOrWhiteSpace(_eventName))
            {
                receiver?.OnAnimationEventTriggered(_eventName);
            }

            return true;
        }

        #endregion
    }
}