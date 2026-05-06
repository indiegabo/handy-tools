using System;
using UnityEngine;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Base class for serializable animation events authored in animator state
    /// behaviours and dispatched through the HandyBus.
    /// </summary>
    [Serializable]
    public abstract class AnimatorBusEventBase : IAnimatorBusEvent
    {
        #region Runtime Context

        [NonSerialized] private Animator _animator;
        [NonSerialized] private RuntimeAnimatorController _runtimeAnimatorController;
        [NonSerialized] private AnimatorStateInfo _stateInfo;
        [NonSerialized] private int _layerIndex;
        [NonSerialized] private float _normalizedTime;
        [NonSerialized] private string _eventPath;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the animator that raised this event.
        /// </summary>
        public Animator Animator => _animator;

        /// <summary>
        /// Gets the controller active on the animator when the event fired.
        /// </summary>
        public RuntimeAnimatorController RuntimeAnimatorController =>
            _runtimeAnimatorController;

        /// <summary>
        /// Gets the animator state info captured at dispatch time.
        /// </summary>
        public AnimatorStateInfo StateInfo => _stateInfo;

        /// <summary>
        /// Gets the layer index that raised the event.
        /// </summary>
        public int LayerIndex => _layerIndex;

        /// <summary>
        /// Gets the normalized state time captured at dispatch time.
        /// </summary>
        public float NormalizedTime => _normalizedTime;

        /// <summary>
        /// Gets the stable logical path of the dispatched event.
        /// </summary>
        public string EventPath => _eventPath ?? string.Empty;

        /// <summary>
        /// Gets the full-path hash of the state that raised the event.
        /// </summary>
        public int StateFullPathHash => _stateInfo.fullPathHash;

        /// <summary>
        /// Gets the short-name hash of the state that raised the event.
        /// </summary>
        public int StateShortNameHash => _stateInfo.shortNameHash;

        /// <summary>
        /// Gets the tag hash of the state that raised the event.
        /// </summary>
        public int StateTagHash => _stateInfo.tagHash;

        #endregion

        #region Runtime API

        /// <summary>
        /// Creates a dispatch-time copy of the authored event payload.
        /// </summary>
        /// <returns>A shallow copy of the current event instance.</returns>
        public AnimatorBusEventBase CreateDispatchCopy()
        {
            return (AnimatorBusEventBase)MemberwiseClone();
        }

        /// <summary>
        /// Injects runtime state context into the dispatch-time event copy.
        /// </summary>
        /// <param name="eventPath">Stable logical event path.</param>
        /// <param name="animator">Animator that raised the event.</param>
        /// <param name="stateInfo">Animator state info at dispatch time.</param>
        /// <param name="layerIndex">Animator layer index.</param>
        public void SetRuntimeContext(
            string eventPath,
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex
        )
        {
            _eventPath = eventPath ?? string.Empty;
            _animator = animator;
            _runtimeAnimatorController = animator != null
                ? animator.runtimeAnimatorController
                : null;
            _stateInfo = stateInfo;
            _layerIndex = layerIndex;
            _normalizedTime = stateInfo.normalizedTime;
        }

        #endregion
    }
}