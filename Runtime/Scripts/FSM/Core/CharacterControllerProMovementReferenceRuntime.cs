using System;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Owns the derived Character Controller Pro movement-reference data used
    /// by the brain when the optional integration is active.
    /// </summary>
    internal sealed class CharacterControllerProMovementReferenceRuntime
    {
        #region Fields

        private Component _characterActor;
        private Transform _externalReference;
        private Vector2 _movementInput;
        private Vector3 _characterInitialForward = Vector3.forward;
        private Vector3 _characterInitialRight = Vector3.right;

        private CharacterControllerProMovementReferenceMode _movementReferenceMode =
            CharacterControllerProMovementReferenceMode.World;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the input vector projected into the current movement reference
        /// space.
        /// </summary>
        public Vector3 InputMovementReference { get; private set; }

        /// <summary>
        /// Gets the current forward axis used by the movement reference.
        /// </summary>
        public Vector3 MovementReferenceForward { get; private set; } =
            Vector3.forward;

        /// <summary>
        /// Gets the current right axis used by the movement reference.
        /// </summary>
        public Vector3 MovementReferenceRight { get; private set; } =
            Vector3.right;

        #endregion

        #region Public API

        /// <summary>
        /// Configures the authored settings that drive the movement reference
        /// projection.
        /// </summary>
        /// <param name="movementReferenceMode">Selected reference mode.</param>
        /// <param name="externalReference">Optional external transform.</param>
        public void Configure(
            CharacterControllerProMovementReferenceMode movementReferenceMode,
            Transform externalReference)
        {
            _movementReferenceMode = movementReferenceMode;
            _externalReference = externalReference;
        }

        /// <summary>
        /// Captures the current actor context required to resolve movement
        /// reference axes.
        /// </summary>
        /// <param name="characterActor">Current CCPro actor component.</param>
        public void Initialize(Component characterActor)
        {
            _characterActor = characterActor;

            if (!CharacterControllerProBridge.TryGetForward(
                    _characterActor,
                    out _characterInitialForward))
            {
                _characterInitialForward = Vector3.forward;
            }

            if (!CharacterControllerProBridge.TryGetRight(
                    _characterActor,
                    out _characterInitialRight))
            {
                _characterInitialRight = Vector3.right;
            }
        }

        /// <summary>
        /// Stores the semantic movement input reported by the bound input
        /// source.
        /// </summary>
        /// <param name="movementInput">Current semantic movement input.</param>
        public void SetMovementInput(Vector2 movementInput)
        {
            _movementInput = movementInput;
        }

        /// <summary>
        /// Clears the currently stored semantic movement input.
        /// </summary>
        public void ClearMovementInput()
        {
            _movementInput = Vector2.zero;
            InputMovementReference = Vector3.zero;
        }

        /// <summary>
        /// Updates the derived movement-reference vectors and projected input
        /// using the last reported semantic movement input.
        /// </summary>
        public void Update()
        {
            if (!CharacterControllerProBridge.TryGetUp(_characterActor, out Vector3 up))
            {
                up = Vector3.up;
            }

            switch (_movementReferenceMode)
            {
                case CharacterControllerProMovementReferenceMode.Character:
                    MovementReferenceForward = _characterInitialForward;
                    MovementReferenceRight = _characterInitialRight;
                    break;

                case CharacterControllerProMovementReferenceMode.External:
                    if (_externalReference != null)
                    {
                        MovementReferenceForward = Vector3.Normalize(
                            Vector3.ProjectOnPlane(
                                _externalReference.forward,
                                up));

                        MovementReferenceRight = Vector3.Normalize(
                            Vector3.ProjectOnPlane(
                                _externalReference.right,
                                up));
                    }
                    else
                    {
                        MovementReferenceForward = Vector3.forward;
                        MovementReferenceRight = Vector3.right;
                    }

                    break;

                default:
                    MovementReferenceForward = Vector3.forward;
                    MovementReferenceRight = Vector3.right;
                    break;
            }

            if (!CharacterControllerProBridge.TryGetIs2D(
                    _characterActor,
                    out bool is2D))
            {
                is2D = false;
            }

            if (is2D)
            {
                InputMovementReference =
                    MovementReferenceRight * _movementInput.x;
                return;
            }

            Vector3 rawMovementReference =
                MovementReferenceRight * _movementInput.x
                + MovementReferenceForward * _movementInput.y;

            InputMovementReference =
                Vector3.ClampMagnitude(rawMovementReference, 1f);
        }

        /// <summary>
        /// Resets the derived reference axes while preserving the last semantic
        /// movement input reported by the input source.
        /// </summary>
        public void ResetDerivedState()
        {
            _characterActor = null;
            InputMovementReference = Vector3.zero;
            MovementReferenceForward = Vector3.forward;
            MovementReferenceRight = Vector3.right;
            _characterInitialForward = Vector3.forward;
            _characterInitialRight = Vector3.right;
        }

        #endregion
    }
}