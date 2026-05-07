using System;
using Lightbug.CharacterControllerPro.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro.Parameters
{
    /// <summary>
    /// Describes how a toggleable movement-related input should be interpreted.
    /// </summary>
    public enum InputMode
    {
        /// <summary>
        /// Flips the target state whenever the action starts.
        /// </summary>
        Toggle,

        /// <summary>
        /// Keeps the target state active only while the action stays pressed.
        /// </summary>
        Hold
    }

    /// <summary>
    /// Stores the planar acceleration and speed-limit settings used by the FSM
    /// CCPro locomotion states.
    /// </summary>
    [Serializable]
    public class PlanarMovementParameters
    {
        #region Runtime Types

        /// <summary>
        /// Stores the acceleration values selected for the current frame.
        /// </summary>
        /// <remarks>
        /// Initializes one runtime planar movement snapshot.
        /// </remarks>
        /// <param name="acceleration">Selected acceleration value.</param>
        /// <param name="deceleration">Selected deceleration value.</param>
        /// <param name="angleAccelerationBoost">
        /// Selected angular acceleration multiplier.
        /// </param>
        [Serializable]
        public struct PlanarMovementProperties
        {
            /// <summary>
            /// How fast the actor approaches the target velocity.
            /// </summary>
            public float acceleration;

            /// <summary>
            /// How fast the actor slows down when steering away from the target velocity.
            /// </summary>
            public float deceleration;

            /// <summary>
            /// Extra acceleration multiplier applied while turning sharply.
            /// </summary>
            public float angleAccelerationMultiplier;

            /// <summary>
            /// Initializes one runtime planar movement snapshot.
            /// </summary>
            /// <param name="acceleration">Selected acceleration value.</param>
            /// <param name="deceleration">Selected deceleration value.</param>
            /// <param name="angleAccelerationBoost">
            /// Selected angular acceleration multiplier.
            /// </param>
            public PlanarMovementProperties(
                float acceleration,
                float deceleration,
                float angleAccelerationBoost)
            {
                this.acceleration = acceleration;
                this.deceleration = deceleration;
                angleAccelerationMultiplier = angleAccelerationBoost;
            }
        }

        #endregion

        #region Inspector

        [Min(0f)]
        public float baseSpeedLimit = 6f;

        [Header("Run (boost)")]
        public bool canRun = true;

        public InputMode runInputMode = InputMode.Hold;

        [Min(0f)]
        public float boostSpeedLimit = 10f;

        [Header("Stable grounded parameters")]
        public float stableGroundedAcceleration = 50f;

        public float stableGroundedDeceleration = 40f;

        public AnimationCurve stableGroundedAngleAccelerationBoost =
            AnimationCurve.EaseInOut(0f, 1f, 180f, 2f);

        [Header("Unstable grounded parameters")]
        public float unstableGroundedAcceleration = 10f;

        public float unstableGroundedDeceleration = 2f;

        public AnimationCurve unstableGroundedAngleAccelerationBoost =
            AnimationCurve.EaseInOut(0f, 1f, 180f, 1f);

        [Header("Not grounded parameters")]
        public float notGroundedAcceleration = 20f;

        public float notGroundedDeceleration = 5f;

        public AnimationCurve notGroundedAngleAccelerationBoost =
            AnimationCurve.EaseInOut(0f, 1f, 180f, 1f);

        #endregion
    }

    /// <summary>
    /// Stores the gravity, jump, coyote-time, and drop-through settings used by
    /// the FSM CCPro locomotion states.
    /// </summary>
    [Serializable]
    public class VerticalMovementParameters
    {
        #region Types

        /// <summary>
        /// Describes how jumps should react when launched from unstable ground.
        /// </summary>
        public enum UnstableJumpMode
        {
            /// <summary>
            /// Launches vertically using the actor up direction.
            /// </summary>
            Vertical,

            /// <summary>
            /// Launches using the unstable ground normal.
            /// </summary>
            GroundNormal
        }

        #endregion

        #region Inspector

        [Header("Gravity")]
        public bool useGravity = true;

        [Header("Jump")]
        public bool canJump = true;

        public bool autoCalculate = true;

        [Min(0f)]
        public float jumpApexHeight = 2.25f;

        [Min(0f)]
        public float jumpApexDuration = 0.5f;

        public float jumpSpeed = 10f;

        public float gravity = 10f;

        public bool cancelJumpOnRelease = true;

        [Range(0f, 1f)]
        public float cancelJumpMultiplier = 0.5f;

        public float cancelJumpMinTime = 0.1f;

        public float cancelJumpMaxTime = 0.3f;

        [Min(0f)]
        public float preGroundedJumpTime = 0.2f;

        [Min(0f)]
        public float postGroundedJumpTime = 0.1f;

        [Min(0)]
        public int availableNotGroundedJumps = 1;

        public bool canJumpOnUnstableGround;

        public bool unstableGroundedResetsPreJump;

        [Header("Jump Down (One Way Platforms)")]
        public bool canJumpDown = true;

        public bool filterByTag;

        public string jumpDownTag = "JumpDown";

        [Min(0f)]
        public float jumpDownDistance = 0.05f;

        [Min(0f)]
        public float jumpDownVerticalVelocity = 0.5f;

        #endregion

        #region Validation

        /// <summary>
        /// Recalculates derived jump values when automatic calculation is enabled.
        /// </summary>
        public void UpdateParameters()
        {
            if (!autoCalculate)
            {
                return;
            }

            gravity = (2f * jumpApexHeight) / Mathf.Pow(jumpApexDuration, 2f);
            jumpSpeed = gravity * jumpApexDuration;
        }

        /// <summary>
        /// Synchronizes the authored fields after inspector changes.
        /// </summary>
        public void OnValidate()
        {
            if (autoCalculate)
            {
                UpdateParameters();
                return;
            }

            if (gravity <= Mathf.Epsilon)
            {
                gravity = Mathf.Epsilon;
            }

            jumpApexDuration = jumpSpeed / gravity;
            jumpApexHeight = gravity * Mathf.Pow(jumpApexDuration, 2f) / 2f;
        }

        #endregion
    }

    /// <summary>
    /// Stores crouch behavior and body-size interpolation settings for the FSM
    /// CCPro locomotion states.
    /// </summary>
    [Serializable]
    public class CrouchParameters
    {
        #region Inspector

        public bool enableCrouch = true;

        public bool notGroundedCrouch;

        [Min(0f)]
        public float heightRatio = 0.75f;

        [Min(0f)]
        public float speedMultiplier = 0.3f;

        public InputMode inputMode = InputMode.Hold;

        public CharacterActor.SizeReferenceType notGroundedReference =
            CharacterActor.SizeReferenceType.Top;

        [Min(0f)]
        public float sizeLerpSpeed = 8f;

        #endregion
    }

    /// <summary>
    /// Stores looking-direction selection and interpolation settings for the FSM
    /// CCPro locomotion states.
    /// </summary>
    [Serializable]
    public class LookingDirectionParameters
    {
        #region Types

        /// <summary>
        /// Selects the source used to resolve the actor facing target.
        /// </summary>
        public enum LookingDirectionMode
        {
            /// <summary>
            /// Resolves facing from movement input or velocity.
            /// </summary>
            Movement,

            /// <summary>
            /// Resolves facing from a target transform.
            /// </summary>
            Target,

            /// <summary>
            /// Resolves facing from the external movement reference.
            /// </summary>
            ExternalReference
        }

        /// <summary>
        /// Selects whether movement-facing should use velocity or input intent.
        /// </summary>
        public enum LookingDirectionMovementSource
        {
            /// <summary>
            /// Uses the current planar velocity as the facing source.
            /// </summary>
            Velocity,

            /// <summary>
            /// Uses the current projected movement input as the facing source.
            /// </summary>
            Input
        }

        #endregion

        #region Inspector

        public bool changeLookingDirection = true;

        [Header("Lerp properties")]
        public float speed = 10f;

        [Header("Target Direction")]
        public LookingDirectionMode lookingDirectionMode =
            LookingDirectionMode.Movement;

        public Transform target;

        public LookingDirectionMovementSource stableGroundedLookingDirectionMode =
            LookingDirectionMovementSource.Input;

        public LookingDirectionMovementSource unstableGroundedLookingDirectionMode =
            LookingDirectionMovementSource.Velocity;

        public LookingDirectionMovementSource notGroundedLookingDirectionMode =
            LookingDirectionMovementSource.Input;

        #endregion
    }
}