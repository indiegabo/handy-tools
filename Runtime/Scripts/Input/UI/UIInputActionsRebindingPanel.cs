using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using IndieGabo.HandyTools.Input.Bindings;
using IndieGabo.HandyTools.HandyServiceLocator;
using IndieGabo.HandyTools.HandyInputSystem;

namespace IndieGabo.HandyTools.Input.UI
{
    public class UIInputActionsRebindingPanel : HandyBehaviour
    {
        #region Inspector


        [BoxGroup("Buttons")]
        [SerializeField]
        private Button _resetButton;

        [BoxGroup("Events")]
        [SerializeField]
        private UnityEvent<InputControlScheme> _deviceChanged;

        #endregion

        #region Fields

        private InputControlScheme _currentControlScheme;
        private PlayerInput _playerInput;
        private GameObject _currentActivePanel;

        private List<InputActionButtonRebinder> _rebinders;
        private Dictionary<string, InputControlScheme> _controlSchemes = new();

        #endregion

        #region Getters

        #endregion

        #region Behaviour

        private void Awake()
        {
            ServiceLocator.Global.Get(
                PlayerManager.SinglePlayerServiceName, 
                out _playerInput
            );

            foreach (InputControlScheme scheme in _playerInput.actions.controlSchemes)
            {
                _controlSchemes.Add(scheme.name, scheme);
            }

            _rebinders = GetComponentsInChildren<InputActionButtonRebinder>().ToList();
        }

        private void OnEnable()
        {
            string controlScheme = _playerInput.currentControlScheme;
            if (_controlSchemes.TryGetValue(controlScheme, out InputControlScheme scheme))
            {
                SetControlScheme(scheme);
            }

            _resetButton.onClick.AddListener(OnResetButtonClicked);
        }

        private void OnDisable()
        {
            _resetButton.onClick.RemoveListener(OnResetButtonClicked);

            foreach (InputActionButtonRebinder rebinder in _rebinders)
            {
                rebinder.Dismiss();
            }

            // _bindingsPrefHandler.Save();
        }

        #endregion

        #region Device 

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

        private void OnDeviceChanged(PlayerInput playerInput, InputControlScheme controlScheme)
        {
            SetControlScheme(controlScheme);
        }

        #endregion

        #region Bindings

        private void ResetCurrentDeviceBindings()
        {
            // _bindingsPrefHandler.ResettAllBindingsFromDevice(_currentControlScheme);
        }

        #endregion

        #region Buttons

        private void OnResetButtonClicked()
        {
            ResetCurrentDeviceBindings();
        }

        #endregion
    }
}