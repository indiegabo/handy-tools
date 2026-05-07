using Lightbug.CharacterControllerPro.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Recreates the 2D Character Controller Pro dash behaviour inside HandyFSM.
    /// The state snapshots a dash direction on enter, drives velocity from a
    /// normalized curve during fixed simulation, and consumes or restores air
    /// dash charges through the shared blackboard.
    /// </summary>
    [CreateAssetMenu(fileName = "DashState", menuName = "HandyTools/FSM/CCPro/States/Dash")]
    public class DashState : ScriptableCCProState
    {
        #region Dependencies And Runtime State

        /// <summary>
        /// Cached locomotion state used as the natural dash exit target.
        /// </summary>
        private NormalMovementState _normalMovementState;

        /// <summary>
        /// Local mirror of the air dash count stored in the shared blackboard.
        /// </summary>
        private int _airDashesLeft;

        /// <summary>
        /// Normalized time cursor used to sample the dash movement curve.
        /// </summary>
        private float _dashCursor;

        /// <summary>
        /// Frozen world-space dash direction captured on enter.
        /// </summary>
        private Vector3 _dashDirection = Vector3.right;

        /// <summary>
        /// Tracks when the state has completed its dash window and can transition out.
        /// </summary>
        private bool _isDone = true;

        /// <summary>
        /// Stores the environment speed multiplier sampled when the dash starts.
        /// </summary>
        private float _currentSpeedMultiplier = 1f;

        #endregion

        #region Configuration Access

        /// <summary>
        /// Gets the resolved dash stats build.
        /// </summary>
        private DashStats DashStats => Brain?.Stats.Get<DashStats>();

        #endregion

        #region State Lifecycle

        /// <summary>
        /// Resolves dependencies and configures the natural dash exit transition.
        /// </summary>
        protected virtual void OnInit()
        {
            if (DashStats == null)
            {
                ThrowStateFailure(
                    "DashState requires a DashStats asset to be resolved by "
                    + "Brain.Stats. Register one in an FSMStatsRegistry or assign "
                    + "a runtime override before initialization.");
            }

            // Dash always exits back to normal locomotion once its time window finishes.
            _normalMovementState = Brain.States.Get<NormalMovementState>();

            if (_normalMovementState == null)
            {
                ThrowStateFailure(
                    "DashState requires NormalMovementState to be loaded in the FSMBrain.");
            }

            // The shared charge pool is initialized here so any later airborne dash can
            // read a deterministic value from the blackboard.
            ResetAirDashes();
            AddTransition(IsDashComplete, _normalMovementState, 100);
            SortTransitions();
        }

        /// <summary>
        /// Validates whether the actor can start a dash.
        /// </summary>
        /// <param name="fromState">The state the machine is leaving.</param>
        /// <returns>True when a dash can start.</returns>
        public override bool CanEnter(IState fromState)
        {
            // Ground dashes are always allowed. Air dashes consume a shared resource so
            // they only remain available while the blackboard counter is above zero.
            return CharacterActor.IsGrounded || ReadAirDashesLeft() > 0;
        }

        /// <summary>
        /// Initializes the dash runtime data and consumes one air dash when needed.
        /// </summary>
        protected virtual void OnEnter()
        {
            // Some dash presets force an airborne state so ground snapping or similar
            // grounded logic cannot interrupt the dash arc.
            if (DashStats.ForceNotGrounded)
            {
                CharacterActor.alwaysNotGrounded = true;
            }

            // Root motion is disabled because the dash owns velocity explicitly.
            CharacterActor.UseRootMotion = false;

            // The material multiplier is sampled once on enter so the dash remains
            // stable for its whole duration instead of changing every frame.
            _currentSpeedMultiplier = ResolveSpeedMultiplier();

            if (!CharacterActor.IsGrounded)
            {
                // Air dashes consume a charge immediately when the state starts.
                WriteAirDashesLeft(ReadAirDashesLeft() - 1);
            }

            // The dash keeps the facing captured on enter, which matches the demo's
            // feeling of a committed burst instead of steering mid-dash.
            _dashDirection = CharacterActor.Forward;
            ResetDash();
        }

        /// <summary>
        /// Restores the forced not-grounded flag after the dash ends.
        /// </summary>
        protected virtual void OnExit()
        {
            // The flag is only borrowed for the dash window and must be restored so
            // subsequent states can rely on the real grounding result again.
            if (DashStats.ForceNotGrounded)
            {
                CharacterActor.alwaysNotGrounded = false;
            }
        }

        /// <summary>
        /// Updates the dash velocity during the fixed tick.
        /// </summary>
        protected virtual void OnFixedTick()
        {
            float dt = Time.deltaTime;

            // The curve is sampled with a normalized cursor so designers can shape the
            // dash as a burst, plateau, or taper without changing state logic.
            float curveValue = DashStats.MovementCurve.Evaluate(_dashCursor);

            // Dash velocity is written as an absolute value every frame so prior motion
            // does not leak into the dash behaviour.
            CharacterActor.Velocity = DashStats.InitialVelocity
                * _currentSpeedMultiplier
                * curveValue
                * _dashDirection;

            // The cursor progresses from 0 to 1 across the configured duration.
            _dashCursor += DashStats.Duration > 0f ? dt / DashStats.Duration : 1f;

            if (_dashCursor >= 1f)
            {
                // Mark completion and reset the cursor so the next dash starts cleanly.
                _isDone = true;
                _dashCursor = 0f;
            }
        }

        /// <summary>
        /// Applies post-simulation completion checks such as wall contact cancel.
        /// </summary>
        /// <param name="dt">The current simulation delta time.</param>
        protected virtual void OnPostCharacterSimulation(float dt)
        {
            if (DashStats.CancelOnContact)
            {
                // Contact-based cancellation is evaluated after simulation so wall hits
                // from the current frame are already reflected in the actor contacts.
                _isDone |= CharacterActor.WallContacts.Count != 0;
            }
        }

        #endregion

        #region Shared Resource API

        /// <summary>
        /// Restores the full amount of available air dashes.
        /// </summary>
        public void ResetAirDashes()
        {
            if (DashStats == null)
            {
                return;
            }

            // Grounded recovery and initialization both funnel through this method so
            // there is a single place that defines the maximum air dash count.
            WriteAirDashesLeft(DashStats.AvailableNotGroundedDashes);
        }

        #endregion

        #region Transition Evaluation

        /// <summary>
        /// Evaluates whether the dash is complete.
        /// </summary>
        /// <returns>True when the dash can return to locomotion.</returns>
        private bool IsDashComplete()
        {
            return _isDone;
        }

        #endregion

        #region Runtime Helpers

        /// <summary>
        /// Computes the current material-based speed multiplier.
        /// </summary>
        /// <returns>The multiplier applied to dash speed.</returns>
        private float ResolveSpeedMultiplier()
        {
            if (DashStats.IgnoreSpeedMultipliers)
            {
                return 1f;
            }

            // Ground dashes consider both surface and volume modifiers, while airborne
            // dashes can only inherit the active volume settings.
            return CharacterActor.IsGrounded
                ? CurrentSurfaceModifiers.SpeedMultiplier
                    * CurrentVolumeModifiers.SpeedMultiplier
                : CurrentVolumeModifiers.SpeedMultiplier;
        }

        /// <summary>
        /// Resets the dash runtime cursor and velocity.
        /// </summary>
        private void ResetDash()
        {
            // Resetting velocity avoids carrying residual momentum into the first dash frame.
            CharacterActor.Velocity = Vector3.zero;
            _isDone = false;
            _dashCursor = 0f;
        }

        #endregion

        #region Blackboard Synchronization

        /// <summary>
        /// Reads the shared air dash count from the blackboard fallback.
        /// </summary>
        /// <returns>The remaining air dash count.</returns>
        private int ReadAirDashesLeft()
        {
            if (Brain != null
                && Brain.Blackboard.TryGetValue(
                    CCPro2DBlackboardKeys.AirDashesLeft,
                    out int value))
            {
                // The local cache mirrors the blackboard so repeated reads stay cheap
                // while still accepting external updates.
                _airDashesLeft = value;
            }

            return _airDashesLeft;
        }

        /// <summary>
        /// Writes the shared air dash count to the blackboard fallback.
        /// </summary>
        /// <param name="value">The remaining air dash count.</param>
        private void WriteAirDashesLeft(int value)
        {
            // The counter is clamped defensively because transition logic only needs a
            // non-negative remaining charge count.
            _airDashesLeft = Mathf.Max(0, value);

            if (Brain != null)
            {
                Brain.Blackboard.SetValue(
                    CCPro2DBlackboardKeys.AirDashesLeft,
                    _airDashesLeft);
            }
        }

        #endregion
    }
}