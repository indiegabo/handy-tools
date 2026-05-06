using System;
using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Maps one string animation event name to a UnityEvent callback list.
    /// </summary>
    [Serializable]
    public sealed class AnimationEventResponseBinding
    {
        #region Inspector

        [SerializeField] private string _eventName;

        [SerializeField] private UnityEvent _onAnimationEvent = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the string identifier raised by one animation state event.
        /// </summary>
        public string EventName => _eventName ?? string.Empty;

        /// <summary>
        /// Gets the UnityEvent callbacks invoked when the event name matches.
        /// </summary>
        public UnityEvent OnAnimationEvent => _onAnimationEvent;

        #endregion
    }
}