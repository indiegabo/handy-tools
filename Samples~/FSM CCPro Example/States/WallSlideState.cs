using Lightbug.CharacterControllerPro.Core;
using Lightbug.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Recreates the CCPro 2D wall slide behaviour.
    /// The state validates that the actor is still attached to the same wall,
    /// applies either grab or slide motion, and hands the exit wall normal to
    /// the next state when a wall jump is requested.
    /// </summary>
    [CreateAssetMenu(fileName = "WallSlideState", menuName = "HandyTools/FSM/CCPro/States/Wall Slide")]
    public class WallSlideState : ScriptableCCProState
    {
        #region Inspector

        [SerializeField]
        private InputActionReference _movementAction;

        [SerializeField]
        private InputActionReference _jumpAction;

        [SerializeField]
        private InputActionReference _crouchAction;

        [SerializeField]
        private InputActionReference _runAction;

        #endregion

        #region Runtime State

        /// <summary>
        /// Tracks whether the state is leaving through a wall jump request.
        /// </summary>
        protected bool _wallJump = false;

        /// <summary>
        /// Stores the actor size so temporary wall-slide resizing can be restored on exit.
        /// </summary>
        protected Vector2 _initialSize = Vector2.zero;

        /// <summary>
        /// Caches the wall normal that produced the wall jump exit.
        /// </summary>
        protected Vector2 _remanescenteNormal;

        #endregion

        #region Configuration Access

        /// <summary>
        /// Gets the stats build that tunes slide, grab, and wall jump behavior.
        /// </summary>
        protected WallSlideStats WallSlideStats => Brain?.Stats.Get<WallSlideStats>();

        /// <summary>
        /// Gets the current movement vector resolved from the FSM input cache.
        /// </summary>
        protected Vector2 MovementInput => ReadVector2Value(_movementAction);

        /// <summary>
        /// Gets whether the current movement vector is non-zero.
        /// </summary>
        protected bool MovementDetected => MovementInput != Vector2.zero;

        /// <summary>
        /// Gets whether the player is currently grabbing the wall instead of free-sliding.
        /// </summary>
        protected bool IsGrabbing =>
            ReadButtonValue(_runAction) && WallSlideStats.EnableGrab;

        /// <summary>
        /// Validates the action references required by this wall slide state.
        /// </summary>
        private void ValidateInputBindings()
        {
            ValidateInputAction(_movementAction, "Movement");
            ValidateInputAction(_jumpAction, "Jump");
            ValidateInputAction(_crouchAction, "Crouch");
            ValidateInputAction(_runAction, "Run");
        }

        /// <summary>
        /// Validates one required action reference.
        /// </summary>
        /// <param name="actionReference">Action reference to validate.</param>
        /// <param name="actionName">Human-readable action name.</param>
        private void ValidateInputAction(
            InputActionReference actionReference,
            string actionName)
        {
            if (actionReference != null && actionReference.action != null)
            {
                return;
            }

            ThrowStateFailure(
                $"WallSlideState requires the '{actionName}' InputActionReference to be assigned.");
        }

        /// <summary>
        /// Resolves one button value from the FSM input cache.
        /// </summary>
        /// <param name="actionReference">Button action to read.</param>
        /// <returns>The current button value, or false when unavailable.</returns>
        private bool ReadButtonValue(InputActionReference actionReference)
        {
            return Brain != null
                && Brain.Input.TryGetButton(actionReference, out bool value)
                && value;
        }

        /// <summary>
        /// Resolves one vector input from the FSM input cache.
        /// </summary>
        /// <param name="actionReference">Vector action to read.</param>
        /// <returns>The current vector value, or zero when unavailable.</returns>
        private Vector2 ReadVector2Value(InputActionReference actionReference)
        {
            return Brain != null
                && Brain.Input.TryGetVector2(actionReference, out Vector2 value)
                ? value
                : Vector2.zero;
        }

        /// <summary>
        /// Resolves one button snapshot from the FSM input cache.
        /// </summary>
        /// <param name="actionReference">Button action to inspect.</param>
        /// <param name="snapshot">The resolved snapshot.</param>
        /// <returns>True when a valid button snapshot was found.</returns>
        private bool TryGetButtonSnapshot(
            InputActionReference actionReference,
            out FSMInputSnapshot snapshot)
        {
            if (Brain == null
                || !Brain.Input.TryGetSnapshot(actionReference, out snapshot)
                || snapshot.ValueKind != FSMInputValueKind.Button)
            {
                snapshot = default;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets whether one button action started recently enough to still be
        /// relevant for the next fixed-step evaluation.
        /// </summary>
        /// <param name="actionReference">Button action to inspect.</param>
        /// <returns>True when the button started within the grace window.</returns>
        private bool WasButtonStarted(InputActionReference actionReference)
        {
            return Brain != null
                && Brain.Input.HasRecentButtonStart(
                    actionReference,
                    GetButtonStartGraceWindow());
        }

        /// <summary>
        /// Marks one recent button start as consumed by this wall-slide state.
        /// </summary>
        /// <param name="actionReference">Button action to consume.</param>
        /// <returns>True when one recent button press was consumed.</returns>
        private bool ConsumeButtonStart(InputActionReference actionReference)
        {
            return Brain != null
                && Brain.Input.TryConsumeRecentButtonStart(
                    actionReference,
                    GetButtonStartGraceWindow());
        }

        /// <summary>
        /// Gets the short realtime grace window that bridges Update-origin input
        /// events into the next fixed-step evaluation.
        /// </summary>
        /// <returns>The maximum accepted button-start age in seconds.</returns>
        private static float GetButtonStartGraceWindow()
        {
            return Mathf.Max(Time.fixedDeltaTime + 0.005f, 0.025f);
        }

        /// <summary>
        /// Gets whether the active animator exposes one parameter with the given
        /// name and type.
        /// </summary>
        /// <param name="parameterName">Animator parameter name to resolve.</param>
        /// <param name="parameterType">Expected parameter type.</param>
        /// <returns>True when the parameter exists on the current controller.</returns>
        private bool HasAnimatorParameter(
            string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            if (string.IsNullOrWhiteSpace(parameterName)
                || CharacterActor.Animator == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = CharacterActor.Animator.parameters;

            for (int index = 0; index < parameters.Length; index++)
            {
                AnimatorControllerParameter parameter = parameters[index];

                if (parameter.type == parameterType
                    && string.Equals(
                        parameter.name,
                        parameterName,
                        System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Writes one float parameter only when the current controller exposes it.
        /// </summary>
        /// <param name="parameterName">Animator parameter name.</param>
        /// <param name="value">Float value to write.</param>
        private void SetAnimatorFloatIfAvailable(string parameterName, float value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                CharacterActor.Animator.SetFloat(parameterName, value);
            }
        }

        /// <summary>
        /// Writes one bool parameter only when the current controller exposes it.
        /// </summary>
        /// <param name="parameterName">Animator parameter name.</param>
        /// <param name="value">Bool value to write.</param>
        private void SetAnimatorBoolIfAvailable(string parameterName, bool value)
        {
            if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                CharacterActor.Animator.SetBool(parameterName, value);
            }
        }

        #endregion

        #region Exposed Data

        /// <summary>
        /// Gets the wall normal captured when leaving through a wall jump.
        /// </summary>
        public Vector2 RemanescenteNormal => _remanescenteNormal;

        #endregion

        #region Transition Evaluation

        /// <summary>
        /// Evaluates whether the state should return to normal locomotion.
        /// </summary>
        /// <returns>
        /// True when the actor lost the wall, reached the ground, started crouching,
        /// or requested a wall jump.
        /// </returns>
        protected bool CheckNormalMovementTransition()
        {
            // Any condition that invalidates a meaningful wall slide immediately hands
            // control back to locomotion.
            if (ReadButtonValue(_crouchAction)
                || CharacterActor.IsGrounded
                || !CharacterActor.WallCollision
                || !CheckCenterRay())
            {
                return true;
            }

            if (ConsumeButtonStart(_jumpAction))
            {
                // The actual wall jump impulse is applied on exit so locomotion receives
                // the actor after the launch velocity has already been written.
                _wallJump = true;
                return true;
            }

            return false;
        }

        #endregion

        #region State Lifecycle

        /// <summary>
        /// Resolves configuration and wires the exit transition back to locomotion.
        /// </summary>
        protected virtual void OnInit()
        {
            ValidateInputBindings();

            if (WallSlideStats == null)
            {
                ThrowStateFailure(
                    "WallSlideState requires a WallSlideStats asset to be resolved "
                    + "by Brain.Stats. Register one in an FSMStatsRegistry or assign "
                    + "a runtime override before initialization.");
            }

            AddTransition(
                CheckNormalMovementTransition,
                Brain.States.Get<NormalMovementState>());
            SortTransitions();
        }

        /// <summary>
        /// Validates whether the actor can enter the wall slide state.
        /// </summary>
        /// <param name="fromState">The state the machine is leaving.</param>
        /// <returns>True when the actor is descending against a valid wall.</returns>
        public override bool CanEnter(IState fromState)
        {
            // Ascending contacts are ignored so the state only models the downward wall
            // interaction seen in the original 2D demo.
            if (CharacterActor.IsAscending)
            {
                return false;
            }

            if (!CharacterActor.WallCollision)
            {
                return false;
            }

            if (WallSlideStats.FilterByTag)
            {
                if (!CharacterActor.WallContact.gameObject.CompareTag(WallSlideStats.WallTag))
                    return false;
            }

            if (!CheckCenterRay())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initializes the slide state and aligns the actor to the contacted wall.
        /// </summary>
        protected virtual void OnEnter()
        {
            _remanescenteNormal = Vector2.zero;
            _wallJump = false;

            // Wall slide writes velocity directly, so root motion would only fight the state.
            CharacterActor.UseRootMotion = false;

            // The incoming velocity is partially preserved to keep the transition from
            // locomotion or falling from feeling abrupt.
            CharacterActor.Velocity *= WallSlideStats.InitialInertia;

            // Facing opposite to the wall keeps the sprite readable while attached.
            CharacterActor.SetYaw(-CharacterActor.WallContact.normal);

            if (WallSlideStats.ModifySize)
            {
                // The body can be compressed while sliding and restored on exit.
                _initialSize = CharacterActor.BodySize;
                CharacterActor.SetSize(
                    new Vector2(_initialSize.x, WallSlideStats.Height),
                    CharacterActor.SizeReferenceType.Center);
            }
        }

        /// <summary>
        /// Restores temporary wall-slide state and applies the wall jump impulse when requested.
        /// </summary>
        protected virtual void OnExit()
        {
            if (_wallJump)
            {
                _wallJump = false;

                // The next state can inspect the exit normal if it needs context about
                // which wall launched the character.
                _remanescenteNormal = CharacterActor.WallContact.normal;

                // Turning around before applying the launch keeps the actor orientation in
                // sync with the outgoing jump direction.
                CharacterActor.TurnAround();

                CharacterActor.Velocity = WallSlideStats.JumpVerticalVelocity
                    * CharacterActor.Up
                    + WallSlideStats.JumpNormalVelocity
                    * CharacterActor.WallContact.normal;
            }

            if (WallSlideStats.ModifySize)
            {
                CharacterActor.SizeReferenceType sizeReferenceType = CharacterActor.IsGrounded ?
                    CharacterActor.SizeReferenceType.Bottom : CharacterActor.SizeReferenceType.Top;

                // The original body size is restored using the correct reference point for
                // the current grounded state.
                CharacterActor.SetSize(_initialSize, sizeReferenceType);
            }
        }

        /// <summary>
        /// Applies either wall climb motion or passive slide acceleration.
        /// </summary>
        protected virtual void OnFixedTick()
        {
            float dt = Time.deltaTime;

            if (IsGrabbing)
            {
                // The horizontal climb axis is projected onto the wall plane so player
                // input moves along the wall instead of pushing into it.
                Vector3 rightDirection = Vector3.ProjectOnPlane(MovementReferenceRight, CharacterActor.WallContact.normal);
                rightDirection.Normalize();

                Vector3 upDirection = CharacterActor.Up;
                Vector3 targetVelocity = WallSlideStats.EnableClimb
                    ? MovementInput.x
                        * WallSlideStats.WallClimbHorizontalSpeed
                        * rightDirection
                        + MovementInput.y
                        * WallSlideStats.WallClimbVerticalSpeed
                        * upDirection
                    : Vector3.zero;

                CharacterActor.Velocity = Vector3.MoveTowards(
                    CharacterActor.Velocity,
                    targetVelocity,
                    WallSlideStats.WallClimbAcceleration * dt
                );
            }
            else
            {
                // Free sliding only accelerates downward along the actor up axis.
                CharacterActor.VerticalVelocity += dt * WallSlideStats.SlideAcceleration * -CharacterActor.Up;
            }
        }

        /// <summary>
        /// Synchronizes animation parameters after fixed simulation finishes.
        /// </summary>
        protected virtual void OnPostFixedTick()
        {
            if (!CharacterActor.IsAnimatorValid())
            {
                return;
            }

            // Animator values are sent after simulation so they describe the final state
            // of the current frame rather than stale pre-simulation data.
            SetAnimatorFloatIfAvailable(
                WallSlideStats.HorizontalVelocityParameter,
                CharacterActor.LocalVelocity.x);
            SetAnimatorFloatIfAvailable(
                WallSlideStats.VerticalVelocityParameter,
                CharacterActor.LocalVelocity.y);
            SetAnimatorBoolIfAvailable(
                WallSlideStats.GrabParameter,
                IsGrabbing);
            SetAnimatorBoolIfAvailable(
                WallSlideStats.MovementDetectedParameter,
                MovementDetected);
        }

        /// <summary>
        /// Updates IK look-at data so the character gaze follows wall-climb motion.
        /// </summary>
        /// <param name="layerIndex">Animator IK layer index provided by the runtime hook.</param>
        protected virtual void OnTickIK(int layerIndex)
        {
            if (!CharacterActor.IsAnimatorValid())
            {
                return;
            }

            if (IsGrabbing && MovementDetected)
            {
                // Looking toward the current velocity direction gives the grab/climb pose
                // a more intentional animation target.
                CharacterActor.Animator.SetLookAtWeight(Mathf.Clamp01(CharacterActor.Velocity.magnitude), 0f, 0.2f);
                CharacterActor.Animator.SetLookAtPosition(CharacterActor.Position + CharacterActor.Velocity);
            }
            else
            {
                CharacterActor.Animator.SetLookAtWeight(0f);
            }
        }

        #endregion

        #region Contact Validation

        /// <summary>
        /// Casts a short ray toward the contacted wall to verify the actor is still centered on it.
        /// </summary>
        /// <returns>True when the ray confirms the current wall contact.</returns>
        protected virtual bool CheckCenterRay()
        {
            HitInfoFilter filter = new HitInfoFilter(
                CharacterActor.PhysicsComponent.CollisionLayerMask,
                true,
                true);

            CharacterActor.PhysicsComponent.Raycast(
                out HitInfo centerRayHitInfo,
                CharacterActor.Center,
                -CharacterActor.WallContact.normal
                    * 1.2f
                    * CharacterActor.BodySize.x,
                in filter);

            return centerRayHitInfo.hit
                && centerRayHitInfo.transform.gameObject
                    == CharacterActor.WallContact.gameObject;
        }

        #endregion
    }
}