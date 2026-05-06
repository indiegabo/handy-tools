using UnityEngine;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Raises one typed HandyBus event from an animator state each time the
    /// configured normalized threshold is reached within a loop.
    /// </summary>
    public sealed class AnimationEventBusStateBehaviour : StateMachineBehaviour
    {
        #region Inspector

        [SerializeField] private AnimationEventBusStateTrigger _event = new();

        #endregion

        #region Animator Lifecycle

        /// <summary>
        /// Resets state-local trigger flags when the animator enters the state.
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
            _event?.ResetRuntimeState();
            DispatchReadyEvent(animator, stateInfo, layerIndex, 0f);
        }

        /// <summary>
        /// Evaluates all configured typed triggers against the current
        /// full normalized state time.
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
            DispatchReadyEvent(
                animator,
                stateInfo,
                layerIndex,
                stateInfo.normalizedTime
            );
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Attempts to dispatch the configured typed trigger when it is ready.
        /// </summary>
        /// <param name="animator">Animator evaluating this state.</param>
        /// <param name="stateInfo">Current state info.</param>
        /// <param name="layerIndex">Animator layer index.</param>
        /// <param name="normalizedTime">Current full normalized state time.</param>
        private void DispatchReadyEvent(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex,
            float normalizedTime
        )
        {
            _event?.TryTrigger(
                animator,
                stateInfo,
                layerIndex,
                normalizedTime
            );
        }

        #endregion
    }
}