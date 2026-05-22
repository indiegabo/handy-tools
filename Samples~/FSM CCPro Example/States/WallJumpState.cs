
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Stores wall-jump context extracted from <see cref="WallSlideState"/>.
    /// The current sample does not yet run a dedicated wall-jump motion here, but
    /// the state documents how wall-derived data can be pulled from another state and
    /// cached for later use.
    /// </summary>
    [CreateAssetMenu(fileName = "WallJumpState", menuName = "HandyTools/FSM/CCPro/States/Wall Jump")]
    public class WallJumpState : ScriptableCCProState
    {
        #region Runtime State

        /// <summary>
        /// Cached reference to the wall slide state that produced the exit normal.
        /// </summary>
        private WallSlideState _wallSlideState;

        /// <summary>
        /// Stores the wall normal captured when the state becomes active.
        /// </summary>
        private Vector2 _currentNormal;

        #endregion

        #region State Lifecycle

        /// <summary>
        /// Resolves the wall slide state that provides the most recent wall normal.
        /// </summary>
        protected virtual void OnInit()
        {
            _wallSlideState = Brain.States.Get<WallSlideState>();
        }

        /// <summary>
        /// Copies the outgoing wall normal so wall-jump logic can use a stable launch reference.
        /// </summary>
        protected virtual void OnEnter()
        {
            // The wall slide state caches the wall normal on exit, so this state can read
            // that value without querying contacts that may already have changed.
            _currentNormal = _wallSlideState.RemanescenteNormal;

            // The commented line below documents the original direction of the prototype:
            // wall jump could optionally restore an extra airborne jump on entry.
            // _notGroundedJumpsLeft = VerticalMovement.availableNotGroundedJumps + 1;
        }

        #endregion
    }
}