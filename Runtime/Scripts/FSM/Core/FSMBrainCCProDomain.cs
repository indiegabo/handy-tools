using System;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Owns optional Character Controller Pro runtime services for one FSM
    /// brain.
    /// </summary>
    public sealed class FSMBrainCCProDomain
    {
        #region Fields

        private readonly FSMBrain _brain;
        private readonly Action<float> _preSimulationHandler;
        private readonly Action<float> _postSimulationHandler;
        private readonly Action<int> _animatorIkHandler;

        private CharacterControllerProMovementReferenceRuntime
            _movementReferenceRuntime;

        private bool _isInitialized;
        private bool _isSubscribed;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes one CCPro domain for the provided brain.
        /// </summary>
        /// <param name="brain">The owning brain.</param>
        /// <param name="preSimulationHandler">Handler for pre-simulation callbacks.</param>
        /// <param name="postSimulationHandler">Handler for post-simulation callbacks.</param>
        /// <param name="animatorIkHandler">Handler for animator IK callbacks.</param>
        public FSMBrainCCProDomain(
            FSMBrain brain,
            Action<float> preSimulationHandler,
            Action<float> postSimulationHandler,
            Action<int> animatorIkHandler)
        {
            _brain = brain;
            _preSimulationHandler = preSimulationHandler;
            _postSimulationHandler = postSimulationHandler;
            _animatorIkHandler = animatorIkHandler;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the optional CCPro integration is enabled and
        /// available for the owning brain.
        /// </summary>
        public bool IsEnabled => _brain.UseCharacterControllerPro;

        /// <summary>
        /// Gets the animator owned by the brain.
        /// </summary>
        public Animator Animator => _brain.ConfiguredAnimator;

        /// <summary>
        /// Gets the configured Character Controller Pro actor component.
        /// </summary>
        public Component Actor => _brain.ConfiguredCharacterActor;

        /// <summary>
        /// Gets the current input movement reference computed for the optional
        /// CCPro integration.
        /// </summary>
        public Vector3 InputMovementReference =>
            IsEnabled && _movementReferenceRuntime != null
                ? _movementReferenceRuntime.InputMovementReference
                : Vector3.zero;

        /// <summary>
        /// Gets or sets the external transform used as movement reference.
        /// </summary>
        public Transform ExternalReference
        {
            get => _brain.ConfiguredExternalReference;
            set => _brain.ConfiguredExternalReference = value;
        }

        /// <summary>
        /// Gets or sets the movement reference mode used by the optional CCPro integration.
        /// </summary>
        public CharacterControllerProMovementReferenceMode MovementReferenceMode
        {
            get => _brain.ConfiguredMovementReferenceMode;
            set => _brain.ConfiguredMovementReferenceMode = value;
        }

        /// <summary>
        /// Gets the forward vector used by the optional CCPro integration.
        /// </summary>
        public Vector3 MovementReferenceForward =>
            IsEnabled && _movementReferenceRuntime != null
                ? _movementReferenceRuntime.MovementReferenceForward
                : Vector3.forward;

        /// <summary>
        /// Gets the right vector used by the optional CCPro integration.
        /// </summary>
        public Vector3 MovementReferenceRight =>
            IsEnabled && _movementReferenceRuntime != null
                ? _movementReferenceRuntime.MovementReferenceRight
                : Vector3.right;

        /// <summary>
        /// Gets or sets whether root motion should be enabled on the actor.
        /// </summary>
        public bool UseRootMotion
        {
            get
            {
                return IsEnabled
                    && CharacterControllerProBridge.TryGetUseRootMotion(
                        Actor,
                        out bool value)
                    && value;
            }
            set
            {
                if (!IsEnabled)
                {
                    return;
                }

                CharacterControllerProBridge.TrySetUseRootMotion(Actor, value);
            }
        }

        /// <summary>
        /// Gets or sets whether root-position updates should be enabled.
        /// </summary>
        public bool UpdateRootPosition
        {
            get
            {
                return IsEnabled
                    && CharacterControllerProBridge.TryGetUpdateRootPosition(
                        Actor,
                        out bool value)
                    && value;
            }
            set
            {
                if (!IsEnabled)
                {
                    return;
                }

                CharacterControllerProBridge.TrySetUpdateRootPosition(Actor, value);
            }
        }

        /// <summary>
        /// Gets or sets whether root-rotation updates should be enabled.
        /// </summary>
        public bool UpdateRootRotation
        {
            get
            {
                return IsEnabled
                    && CharacterControllerProBridge.TryGetUpdateRootRotation(
                        Actor,
                        out bool value)
                    && value;
            }
            set
            {
                if (!IsEnabled)
                {
                    return;
                }

                CharacterControllerProBridge.TrySetUpdateRootRotation(Actor, value);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Initializes the optional Character Controller Pro support when the
        /// package is installed and the integration toggle is enabled.
        /// </summary>
        public void InitializeSupport()
        {
            ResetRuntimeState();

            if (!IsEnabled)
            {
                return;
            }

            CharacterControllerProMovementReferenceRuntime movementReferenceRuntime =
                EnsureMovementReferenceRuntime();

            movementReferenceRuntime?.Configure(
                MovementReferenceMode,
                ExternalReference);
            movementReferenceRuntime?.Initialize(Actor);
            movementReferenceRuntime?.Update();

            _isInitialized = true;
        }

        /// <summary>
        /// Updates the optional Character Controller Pro cached movement data.
        /// </summary>
        public void UpdateSupport()
        {
            if (!IsEnabled)
            {
                return;
            }

            if (!_isInitialized)
            {
                InitializeSupport();
            }

            CharacterControllerProMovementReferenceRuntime movementReferenceRuntime =
                EnsureMovementReferenceRuntime();

            movementReferenceRuntime?.Configure(
                MovementReferenceMode,
                ExternalReference);
            movementReferenceRuntime?.Update();
        }

        /// <summary>
        /// Resets the cached Character Controller Pro runtime data.
        /// </summary>
        public void ResetRuntimeState()
        {
            _isInitialized = false;
            _movementReferenceRuntime?.ResetDerivedState();

            if (!IsEnabled)
            {
                _movementReferenceRuntime = null;
            }
        }

        /// <summary>
        /// Refreshes the authored movement-reference configuration on the
        /// optional runtime helper when it already exists.
        /// </summary>
        public void RefreshMovementReferenceConfiguration()
        {
            _movementReferenceRuntime?.Configure(
                MovementReferenceMode,
                ExternalReference);
        }

        /// <summary>
        /// Subscribes the brain to Character Controller Pro actor callbacks.
        /// </summary>
        public void SubscribeCallbacks()
        {
            if (_isSubscribed || !IsEnabled)
            {
                return;
            }

            if (!CharacterControllerProBridge.IsCharacterActor(Actor))
            {
                return;
            }

            CharacterControllerProBridge.SubscribePreSimulation(
                Actor,
                _preSimulationHandler);

            CharacterControllerProBridge.SubscribePostSimulation(
                Actor,
                _postSimulationHandler);

            if (Animator != null)
            {
                CharacterControllerProBridge.SubscribeAnimatorIk(
                    Actor,
                    _animatorIkHandler);
            }

            _isSubscribed = true;
        }

        /// <summary>
        /// Unsubscribes the brain from Character Controller Pro actor callbacks.
        /// </summary>
        public void UnsubscribeCallbacks()
        {
            if (!_isSubscribed)
            {
                return;
            }

            CharacterControllerProBridge.UnsubscribePreSimulation(
                Actor,
                _preSimulationHandler);

            CharacterControllerProBridge.UnsubscribePostSimulation(
                Actor,
                _postSimulationHandler);

            if (Animator != null)
            {
                CharacterControllerProBridge.UnsubscribeAnimatorIk(
                    Actor,
                    _animatorIkHandler);
            }

            _isSubscribed = false;
        }

        /// <summary>
        /// Resets all IK weights on the configured Character Controller Pro actor.
        /// </summary>
        public void ResetIKWeights()
        {
            if (!IsEnabled)
            {
                return;
            }

            CharacterControllerProBridge.TryResetIKWeights(Actor);
        }

        #endregion

        #region Input Feed

        /// <summary>
        /// Stores the semantic movement input reported by the bound input
        /// source on the optional movement-reference runtime.
        /// </summary>
        /// <param name="value">Current semantic movement input.</param>
        internal void SetReportedMovementInput(Vector2 value)
        {
            EnsureMovementReferenceRuntime()?.SetMovementInput(value);
        }

        /// <summary>
        /// Clears the semantic movement input reported by the bound input
        /// source.
        /// </summary>
        internal void ClearReportedMovementInput()
        {
            _movementReferenceRuntime?.ClearMovementInput();
        }

        #endregion

        #region Internal Helpers

        internal CharacterControllerProMovementReferenceRuntime EnsureMovementReferenceRuntime()
        {
            if (!IsEnabled)
            {
                return null;
            }

            if (_movementReferenceRuntime != null)
            {
                return _movementReferenceRuntime;
            }

            _movementReferenceRuntime =
                new CharacterControllerProMovementReferenceRuntime();

            _movementReferenceRuntime.Configure(
                MovementReferenceMode,
                ExternalReference);

            return _movementReferenceRuntime;
        }

        #endregion
    }
}