using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.UI;
using IndieGabo.HandyTools.HandyInputSystemModule.Bindings;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using IndieGabo.HandyTools.HandyInputSystemModule;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

namespace IndieGabo.HandyTools.HandyInputSystemModule.RuntimeUI
{
    /// <summary>
    /// Coordinates a group of rebinding rows for the currently active control
    /// scheme and persists binding overrides through the preferences handler.
    /// </summary>
    public class UIInputActionsRebindingPanel : HandyBehaviour
    {
        #region Inspector

        [SerializeField]
        private Button _resetButton;

        [SerializeField]
        private UnityEvent<InputControlScheme> _deviceChanged;

        [SerializeField]
        private BindingsPrefHandler _bindingsPrefHandler;

        #endregion

        #region Fields

        private InputControlScheme _currentControlScheme;
        private PlayerInput _playerInput;

        private List<InputActionButtonRebinder> _rebinders;
        private Dictionary<string, InputControlScheme> _controlSchemes = new();

        #endregion

        #region Getters

        #endregion

        #region Behaviour

        /// <summary>
        /// Resolves shared services and discovers child rebinding controls.
        /// </summary>
        private void Awake()
        {
            PlayerManager playerManager = ServiceLocator.GetRequired<PlayerManager>();
            _playerInput = playerManager.GetRequiredSinglePlayerInput();

            if (_bindingsPrefHandler == null)
            {
                _bindingsPrefHandler = GetComponentInChildren<BindingsPrefHandler>(true);
            }

            foreach (InputControlScheme scheme in _playerInput.actions.controlSchemes)
            {
                _controlSchemes.Add(scheme.name, scheme);
            }

            _rebinders = new List<InputActionButtonRebinder>(
                GetComponentsInChildren<InputActionButtonRebinder>(true)
            );
        }

        /// <summary>
        /// Loads persisted overrides and initializes the UI to the active
        /// control scheme.
        /// </summary>
        private void OnEnable()
        {
            _bindingsPrefHandler?.Load();

            string controlScheme = _playerInput.currentControlScheme;
            if (_controlSchemes.TryGetValue(controlScheme, out InputControlScheme scheme))
            {
                SetControlScheme(scheme);
            }

            RegisterRebinderCallbacks();
            _resetButton.onClick.AddListener(OnResetButtonClicked);
        }

        /// <summary>
        /// Persists current overrides and releases runtime listeners.
        /// </summary>
        private void OnDisable()
        {
            _resetButton.onClick.RemoveListener(OnResetButtonClicked);
            UnregisterRebinderCallbacks();

            _bindingsPrefHandler?.Save();

            foreach (InputActionButtonRebinder rebinder in _rebinders)
            {
                rebinder.Dismiss();
            }
        }

        #endregion

        #region Device 

        /// <summary>
        /// Applies one control scheme to every discovered rebinding row.
        /// </summary>
        /// <param name="controlScheme">Control scheme to display.</param>
        private void SetControlScheme(InputControlScheme controlScheme)
        {
            if (_currentControlScheme == controlScheme) return;

            _currentControlScheme = controlScheme;

            foreach (InputActionButtonRebinder rebinder in _rebinders)
            {
                rebinder.Dismiss();
                rebinder.Initialize(_currentControlScheme);
            }

            _deviceChanged.Invoke(_currentControlScheme);
        }

        /// <summary>
        /// Reacts to PlayerInput control-scheme changes.
        /// </summary>
        /// <param name="playerInput">PlayerInput that changed schemes.</param>
        /// <param name="controlScheme">New active control scheme.</param>
        private void OnDeviceChanged(PlayerInput playerInput, InputControlScheme controlScheme)
        {
            SetControlScheme(controlScheme);
        }

        #endregion

        #region Bindings

        /// <summary>
        /// Clears overrides for the currently displayed control scheme and
        /// refreshes all bound rows.
        /// </summary>
        private void ResetCurrentDeviceBindings()
        {
            if (_bindingsPrefHandler == null)
            {
                return;
            }

            _bindingsPrefHandler.ResetAllBindingsOfScheme(_currentControlScheme);
            _bindingsPrefHandler.Save();

            foreach (InputActionButtonRebinder rebinder in _rebinders)
            {
                rebinder.UpdateBindingInfo();
            }
        }

        #endregion

        #region Buttons

        /// <summary>
        /// Resets the overrides bound to the current control scheme.
        /// </summary>
        private void OnResetButtonClicked()
        {
            ResetCurrentDeviceBindings();
        }

        /// <summary>
        /// Subscribes to rebinder completion events so overrides are saved as
        /// soon as the user finishes a capture.
        /// </summary>
        private void RegisterRebinderCallbacks()
        {
            foreach (InputActionButtonRebinder rebinder in _rebinders)
            {
                rebinder.RebindStopped.AddListener(OnRebindStopped);
            }
        }

        /// <summary>
        /// Removes rebinder completion subscriptions owned by this panel.
        /// </summary>
        private void UnregisterRebinderCallbacks()
        {
            foreach (InputActionButtonRebinder rebinder in _rebinders)
            {
                rebinder.RebindStopped.RemoveListener(OnRebindStopped);
            }
        }

        /// <summary>
        /// Persists binding overrides after a row finishes rebinding.
        /// </summary>
        /// <param name="rebinder">Rebinder that completed the operation.</param>
        /// <param name="operation">Operation that completed.</param>
        private void OnRebindStopped(
            InputActionButtonRebinder rebinder,
            RebindingOperation operation
        )
        {
            _ = rebinder;
            _ = operation;
            _bindingsPrefHandler?.Save();
        }

        #endregion
    }
}