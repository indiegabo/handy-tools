using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.HandyInputSystemModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Reads authored InputActionReference entries from one resolved
    /// PlayerInput instance and republishes their values into the bound FSM
    /// brain.
    /// </summary>
    [AddComponentMenu("HandyTools/FSM/Input/FSM Player Input Source")]
    public sealed class FSMPlayerInputSource : FSMInputSource
    {
        /// <summary>
        /// Defines how this source should resolve the PlayerInput that feeds
        /// the brain.
        /// </summary>
        public enum PlayerInputResolutionStrategy
        {
            /// <summary>
            /// Resolves the PlayerManager from the global service locator and
            /// requests its single-player PlayerInput.
            /// </summary>
            SinglePlayerService = 0,

            /// <summary>
            /// Resolves the PlayerInput assigned directly on this component in
            /// the inspector.
            /// </summary>
            InspectorReference = 1,

            /// <summary>
            /// Requires custom project-owned runtime composition. Another
            /// runtime system must inject the PlayerInput through
            /// SetPlayerInput or SetProvidedPlayerInput.
            /// </summary>
            RuntimeProvider = 2
        }

        #region Inspector

        [SerializeField]
        private PlayerInputResolutionStrategy _playerInputResolutionStrategy =
            PlayerInputResolutionStrategy.SinglePlayerService;

        [SerializeField]
        private PlayerInput _playerInput;

        /// <summary>
        /// Optional semantic movement action forwarded to the bound brain when
        /// Character Controller Pro movement reference support is enabled.
        /// </summary>
        [SerializeField]
        private InputActionReference _movementInputAction;

        /// <summary>
        /// Additional action references reported into the generic brain input
        /// cache besides the dedicated movement action.
        /// </summary>
        [SerializeField]
        private List<InputActionReference> _inputActions = new();

        #endregion

        #region Fields

        private readonly List<ResolvedBinding> _resolvedBindings = new();
        private readonly HashSet<string> _missingActions =
            new(StringComparer.Ordinal);

        private PlayerInput _providedPlayerInput;
        private PlayerInput _resolvedPlayerInput;
        private PlayerInput _cachedResolvedPlayerInput;
        private InputActionAsset _cachedActionAsset;
        private int _cachedActionCount = -1;
        private Guid _cachedMovementActionId = Guid.Empty;
        private bool _canWarnAboutMissingPlayerInput;
        private bool _missingPlayerInputWarningIssued;

        private InputAction _resolvedMovementAction;
        private string _resolvedMovementDisplayName = string.Empty;

        #endregion

        #region Unity Messages

        /// <summary>
        /// Resolves the nearest PlayerInput before gameplay starts.
        /// </summary>
        private void Awake()
        {
            ResolvePlayerInput(false);
        }

        /// <summary>
        /// Enables missing PlayerInput warnings only after startup ordering had
        /// a chance to inject runtime dependencies.
        /// </summary>
        private void Start()
        {
            _canWarnAboutMissingPlayerInput = true;
        }

        /// <summary>
        /// Migrates legacy binding data and invalidates the runtime cache when
        /// the component changes in the editor.
        /// </summary>
        private void OnValidate()
        {
            ResetBindingCache();

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            ResolvePlayerInput(false);
            RebuildBindingCache();
        }

        /// <summary>
        /// Clears cached inputs when this source is disabled.
        /// </summary>
        private void OnDisable()
        {
            if (!IsBound)
            {
                ResetBindingCache();
                return;
            }

            ClearInputs();
            ResetBindingCache();
        }

        /// <summary>
        /// Publishes the current bindings into the bound brain every frame.
        /// </summary>
        private void Update()
        {
            if (!IsBound || !EnsureBindingCache())
            {
                return;
            }

            if (_resolvedMovementAction != null)
            {
                Vector2 movementValue = _resolvedMovementAction.ReadValue<Vector2>();
                ReportMovementInput(movementValue);
                ReportVector2(
                    _cachedMovementActionId,
                    _resolvedMovementDisplayName,
                    movementValue);
            }
            else
            {
                ClearMovementInput();
            }

            for (int index = 0; index < _resolvedBindings.Count; index++)
            {
                ResolvedBinding binding = _resolvedBindings[index];

                switch (binding.ValueKind)
                {
                    case FSMInputValueKind.Button:
                        ReportButton(
                            binding.ActionId,
                            binding.DisplayName,
                            binding.Action.IsPressed());
                        break;

                    case FSMInputValueKind.Float:
                        ReportFloat(
                            binding.ActionId,
                            binding.DisplayName,
                            binding.Action.ReadValue<float>());
                        break;

                    case FSMInputValueKind.Vector2:
                        ReportVector2(
                            binding.ActionId,
                            binding.DisplayName,
                            binding.Action.ReadValue<Vector2>());
                        break;
                }
            }
        }

        #endregion

        #region FSMInputSource

        /// <summary>
        /// Refreshes the binding cache when the source is attached to a brain.
        /// </summary>
        /// <param name="brain">Brain that will receive the values.</param>
        protected override void OnBound(FSMBrain brain)
        {
            ResolvePlayerInput(false);
            RebuildBindingCache();
        }

        /// <summary>
        /// Clears cached runtime values when the source is detached.
        /// </summary>
        /// <param name="brain">Brain that is about to stop receiving values.</param>
        protected override void OnUnbinding(FSMBrain brain)
        {
            ClearInputs();
            ResetBindingCache();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Provides a PlayerInput instance from another runtime system.
        /// </summary>
        /// <param name="playerInput">Player input that should drive this source.</param>
        public void SetPlayerInput(PlayerInput playerInput)
        {
            if (ReferenceEquals(_providedPlayerInput, playerInput))
            {
                return;
            }

            _providedPlayerInput = playerInput;
            ResolvePlayerInput(false);
            RebuildBindingCache();
        }

        /// <summary>
        /// Provides a semantic alias for runtime provider flows.
        /// </summary>
        /// <param name="playerInput">Player input that should drive this source.</param>
        public void SetProvidedPlayerInput(PlayerInput playerInput)
        {
            SetPlayerInput(playerInput);
        }

        /// <summary>
        /// Clears any runtime-provided PlayerInput instance.
        /// </summary>
        public void ClearPlayerInputOverride()
        {
            _providedPlayerInput = null;
            ResolvePlayerInput(false);
            RebuildBindingCache();
        }

        /// <summary>
        /// Clears the runtime-provided PlayerInput instance.
        /// </summary>
        public void ClearProvidedPlayerInput()
        {
            ClearPlayerInputOverride();
        }

        /// <summary>
        /// Gets the current strategy used to resolve the active PlayerInput.
        /// </summary>
        public PlayerInputResolutionStrategy ResolutionStrategy =>
            _playerInputResolutionStrategy;

        /// <summary>
        /// Gets the PlayerInput currently resolved by the configured strategy.
        /// </summary>
        public PlayerInput ResolvedPlayerInput => _resolvedPlayerInput;

        /// <summary>
        /// Gets the optional semantic movement action authored on this source.
        /// </summary>
        public InputActionReference MovementInputAction => _movementInputAction;

        #endregion

        #region Resolution

        /// <summary>
        /// Ensures the source still points to the current PlayerInput action asset.
        /// </summary>
        /// <returns>True when the binding cache is ready for reads.</returns>
        private bool EnsureBindingCache()
        {
            ResolvePlayerInput();

            if (_resolvedPlayerInput == null || _resolvedPlayerInput.actions == null)
            {
                if (_cachedResolvedPlayerInput != null || _resolvedBindings.Count != 0)
                {
                    RebuildBindingCache();
                }

                return false;
            }

            if (!ReferenceEquals(_cachedResolvedPlayerInput, _resolvedPlayerInput)
                || !ReferenceEquals(_cachedActionAsset, _resolvedPlayerInput.actions)
                || _cachedActionCount != _inputActions.Count
                || _cachedMovementActionId != ResolveActionId(_movementInputAction))
            {
                RebuildBindingCache();
            }

            return _resolvedBindings.Count != 0
                || _resolvedMovementAction != null
                || _inputActions.Count == 0;
        }

        /// <summary>
        /// Resolves the PlayerInput instance used by this source according to
        /// the configured strategy.
        /// </summary>
        /// <param name="allowWarning">Whether missing-input warnings may be logged.</param>
        private void ResolvePlayerInput(bool allowWarning = true)
        {
            if (TryResolveConfiguredPlayerInput(out PlayerInput resolvedPlayerInput))
            {
                _resolvedPlayerInput = resolvedPlayerInput;
                _missingPlayerInputWarningIssued = false;
                return;
            }

            _resolvedPlayerInput = null;

            if (!allowWarning
                || !_canWarnAboutMissingPlayerInput
                || _missingPlayerInputWarningIssued)
            {
                return;
            }

            _missingPlayerInputWarningIssued = true;
            Debug.LogWarning(BuildMissingPlayerInputWarning(), this);
        }

        /// <summary>
        /// Rebuilds the cached action bindings against the current PlayerInput
        /// action asset.
        /// </summary>
        private void RebuildBindingCache()
        {
            _resolvedBindings.Clear();
            _resolvedMovementAction = null;
            _resolvedMovementDisplayName = string.Empty;
            ClearInputs();
            _missingActions.Clear();

            if (_resolvedPlayerInput == null || _resolvedPlayerInput.actions == null)
            {
                _cachedResolvedPlayerInput = null;
                _cachedActionAsset = null;
                _cachedActionCount = -1;
                _cachedMovementActionId = Guid.Empty;
                return;
            }

            _cachedResolvedPlayerInput = _resolvedPlayerInput;
            _cachedActionAsset = _resolvedPlayerInput.actions;
            _cachedActionCount = _inputActions.Count;
            _cachedMovementActionId = ResolveActionId(_movementInputAction);

            ResolveMovementInputAction();

            for (int index = 0; index < _inputActions.Count; index++)
            {
                InputActionReference actionReference = _inputActions[index];

                if (actionReference == null || actionReference.action == null)
                {
                    PrintMissingActionReferenceWarning(index);
                    continue;
                }

                Guid actionId = actionReference.action.id;

                if (actionId == Guid.Empty)
                {
                    PrintMissingActionReferenceWarning(index);
                    continue;
                }

                if (_resolvedMovementAction != null
                    && actionId == _cachedMovementActionId)
                {
                    continue;
                }

                InputAction action = _resolvedPlayerInput.actions.FindAction(
                    actionId.ToString(),
                    false);

                if (action == null)
                {
                    PrintMissingResolvedActionWarning(ResolveDisplayName(actionReference));
                    continue;
                }

                _resolvedBindings.Add(new ResolvedBinding(
                    actionId,
                    ResolveDisplayName(actionReference),
                    ResolveValueKind(action),
                    action));
            }
        }

        /// <summary>
        /// Resolves the semantic movement input action against the current
        /// PlayerInput action asset.
        /// </summary>
        private void ResolveMovementInputAction()
        {
            if (_movementInputAction == null)
            {
                return;
            }

            if (_movementInputAction.action == null)
            {
                PrintMissingMovementInputActionWarning();
                return;
            }

            Guid movementActionId = ResolveActionId(_movementInputAction);

            if (movementActionId == Guid.Empty)
            {
                PrintMissingMovementInputActionWarning();
                return;
            }

            InputAction resolvedAction = _resolvedPlayerInput.actions.FindAction(
                movementActionId.ToString(),
                false);

            if (resolvedAction == null)
            {
                PrintMissingResolvedActionWarning(ResolveDisplayName(_movementInputAction));
                return;
            }

            if (ResolveValueKind(resolvedAction) != FSMInputValueKind.Vector2)
            {
                PrintInvalidMovementInputActionWarning(
                    ResolveDisplayName(_movementInputAction));
                return;
            }

            _resolvedMovementAction = resolvedAction;
            _resolvedMovementDisplayName = ResolveDisplayName(_movementInputAction);
        }

        /// <summary>
        /// Tries to resolve a PlayerInput using the configured strategy.
        /// </summary>
        /// <param name="playerInput">Resolved player input when found.</param>
        /// <returns>True when a valid PlayerInput was resolved.</returns>
        private bool TryResolveConfiguredPlayerInput(out PlayerInput playerInput)
        {
            return _playerInputResolutionStrategy switch
            {
                PlayerInputResolutionStrategy.SinglePlayerService =>
                    TryResolvePlayerInputFromServices(out playerInput),
                PlayerInputResolutionStrategy.InspectorReference =>
                    TryResolvePlayerInputFromInspector(out playerInput),
                PlayerInputResolutionStrategy.RuntimeProvider =>
                    TryResolvePlayerInputFromProvider(out playerInput),
                _ => TryResolvePlayerInputFromServices(out playerInput)
            };
        }

        /// <summary>
        /// Tries to resolve the inspector-authored PlayerInput reference.
        /// </summary>
        /// <param name="playerInput">Resolved player input when found.</param>
        /// <returns>True when the inspector field contains a valid PlayerInput.</returns>
        private bool TryResolvePlayerInputFromInspector(out PlayerInput playerInput)
        {
            playerInput = _playerInput;
            return playerInput != null && playerInput.actions != null;
        }

        /// <summary>
        /// Tries to resolve the single-player PlayerInput exposed by the
        /// HandyTools PlayerManager.
        /// </summary>
        /// <param name="playerInput">Resolved player input when found.</param>
        /// <returns>True when the service locator exposes the PlayerManager.</returns>
        private static bool TryResolvePlayerInputFromServices(out PlayerInput playerInput)
        {
            playerInput = null;

            if (!ServiceLocator.TryGet(out PlayerManager playerManager)
                || playerManager == null)
            {
                return false;
            }

            playerInput = playerManager.GetRequiredSinglePlayerInput();
            return playerInput.actions != null;
        }

        /// <summary>
        /// Tries to resolve a PlayerInput provided by another runtime entity.
        /// </summary>
        /// <param name="playerInput">Resolved player input when found.</param>
        /// <returns>True when a runtime provider already injected a valid input.</returns>
        private bool TryResolvePlayerInputFromProvider(out PlayerInput playerInput)
        {
            playerInput = _providedPlayerInput;
            return playerInput != null && playerInput.actions != null;
        }

        /// <summary>
        /// Resolves the display name that should represent one action
        /// reference.
        /// </summary>
        /// <param name="actionReference">Action reference to inspect.</param>
        /// <returns>The display name used by diagnostics.</returns>
        private static string ResolveDisplayName(InputActionReference actionReference)
        {
            return actionReference != null && actionReference.action != null
                ? actionReference.action.name
                : "Unnamed Input";
        }

        /// <summary>
        /// Resolves the action id exposed by an authored action reference.
        /// </summary>
        /// <param name="actionReference">Action reference to inspect.</param>
        /// <returns>The authored action id, or Guid.Empty when unavailable.</returns>
        private static Guid ResolveActionId(InputActionReference actionReference)
        {
            return actionReference != null && actionReference.action != null
                ? actionReference.action.id
                : Guid.Empty;
        }

        /// <summary>
        /// Resolves how one action should be read based on its authored shape.
        /// </summary>
        /// <param name="action">Action to inspect.</param>
        /// <returns>The inferred runtime value kind.</returns>
        private static FSMInputValueKind ResolveValueKind(InputAction action)
        {
            if (action == null)
            {
                return FSMInputValueKind.Button;
            }

            if (action.type == InputActionType.Button
                || string.Equals(
                    action.expectedControlType,
                    "Button",
                    StringComparison.OrdinalIgnoreCase))
            {
                return FSMInputValueKind.Button;
            }

            if (string.Equals(
                    action.expectedControlType,
                    nameof(Vector2),
                    StringComparison.OrdinalIgnoreCase)
                || Uses2DComposite(action)
                || action.activeControl?.valueType == typeof(Vector2))
            {
                return FSMInputValueKind.Vector2;
            }

            return FSMInputValueKind.Float;
        }

        /// <summary>
        /// Gets whether one action uses a 2D composite binding.
        /// </summary>
        /// <param name="action">Action to inspect.</param>
        /// <returns>True when the action exposes a 2DVector composite.</returns>
        private static bool Uses2DComposite(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            for (int index = 0; index < action.bindings.Count; index++)
            {
                InputBinding binding = action.bindings[index];

                if (!binding.isComposite)
                {
                    continue;
                }

                if (string.Equals(
                    binding.path,
                        "2DVector",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Logs an invalid action reference warning only once per list index.
        /// </summary>
        /// <param name="index">List index that could not be resolved.</param>
        private void PrintMissingActionReferenceWarning(int index)
        {
            string warningKey = $"action:{index}";

            if (!_missingActions.Add(warningKey))
            {
                return;
            }

            Debug.LogWarning(
                $"FSMPlayerInputSource action at index {index} is not assigned correctly.",
                this);
        }

        /// <summary>
        /// Logs an unresolved action warning only once per action name.
        /// </summary>
        /// <param name="displayName">Display name of the missing action.</param>
        private void PrintMissingResolvedActionWarning(string displayName)
        {
            string warningKey = $"resolved:{displayName}";

            if (!_missingActions.Add(warningKey))
            {
                return;
            }

            Debug.LogWarning(
                $"FSMPlayerInputSource could not resolve '{displayName}' from the active PlayerInput.",
                this);
        }

        /// <summary>
        /// Logs an invalid movement-input reference warning only once.
        /// </summary>
        private void PrintMissingMovementInputActionWarning()
        {
            const string warningKey = "movement:missing";

            if (!_missingActions.Add(warningKey))
            {
                return;
            }

            Debug.LogWarning(
                "FSMPlayerInputSource movement input action is assigned incorrectly. Movement reference support expects a valid Vector2 InputActionReference.",
                this);
        }

        /// <summary>
        /// Logs an invalid movement-input shape warning only once per action.
        /// </summary>
        /// <param name="displayName">Display name of the invalid action.</param>
        private void PrintInvalidMovementInputActionWarning(string displayName)
        {
            string warningKey = $"movement:invalid:{displayName}";

            if (!_missingActions.Add(warningKey))
            {
                return;
            }

            Debug.LogWarning(
                $"FSMPlayerInputSource movement input action '{displayName}' must resolve to a Vector2 action.",
                this);
        }

        /// <summary>
        /// Resets the local resolution and binding cache.
        /// </summary>
        private void ResetBindingCache()
        {
            _resolvedPlayerInput = null;
            _cachedResolvedPlayerInput = null;
            _cachedActionAsset = null;
            _cachedActionCount = -1;
            _cachedMovementActionId = Guid.Empty;
            _resolvedMovementAction = null;
            _resolvedMovementDisplayName = string.Empty;
            _resolvedBindings.Clear();
            _missingActions.Clear();
            _missingPlayerInputWarningIssued = false;
        }

        /// <summary>
        /// Builds the warning emitted when the configured strategy fails to
        /// resolve a PlayerInput.
        /// </summary>
        /// <returns>The warning message shown in the console.</returns>
        private string BuildMissingPlayerInputWarning()
        {
            return _playerInputResolutionStrategy switch
            {
                PlayerInputResolutionStrategy.SinglePlayerService =>
                    "FSMPlayerInputSource is configured to adopt the PlayerManager single-player input, but no PlayerManager could be resolved from the ServiceLocator yet.",
                PlayerInputResolutionStrategy.InspectorReference =>
                    "FSMPlayerInputSource is configured to use an inspector PlayerInput reference, but no valid PlayerInput is assigned.",
                PlayerInputResolutionStrategy.RuntimeProvider =>
                    "FSMPlayerInputSource is configured to wait for a runtime provider, but no valid PlayerInput was provided yet. This strategy requires custom project implementation. Another runtime system should call SetPlayerInput. See Assets/HandyTools/Docs/FSMModule/03-FSMBrain-and-Machine-Flow.md, section 'Runtime PlayerInput Injection'.",
                _ => "FSMPlayerInputSource could not resolve a valid PlayerInput."
            };
        }

        #endregion

        #region Types

        /// <summary>
        /// Stores one resolved runtime action binding.
        /// </summary>
        private readonly struct ResolvedBinding
        {
            /// <summary>
            /// Initializes one resolved runtime action binding.
            /// </summary>
            /// <param name="actionId">Resolved action identifier.</param>
            /// <param name="displayName">Display name shown by diagnostics.</param>
            /// <param name="valueKind">Stored value kind.</param>
            /// <param name="action">Resolved runtime action.</param>
            public ResolvedBinding(
                Guid actionId,
                string displayName,
                FSMInputValueKind valueKind,
                InputAction action)
            {
                ActionId = actionId;
                DisplayName = displayName;
                ValueKind = valueKind;
                Action = action;
            }

            /// <summary>
            /// Gets the resolved action identifier.
            /// </summary>
            public Guid ActionId { get; }

            /// <summary>
            /// Gets the display name shown by diagnostics.
            /// </summary>
            public string DisplayName { get; }

            /// <summary>
            /// Gets how the action value should be read.
            /// </summary>
            public FSMInputValueKind ValueKind { get; }

            /// <summary>
            /// Gets the resolved runtime input action.
            /// </summary>
            public InputAction Action { get; }
        }

        #endregion
    }
}