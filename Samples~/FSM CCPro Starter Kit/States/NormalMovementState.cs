using System;
using Lightbug.CharacterControllerPro.Core;
using CCProParameters = IndieGabo.HandyTools.FSMModule.CCPro.Parameters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Recreates the main CCPro 2D locomotion state inside HandyFSM.
    /// This state owns the grounded and airborne locomotion loop, including
    /// planar movement, gravity, jumps, crouch, looking direction, animation
    /// synchronization, and the shared airborne resources used by the dash
    /// and wall states.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NormalMovementState",
        menuName = "HandyTools/FSM/CCPro/States/Normal Movement")]
    public class NormalMovementState : ScriptableCCProState
    {
        #region Constants And Types

        /// <summary>
        /// Legacy run signal name preserved for parity with the original sample.
        /// </summary>
        protected static readonly string WantsToRunSignal = "wantsToRun";

        /// <summary>
        /// Mirrors the minimum downward offset used by the original CCPro jump-down
        /// branch to separate the actor from one-way ground.
        /// </summary>
        private const float JumpDownColliderBottomOffset = 0.1f;

        /// <summary>
        /// Describes which jump branch is valid on the current frame.
        /// </summary>
        public enum JumpResult
        {
            /// <summary>
            /// No jump branch is currently valid.
            /// </summary>
            Invalid,

            /// <summary>
            /// A grounded or coyote jump should be executed.
            /// </summary>
            Grounded,

            /// <summary>
            /// An additional airborne jump should be executed.
            /// </summary>
            NotGrounded
        }

        #endregion

        #region Inspector

        [SerializeField]
        private InputActionReference _movementAction;

        [SerializeField]
        private InputActionReference _jumpAction;

        [SerializeField]
        private InputActionReference _crouchAction;

        [SerializeField]
        private InputActionReference _runAction;

        [SerializeField]
        private InputActionReference _dashAction;

        #endregion

        #region Runtime State

        // Shared airborne resources.
        protected int _notGroundedJumpsLeft = 0;
        protected DashState _dashState;

        // Jump flow state.
        protected bool _isAllowedToCancelJump = false;
        protected bool _groundedJumpAvailable = true;
        protected Vector3 _jumpDirection = default;

        // Horizontal locomotion intent and speed state.
        protected bool _wantToRun = false;
        protected float _currentPlanarSpeedLimit = 0f;
        protected CCProParameters.PlanarMovementParameters.PlanarMovementProperties
            _currentMotion =
            new();

        // Looking and body-size state.
        protected Vector3 _targetLookingDirection = default;
        protected float _targetHeight = 1f;
        protected bool _wantToCrouch = false;
        protected bool _isCrouched = false;
        protected float _minimumCrouchHeightRatio = 0f;

        // Temporary air-control reduction used after wall jumps.
        private bool _reducedAirControlFlag = false;
        private float _reducedAirControlInitialTime = 0f;
        private float _reductionDuration = 0.5f;

        #endregion

        #region Configuration Access

        /// <summary>
        /// Gets the resolved locomotion stats build for this actor branch.
        /// </summary>
        protected NormalMovementStats MovementStats =>
            Brain?.Stats.Get<NormalMovementStats>();

        /// <summary>
        /// Gets the planar movement tuning block.
        /// </summary>
        protected CCProParameters.PlanarMovementParameters PlanarMovement =>
            MovementStats.PlanarMovement;

        /// <summary>
        /// Gets the vertical movement tuning block.
        /// </summary>
        protected CCProParameters.VerticalMovementParameters VerticalMovement =>
            MovementStats.VerticalMovement;

        /// <summary>
        /// Gets the crouch tuning block.
        /// </summary>
        protected CCProParameters.CrouchParameters Crouch => MovementStats.Crouch;

        /// <summary>
        /// Gets the crouch height ratio clamped so the body never becomes thinner
        /// than its current width.
        /// </summary>
        protected float CrouchHeightRatio =>
            Mathf.Max(_minimumCrouchHeightRatio, Crouch.heightRatio);

        /// <summary>
        /// Gets the looking direction tuning block.
        /// </summary>
        protected CCProParameters.LookingDirectionParameters LookingDirection =>
            MovementStats.LookingDirection;

        /// <summary>
        /// Gets whether unstable ground should currently reject a grounded jump.
        /// </summary>
        protected bool UnstableGroundedJumpAvailable =>
            !VerticalMovement.canJumpOnUnstableGround
            && CharacterActor.CurrentState == CharacterActorState.UnstableGrounded;

        #endregion

        #region Events

        /// <summary>
        /// Event raised whenever any jump branch is executed.
        /// </summary>
        public event Action OnJumpPerformed;

        /// <summary>
        /// Event raised when a grounded or coyote jump is executed.
        /// </summary>
        public event Action<bool> OnGroundedJumpPerformed;

        /// <summary>
        /// Event raised when an additional airborne jump is executed.
        /// </summary>
        public event Action<int> OnNotGroundedJumpPerformed;

        #endregion

        #region Input Access

        /// <summary>
        /// Gets the condition that starts the dash state.
        /// </summary>
        protected Func<bool> DashConditions => () => ConsumeButtonStart(_dashAction);

        /// <summary>
        /// Gets the condition that starts the wall slide state.
        /// </summary>
        protected Func<bool> WallSlideConditions => () =>
            CharacterActor.WallCollision
            && !CharacterActor.IsAscending
            && !CharacterActor.IsGrounded
            && !ReadButtonValue(_crouchAction);

        /// <summary>
        /// Gets the current movement vector resolved from the FSM input cache.
        /// </summary>
        protected Vector2 MovementInput => ReadVector2Value(_movementAction);

        /// <summary>
        /// Gets the movement vector projected into the active movement reference space.
        /// </summary>
        protected Vector3 MovementReferenceInput => ResolveMovementReferenceInput();

        /// <summary>
        /// Gets whether the current movement input points downward.
        /// </summary>
        protected bool MovementDown => MovementInput.y < 0f;

        /// <summary>
        /// Gets the elapsed time since the jump action last started.
        /// </summary>
        protected float JumpStartedElapsedTime =>
            ReadButtonStartedElapsedTime(_jumpAction);

        /// <summary>
        /// Validates the action references required by this locomotion state.
        /// </summary>
        private void ValidateInputBindings()
        {
            ValidateInputAction(_movementAction, "Movement");
            ValidateInputAction(_jumpAction, "Jump");
            ValidateInputAction(_crouchAction, "Crouch");
            ValidateInputAction(_runAction, "Run");
            ValidateInputAction(_dashAction, "Dash");
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
                $"NormalMovementState requires the '{actionName}' InputActionReference to be assigned.");
        }

        /// <summary>
        /// Resolves a button value from the FSM input cache.
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
        /// Resolves the elapsed time since one button action last started.
        /// </summary>
        /// <param name="actionReference">Button action to inspect.</param>
        /// <returns>The elapsed time since the latest press, or infinity when unavailable.</returns>
        private float ReadButtonStartedElapsedTime(
            InputActionReference actionReference)
        {
            return TryGetButtonSnapshot(actionReference, out FSMInputSnapshot snapshot)
                ? snapshot.ButtonStartedElapsedTime
                : Mathf.Infinity;
        }

        /// <summary>
        /// Resolves a vector input from the FSM input cache.
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
        /// Marks one recent button start as consumed by this locomotion state.
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
        /// Converts the cached movement vector into the active movement reference space.
        /// </summary>
        /// <returns>The projected movement reference vector.</returns>
        private Vector3 ResolveMovementReferenceInput()
        {
            Vector2 movementInput = MovementInput;
            Vector3 rawMovementReference = MovementReferenceRight * movementInput.x;

            if (CharacterActor.CharacterBody.Is2D)
            {
                return Vector3.ClampMagnitude(rawMovementReference, 1f);
            }

            rawMovementReference += MovementReferenceForward * movementInput.y;
            rawMovementReference = Vector3.ProjectOnPlane(
                rawMovementReference,
                CharacterActor.Up);

            return Vector3.ClampMagnitude(rawMovementReference, 1f);
        }

        #endregion

        #region State Lifecycle

        /// <summary>
        /// Resolves dependencies and registers the high-priority locomotion exits.
        /// </summary>
        protected virtual void OnInit()
        {
            ValidateInputBindings();

            if (MovementStats == null)
            {
                ThrowStateFailure(
                    "NormalMovementState requires a NormalMovementStats asset to be "
                    + "resolved by Brain.Stats. Register one in an FSMStatsRegistry "
                    + "or assign a runtime override before initialization.");
            }

            // Dash is the highest-priority locomotion exit, so it is resolved once.
            _dashState = Brain.States.Get<DashState>();

            if (_dashState == null)
            {
                ThrowStateFailure(
                    "NormalMovementState requires DashState to be loaded in the "
                    + "FSMBrain.");
            }

            // Initialize the shared air-jump pool before gameplay starts.
            _notGroundedJumpsLeft =
                MovementStats.VerticalMovement.availableNotGroundedJumps;
            WriteNotGroundedJumpsLeft(_notGroundedJumpsLeft);

            // Preserve the default body height so crouch interpolation always has a
            // stable standing target.
            _targetHeight = CharacterActor.DefaultBodySize.y;

            // The crouched body cannot become thinner than the current body width.
            _minimumCrouchHeightRatio =
                CharacterActor.BodySize.x
                / Mathf.Max(CharacterActor.BodySize.y, Mathf.Epsilon);

            // Transition priorities mirror the original demo: dash wins first, wall
            // slide remains a lower-priority locomotion exit.
            AddTransition(DashConditions, _dashState, 100);
            AddTransition(
                WallSlideConditions,
                Brain.States.Get<WallSlideState>(),
                10);
            SortTransitions();
        }

        /// <summary>
        /// Restores the locomotion runtime state each time the state becomes active.
        /// </summary>
        protected virtual void OnEnter()
        {
            // Locomotion must observe the real grounding result unless a specialized
            // state explicitly forces a different rule.
            CharacterActor.alwaysNotGrounded = false;

            _targetLookingDirection = CharacterActor.Forward;

            if (Brain.PreviousState is WallSlideState)
            {
                // Wall jump exits are allowed to preserve a full airborne jump budget.
                // The +1 compensates for the immediate airborne jump consumption path.
                WriteNotGroundedJumpsLeft(
                    VerticalMovement.availableNotGroundedJumps + 1);

                // The original demo temporarily reduces air control after a wall jump
                // so the launch keeps its commitment before full steering returns.
                ReduceAirControl(MovementStats.WallJumpReducedControlDuration);
            }

            // Entering locomotion should not suddenly slow an already moving actor.
            _currentPlanarSpeedLimit = Mathf.Max(
                CharacterActor.PlanarVelocity.magnitude,
                PlanarMovement.baseSpeedLimit);

            // Planar and vertical velocity are authored directly by this state.
            CharacterActor.UseRootMotion = false;
            CharacterActor.OnTeleport += OnTeleport;
        }

        /// <summary>
        /// Clears temporary runtime flags when leaving locomotion.
        /// </summary>
        protected virtual void OnExit()
        {
            _reducedAirControlFlag = false;
            CharacterActor.OnTeleport -= OnTeleport;
        }

        /// <summary>
        /// Executes the locomotion fixed-step pipeline.
        /// </summary>
        protected virtual void OnFixedTick()
        {
            float dt = Time.deltaTime;

            // The order matters: size may affect collisions, velocity depends on the
            // current crouch state, and rotation consumes the resolved intent.
            HandleSize(dt);
            HandleVelocity(dt);
            HandleRotation(dt);
        }

        /// <summary>
        /// Synchronizes pre-simulation animator parameters.
        /// </summary>
        /// <param name="dt">The current simulation delta time.</param>
        protected virtual void OnPreCharacterSimulation(float dt)
        {
            // These values describe controller intent and grounding state, so they are
            // safe to write before simulation mutates final velocity.
            if (!CharacterActor.IsAnimatorValid())
            {
                return;
            }

            Animator.SetBool(
                MovementStats.GroundedAnimationParameter,
                CharacterActor.IsGrounded);
            Animator.SetBool(
                MovementStats.StableAnimationParameter,
                CharacterActor.IsStable);
            Animator.SetFloat(
                MovementStats.HorizontalAxisAnimationParameter,
                MovementInput.x);
            Animator.SetFloat(
                MovementStats.VerticalAxisAnimationParameter,
                MovementInput.y);
            Animator.SetFloat(
                MovementStats.HeightAnimationParameter,
                CharacterActor.BodySize.y);
        }

        /// <summary>
        /// Synchronizes post-simulation animator parameters.
        /// </summary>
        /// <param name="dt">The current simulation delta time.</param>
        public virtual void OnPostCharacterSimulation(float dt)
        {
            // Velocity-driven parameters must be written after simulation because CCPro
            // may still adjust the actor velocity during its post-processing steps.
            if (!CharacterActor.IsAnimatorValid())
            {
                return;
            }

            Animator.SetFloat(
                MovementStats.VerticalSpeedAnimationParameter,
                CharacterActor.LocalVelocity.y);
            Animator.SetFloat(
                MovementStats.PlanarSpeedAnimationParameter,
                CharacterActor.PlanarVelocity.magnitude);
        }

        /// <summary>
        /// Resets transient locomotion state after a teleport.
        /// </summary>
        /// <param name="position">The teleported actor position.</param>
        /// <param name="rotation">The teleported actor rotation.</param>
        private void OnTeleport(Vector3 position, Quaternion rotation)
        {
            // Any previous facing target or jump-cancel window is invalid after a
            // teleport because the actor may have been moved arbitrarily.
            _targetLookingDirection = CharacterActor.Forward;
            _isAllowedToCancelJump = false;
        }

        /// <summary>
        /// Reduces airborne acceleration and deceleration for a short period.
        /// </summary>
        /// <param name="reductionDuration">
        /// How long the reduced-control window lasts.
        /// </param>
        private void ReduceAirControl(float reductionDuration = 0.5f)
        {
            _reducedAirControlFlag = true;
            _reducedAirControlInitialTime = Time.time;
            _reductionDuration = reductionDuration;
        }

        #endregion

        #region Planar Movement

        /// <summary>
        /// Computes the acceleration, deceleration, and angle boost used for the
        /// current target velocity.
        /// </summary>
        /// <param name="targetPlanarVelocity">
        /// Desired planar velocity for the current frame.
        /// </param>
        private void SetMotionValues(Vector3 targetPlanarVelocity)
        {
            // Turning sharply is intentionally more responsive than moving straight.
            float angleCurrentTargetVelocity = Vector3.Angle(
                CharacterActor.PlanarVelocity,
                targetPlanarVelocity);

            switch (CharacterActor.CurrentState)
            {
                case CharacterActorState.StableGrounded:
                    _currentMotion.acceleration =
                        PlanarMovement.stableGroundedAcceleration;
                    _currentMotion.deceleration =
                        PlanarMovement.stableGroundedDeceleration;
                    _currentMotion.angleAccelerationMultiplier =
                        PlanarMovement.stableGroundedAngleAccelerationBoost.Evaluate(
                            angleCurrentTargetVelocity);
                    break;

                case CharacterActorState.UnstableGrounded:
                    _currentMotion.acceleration =
                        PlanarMovement.unstableGroundedAcceleration;
                    _currentMotion.deceleration =
                        PlanarMovement.unstableGroundedDeceleration;
                    _currentMotion.angleAccelerationMultiplier =
                        PlanarMovement.unstableGroundedAngleAccelerationBoost.Evaluate(
                            angleCurrentTargetVelocity);
                    break;

                case CharacterActorState.NotGrounded:
                    if (_reducedAirControlFlag)
                    {
                        // Air control ramps back to full strength over time instead of
                        // snapping instantly after a wall jump.
                        float time = Time.time - _reducedAirControlInitialTime;

                        if (time <= _reductionDuration)
                        {
                            _currentMotion.acceleration =
                                PlanarMovement.notGroundedAcceleration
                                / _reductionDuration
                                * time;
                            _currentMotion.deceleration =
                                PlanarMovement.notGroundedDeceleration
                                / _reductionDuration
                                * time;
                        }
                        else
                        {
                            _reducedAirControlFlag = false;
                            _currentMotion.acceleration =
                                PlanarMovement.notGroundedAcceleration;
                            _currentMotion.deceleration =
                                PlanarMovement.notGroundedDeceleration;
                        }
                    }
                    else
                    {
                        _currentMotion.acceleration =
                            PlanarMovement.notGroundedAcceleration;
                        _currentMotion.deceleration =
                            PlanarMovement.notGroundedDeceleration;
                    }

                    _currentMotion.angleAccelerationMultiplier =
                        PlanarMovement.notGroundedAngleAccelerationBoost.Evaluate(
                            angleCurrentTargetVelocity);
                    break;
            }

            // Environment modifiers can override how responsive movement feels.
            if (CharacterActor.IsGrounded)
            {
                _currentMotion.acceleration *=
                    CurrentSurfaceModifiers.AccelerationMultiplier
                    * CurrentVolumeModifiers.AccelerationMultiplier;
                _currentMotion.deceleration *=
                    CurrentSurfaceModifiers.DecelerationMultiplier
                    * CurrentVolumeModifiers.DecelerationMultiplier;
            }
            else
            {
                _currentMotion.acceleration *=
                    CurrentVolumeModifiers.AccelerationMultiplier;
                _currentMotion.deceleration *=
                    CurrentVolumeModifiers.DecelerationMultiplier;
            }
        }

        /// <summary>
        /// Processes the planar movement branch for grounded and airborne locomotion.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        protected virtual void ProcessPlanarMovement(float dt)
        {
            float speedMultiplier = CharacterActor.IsGrounded
                ? CurrentSurfaceModifiers.SpeedMultiplier
                    * CurrentVolumeModifiers.SpeedMultiplier
                : CurrentVolumeModifiers.SpeedMultiplier;

            // Acceleration and deceleration are chosen by comparing how large the
            // desired speed is relative to the current planar velocity.
            bool needToAccelerate =
                (MovementReferenceInput * _currentPlanarSpeedLimit)
                    .sqrMagnitude
                >= CharacterActor.PlanarVelocity.sqrMagnitude;

            Vector3 targetPlanarVelocity = default;

            switch (CharacterActor.CurrentState)
            {
                case CharacterActorState.NotGrounded:
                    if (CharacterActor.WasGrounded)
                    {
                        // The first airborne frame inherits as much horizontal speed as
                        // needed so stepping off ledges does not clamp momentum abruptly.
                        _currentPlanarSpeedLimit = Mathf.Max(
                            CharacterActor.PlanarVelocity.magnitude,
                            PlanarMovement.baseSpeedLimit);
                    }

                    targetPlanarVelocity =
                        MovementReferenceInput
                        * (speedMultiplier * _currentPlanarSpeedLimit);
                    break;

                case CharacterActorState.StableGrounded:
                    // Run state is resolved from the configured input mode every frame.
                    if (PlanarMovement.runInputMode == CCProParameters.InputMode.Toggle)
                    {
                        if (ConsumeButtonStart(_runAction))
                        {
                            _wantToRun = !_wantToRun;
                        }
                    }
                    else
                    {
                        _wantToRun = ReadButtonValue(_runAction);
                    }

                    if (_wantToCrouch || !PlanarMovement.canRun)
                    {
                        _wantToRun = false;
                    }

                    if (_isCrouched)
                    {
                        _currentPlanarSpeedLimit =
                            PlanarMovement.baseSpeedLimit * Crouch.speedMultiplier;
                    }
                    else
                    {
                        _currentPlanarSpeedLimit = _wantToRun
                            ? PlanarMovement.boostSpeedLimit
                            : PlanarMovement.baseSpeedLimit;
                    }

                    targetPlanarVelocity =
                        MovementReferenceInput
                        * (speedMultiplier * _currentPlanarSpeedLimit);
                    break;

                case CharacterActorState.UnstableGrounded:
                    _currentPlanarSpeedLimit = PlanarMovement.baseSpeedLimit;
                    targetPlanarVelocity =
                        MovementReferenceInput
                        * (speedMultiplier * _currentPlanarSpeedLimit);
                    break;
            }

            SetMotionValues(targetPlanarVelocity);

            float acceleration = _currentMotion.acceleration;

            // MoveTowards yields a deterministic acceleration-limited update.
            if (needToAccelerate)
            {
                acceleration *= _currentMotion.angleAccelerationMultiplier;
            }
            else
            {
                acceleration = _currentMotion.deceleration;
            }

            CharacterActor.PlanarVelocity = Vector3.MoveTowards(
                CharacterActor.PlanarVelocity,
                targetPlanarVelocity,
                acceleration * dt);
        }

        #endregion

        #region Vertical Movement

        /// <summary>
        /// Applies gravity according to the vertical state and active environment volume.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        protected virtual void ProcessGravity(float dt)
        {
            if (!VerticalMovement.useGravity)
            {
                return;
            }

            // Some CCPro parameters are dynamic, so the provider refreshes them first.
            VerticalMovement.UpdateParameters();

            float gravityMultiplier = 1f;

            gravityMultiplier = CharacterActor.LocalVelocity.y >= 0
                ? CurrentVolumeModifiers.GravityAscendingMultiplier
                : CurrentVolumeModifiers.GravityDescendingMultiplier;

            float gravity = gravityMultiplier * VerticalMovement.gravity;

            // Stable grounded motion does not receive explicit gravity because the
            // ground solver already maintains ground adhesion.
            if (!CharacterActor.IsStable)
            {
                CharacterActor.VerticalVelocity +=
                    -CharacterActor.Up * (gravity * dt);
            }
        }

        /// <summary>
        /// Evaluates which jump path is currently available.
        /// </summary>
        /// <returns>The jump branch that should execute.</returns>
        protected JumpResult CanJump()
        {
            JumpResult jumpResult = JumpResult.Invalid;

            if (!VerticalMovement.canJump)
            {
                return jumpResult;
            }

            if (_isCrouched)
            {
                return jumpResult;
            }

            // Drop-through jumps are handled by a dedicated branch and should not be
            // interpreted as ordinary grounded jumps.
            if (VerticalMovement.canJumpDown
                && CharacterActor.IsGrounded
                && MovementDown
                && WasButtonStarted(_jumpAction))
            {
                return jumpResult;
            }

            switch (CharacterActor.CurrentState)
            {
                case CharacterActorState.StableGrounded:
                    if (JumpStartedElapsedTime
                        <= VerticalMovement.preGroundedJumpTime
                        && _groundedJumpAvailable)
                    {
                        jumpResult = JumpResult.Grounded;
                    }

                    break;

                case CharacterActorState.NotGrounded:
                    if (WasButtonStarted(_jumpAction))
                    {
                        // Coyote jump wins before checking the extra airborne budget.
                        if (CharacterActor.NotGroundedTime
                            <= VerticalMovement.postGroundedJumpTime
                            && _groundedJumpAvailable)
                        {
                            jumpResult = JumpResult.Grounded;
                        }
                        else if (ReadNotGroundedJumpsLeft() != 0)
                        {
                            jumpResult = JumpResult.NotGrounded;
                        }
                    }

                    break;

                case CharacterActorState.UnstableGrounded:
                    if (JumpStartedElapsedTime
                        <= VerticalMovement.preGroundedJumpTime
                        && VerticalMovement.canJumpOnUnstableGround)
                    {
                        jumpResult = JumpResult.Grounded;
                    }

                    break;
            }

            return jumpResult;
        }

        /// <summary>
        /// Runs the complete jump pipeline for the current frame.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        protected virtual void ProcessJump(float dt)
        {
            // A regular jump and a drop-through jump solve different problems, so the
            // two branches are evaluated independently.
            ProcessRegularJump(dt);
            ProcessJumpDown(dt);
        }

        /// <summary>
        /// Runs the vertical locomotion branch.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        private void ProcessVerticalMovement(float dt)
        {
            ProcessGravity(dt);
            ProcessJump(dt);
        }

        #endregion

        #region Jump Down

        /// <summary>
        /// Attempts to execute a jump-down through a one-way platform.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        /// <returns>True when a jump-down was executed.</returns>
        protected virtual bool ProcessJumpDown(float dt)
        {
            if (!VerticalMovement.canJumpDown)
            {
                return false;
            }

            if (!CharacterActor.IsStable)
            {
                return false;
            }

            if (!CharacterActor.IsGroundAOneWayPlatform)
            {
                return false;
            }

            if (VerticalMovement.filterByTag)
            {
                if (!CharacterActor.GroundObject.CompareTag(
                    VerticalMovement.jumpDownTag))
                {
                    return false;
                }
            }

            if (!ProcessJumpDownAction())
            {
                return false;
            }

            JumpDown(dt);
            return true;
        }

        /// <summary>
        /// Evaluates the input pattern that requests a jump-down.
        /// </summary>
        /// <returns>True when crouch-down plus jump is pressed.</returns>
        protected virtual bool ProcessJumpDownAction()
        {
            return (_isCrouched || MovementDown)
                && ConsumeButtonStart(_jumpAction);
        }

        /// <summary>
        /// Forces the actor below a one-way platform and adds the downward launch.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        protected virtual void JumpDown(float dt)
        {
            float groundDisplacementExtraDistance = 0f;

            // Dynamic ground can move the actor upward while jump-down starts, so the
            // ground displacement is accounted for to guarantee a clean separation.
            Vector3 groundDisplacement = CharacterActor.GroundVelocity * dt;

            if (!CharacterActor.IsGroundAscending)
            {
                groundDisplacementExtraDistance = groundDisplacement.magnitude;
            }

            CharacterActor.ForceNotGrounded();

            CharacterActor.Position -= CharacterActor.Up
                * (JumpDownColliderBottomOffset
                    + VerticalMovement.jumpDownDistance
                    + groundDisplacementExtraDistance);

            CharacterActor.VerticalVelocity -=
                CharacterActor.Up * VerticalMovement.jumpDownVerticalVelocity;
        }

        #endregion

        #region Regular Jump

        /// <summary>
        /// Executes grounded, coyote, or extra airborne jumps.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        protected virtual void ProcessRegularJump(float dt)
        {
            if (CharacterActor.IsGrounded)
            {
                // Touching the ground restores all shared airborne resources.
                ResetSharedAirResources();
                _groundedJumpAvailable = true;
            }

            if (_isAllowedToCancelJump)
            {
                if (VerticalMovement.cancelJumpOnRelease)
                {
                    // The jump-cancel window closes either when the configured maximum
                    // time is exceeded or when the actor already started falling.
                    if (JumpStartedElapsedTime
                        >= VerticalMovement.cancelJumpMaxTime
                        || CharacterActor.IsFalling)
                    {
                        _isAllowedToCancelJump = false;
                    }
                    else if (!ReadButtonValue(_jumpAction)
                        && JumpStartedElapsedTime
                        >= VerticalMovement.cancelJumpMinTime)
                    {
                        // Only the velocity aligned with the jump direction is reduced;
                        // sideways and inherited motion are intentionally preserved.
                        Vector3 projectedJumpVelocity = Vector3.Project(
                            CharacterActor.Velocity,
                            _jumpDirection);

                        CharacterActor.Velocity -= projectedJumpVelocity
                            * (1f - VerticalMovement.cancelJumpMultiplier);
                        _isAllowedToCancelJump = false;
                    }
                }
            }

            JumpResult jumpResult = CanJump();

            switch (jumpResult)
            {
                case JumpResult.Grounded:
                    ConsumeButtonStart(_jumpAction);
                    _groundedJumpAvailable = false;
                    break;

                case JumpResult.NotGrounded:
                    ConsumeButtonStart(_jumpAction);
                    WriteNotGroundedJumpsLeft(
                        ReadNotGroundedJumpsLeft() - 1);
                    break;

                case JumpResult.Invalid:
                    return;
            }

            // Events are emitted before the velocity write so listeners can react
            // while the jump reason and remaining air resources stay explicit.
            if (CharacterActor.IsGrounded)
            {
                OnGroundedJumpPerformed?.Invoke(true);
            }
            else
            {
                OnNotGroundedJumpPerformed?.Invoke(
                    ReadNotGroundedJumpsLeft());
            }

            OnJumpPerformed?.Invoke();

            // The jump direction is resolved once so later calculations refer to
            // the same launch-frame orientation.
            _jumpDirection = SetJumpDirection();

            if (CharacterActor.IsGrounded)
            {
                CharacterActor.ForceNotGrounded();
            }

            // When no reduced-control window is active, the existing velocity on
            // the jump axis is cleared so jump speed stays deterministic.
            if (!_reducedAirControlFlag)
            {
                CharacterActor.Velocity -=
                    Vector3.Project(CharacterActor.Velocity, _jumpDirection);
            }

            CharacterActor.Velocity +=
                _jumpDirection * VerticalMovement.jumpSpeed;

            if (VerticalMovement.cancelJumpOnRelease)
            {
                _isAllowedToCancelJump = true;
            }
        }

        /// <summary>
        /// Resolves the launch direction for the current jump.
        /// </summary>
        /// <returns>The world-space jump direction.</returns>
        protected virtual Vector3 SetJumpDirection()
        {
            return CharacterActor.Up;
        }

        #endregion

        #region Rotation

        /// <summary>
        /// Updates the actor facing for the current frame.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        protected virtual void HandleRotation(float dt)
        {
            HandleLookingDirection(dt);
        }

        /// <summary>
        /// Resolves the desired looking direction and rotates the actor toward it.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        private void HandleLookingDirection(float dt)
        {
            if (!LookingDirection.changeLookingDirection)
            {
                return;
            }

            switch (LookingDirection.lookingDirectionMode)
            {
                case CCProParameters.LookingDirectionParameters.LookingDirectionMode.Movement:
                    switch (CharacterActor.CurrentState)
                    {
                        case CharacterActorState.NotGrounded:
                            SetTargetLookingDirection(
                                LookingDirection.notGroundedLookingDirectionMode);
                            break;

                        case CharacterActorState.StableGrounded:
                            SetTargetLookingDirection(
                                LookingDirection.stableGroundedLookingDirectionMode);
                            break;

                        case CharacterActorState.UnstableGrounded:
                            SetTargetLookingDirection(
                                LookingDirection.unstableGroundedLookingDirectionMode);
                            break;
                    }

                    break;

                case CCProParameters.LookingDirectionParameters.LookingDirectionMode.ExternalReference:
                    if (!CharacterActor.CharacterBody.Is2D)
                    {
                        _targetLookingDirection = MovementReferenceForward;
                    }

                    break;

                case CCProParameters.LookingDirectionParameters.LookingDirectionMode.Target:
                    _targetLookingDirection =
                        LookingDirection.target.position - CharacterActor.Position;
                    _targetLookingDirection.Normalize();
                    break;
            }

            // 3D rotation uses a smooth yaw interpolation, but the 2D branch rotates
            // only around the actor up axis to avoid any roll on the visual.
            Quaternion targetDeltaRotation = Quaternion.FromToRotation(
                CharacterActor.Forward,
                _targetLookingDirection);
            Quaternion currentDeltaRotation = Quaternion.Slerp(
                Quaternion.identity,
                targetDeltaRotation,
                LookingDirection.speed * dt);

            if (CharacterActor.CharacterBody.Is2D)
            {
                Handle2DLookingDirection();
            }
            else
            {
                CharacterActor.SetYaw(currentDeltaRotation * CharacterActor.Forward);
            }
        }

        /// <summary>
        /// Rotates a 2D actor only around its up axis.
        /// </summary>
        private void Handle2DLookingDirection()
        {
            // Both vectors are projected onto the movement plane so the signed angle
            // only describes horizontal facing, never pitch or roll.
            Vector3 currentForward = Vector3.ProjectOnPlane(
                CharacterActor.Forward,
                CharacterActor.Up);
            Vector3 targetForward = Vector3.ProjectOnPlane(
                _targetLookingDirection,
                CharacterActor.Up);

            if (currentForward.sqrMagnitude <= Mathf.Epsilon
                || targetForward.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float yawAngle = Vector3.SignedAngle(
                currentForward,
                targetForward.normalized,
                CharacterActor.Up);

            CharacterActor.RotateYaw(yawAngle);
        }

        /// <summary>
        /// Selects a target looking direction from either input or planar velocity.
        /// </summary>
        /// <param name="lookingDirectionMode">
        /// Source used to resolve the target direction.
        /// </param>
        private void SetTargetLookingDirection(
            CCProParameters.LookingDirectionParameters.LookingDirectionMovementSource
                lookingDirectionMode)
        {
            if (lookingDirectionMode
                == CCProParameters.LookingDirectionParameters.LookingDirectionMovementSource.Input)
            {
                if (MovementReferenceInput != Vector3.zero)
                {
                    _targetLookingDirection = MovementReferenceInput;
                }
                else
                {
                    _targetLookingDirection = CharacterActor.Forward;
                }
            }
            else
            {
                if (CharacterActor.PlanarVelocity != Vector3.zero)
                {
                    _targetLookingDirection = Vector3.ProjectOnPlane(
                        CharacterActor.PlanarVelocity,
                        CharacterActor.Up);
                }
                else
                {
                    _targetLookingDirection = CharacterActor.Forward;
                }
            }
        }

        #endregion

        #region Body Size

        /// <summary>
        /// Resolves crouch intent and interpolates body size accordingly.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        protected virtual void HandleSize(float dt)
        {
            if (Crouch.enableCrouch)
            {
                // The crouch intent follows the configured input mode exactly, which
                // preserves parity with the original CCPro authoring surface.
                if (Crouch.inputMode == CCProParameters.InputMode.Toggle)
                {
                    if (ConsumeButtonStart(_crouchAction))
                    {
                        _wantToCrouch = !_wantToCrouch;
                    }
                }
                else
                {
                    _wantToCrouch = ReadButtonValue(_crouchAction);
                }

                if (!Crouch.notGroundedCrouch && !CharacterActor.IsGrounded)
                {
                    _wantToCrouch = false;
                }

                if (CharacterActor.IsGrounded && _wantToRun)
                {
                    _wantToCrouch = false;
                }
            }
            else
            {
                _wantToCrouch = false;
            }

            if (_wantToCrouch)
            {
                HandleCrouch(dt);
            }
            else
            {
                StandUp(dt);
            }
        }

        /// <summary>
        /// Interpolates the actor toward the crouched height.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        private void HandleCrouch(float dt)
        {
            CharacterActor.SizeReferenceType sizeReferenceType =
                CharacterActor.IsGrounded
                    ? CharacterActor.SizeReferenceType.Bottom
                    : Crouch.notGroundedReference;

            bool validSize = CharacterActor.CheckAndInterpolateHeight(
                CharacterActor.DefaultBodySize.y * CrouchHeightRatio,
                Crouch.sizeLerpSpeed * dt,
                sizeReferenceType);

            if (validSize)
            {
                _isCrouched = true;
            }
        }

        /// <summary>
        /// Interpolates the actor back to its standing height.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        private void StandUp(float dt)
        {
            CharacterActor.SizeReferenceType sizeReferenceType =
                CharacterActor.IsGrounded
                    ? CharacterActor.SizeReferenceType.Bottom
                    : Crouch.notGroundedReference;

            bool validSize = CharacterActor.CheckAndInterpolateHeight(
                CharacterActor.DefaultBodySize.y,
                Crouch.sizeLerpSpeed * dt,
                sizeReferenceType);

            if (validSize)
            {
                _isCrouched = false;
            }
        }

        #endregion

        #region Velocity Coordination

        /// <summary>
        /// Runs the vertical and planar velocity branches.
        /// </summary>
        /// <param name="dt">The current fixed-step delta time.</param>
        protected virtual void HandleVelocity(float dt)
        {
            ProcessVerticalMovement(dt);
            ProcessPlanarMovement(dt);
        }

        #endregion

        #region Shared Resources

        /// <summary>
        /// Restores the shared airborne resources that locomotion owns.
        /// </summary>
        private void ResetSharedAirResources()
        {
            // Normal locomotion is the authority that resets both extra jumps and air
            // dashes when the actor reaches a grounded state again.
            WriteNotGroundedJumpsLeft(VerticalMovement.availableNotGroundedJumps);
            _dashState?.ResetAirDashes();
        }

        /// <summary>
        /// Reads the shared extra-jump count from the blackboard.
        /// </summary>
        /// <returns>The remaining number of not-grounded jumps.</returns>
        private int ReadNotGroundedJumpsLeft()
        {
            if (Brain != null
                && Brain.Blackboard.TryGetValue(
                    CCPro2DBlackboardKeys.NotGroundedJumpsLeft,
                    out int value))
            {
                _notGroundedJumpsLeft = value;
            }

            return _notGroundedJumpsLeft;
        }

        /// <summary>
        /// Writes the shared extra-jump count to the blackboard.
        /// </summary>
        /// <param name="value">The remaining number of not-grounded jumps.</param>
        private void WriteNotGroundedJumpsLeft(int value)
        {
            _notGroundedJumpsLeft = value;

            if (Brain != null)
            {
                Brain.Blackboard.SetValue(
                    CCPro2DBlackboardKeys.NotGroundedJumpsLeft,
                    value);
            }
        }

        #endregion
    }
}