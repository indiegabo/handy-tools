using UnityEngine;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Raises one string-based event from an animator state each time the
    /// configured normalized threshold is reached within a loop and forwards
    /// it to a local receiver component.
    /// </summary>
    public sealed class AnimationEventStateBehaviour : StateMachineBehaviour
    {
        #region Inspector

        [SerializeField] private AnimationStateEventTrigger _event = new();

        #endregion

        #region State

        private AnimationEventReceiver _receiver;

        #endregion

        #region Animator Lifecycle

        /// <summary>
        /// Resolves the receiver on the animator owner and resets state-local
        /// trigger flags.
        /// </summary>
        /// <param name="animator">Animator evaluating this state.</param>
        /// <param name="stateInfo">Current state info.</param>
        /// <param name="layerIndex">Animator layer index.</param>
        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex
        )
        {
            _receiver = animator.GetComponent<AnimationEventReceiver>();

            _event?.ResetRuntimeState();
            DispatchReadyEvent(0f);
        }

        /// <summary>
        /// Evaluates all configured triggers against the current normalized
        /// full state time.
        /// </summary>
        /// <param name="animator">Animator evaluating this state.</param>
        /// <param name="stateInfo">Current state info.</param>
        /// <param name="layerIndex">Animator layer index.</param>
        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex
        )
        {
            DispatchReadyEvent(stateInfo.normalizedTime);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Attempts to dispatch the configured trigger when it is ready.
        /// </summary>
        /// <param name="normalizedTime">
        /// Current full normalized time for the active state.
        /// </param>
        private void DispatchReadyEvent(float normalizedTime)
        {
            _event?.TryTrigger(_receiver, normalizedTime);
        }

        #endregion
    }
}