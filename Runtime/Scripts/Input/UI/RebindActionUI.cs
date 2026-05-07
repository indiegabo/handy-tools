using System.Collections.Generic;
using IndieGabo.HandyTools.HandyInputSystemModule;
using IndieGabo.HandyTools.HandyInputSystemModule.Feedbacks;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using IndieGabo.HandyTools.HandyInputSystemModule.Bindings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

namespace IndieGabo.HandyTools.HandyInputSystemModule.RuntimeUI
{
    /// <summary>
    /// Binds one rebinding button row to the shared rebinding workflow and
    /// updates the visual prompt shown for the active control scheme.
    /// </summary>
    [RequireComponent(typeof(InputActionButtonRebinder))]
    public class RebindActionUI : HandyBehaviour
    {
        #region Inspector

        [Tooltip("A GameObject to be activated and deactivated based on the rebinding processes status.")]
        [SerializeField]
        private GameObject _overlay;

        [SerializeField]
        private FeedbackContainer _feedbackContainer;

        [Tooltip("The image wich represents the button or key on that device")]
        [SerializeField]
        private Image _buttonImage;

        [SerializeField]
        private Sprite _noButtonSprite;

        [Tooltip("Optional text label that will be updated with prompt for user input.")]
        [SerializeField]
        private TextMeshProUGUI _rebindText;

        [SerializeField]
        private Button _rebindButton;

        [SerializeField]
        private Button _resetButton;

        #endregion        

        #region Fields

        private InputActionButtonRebinder _rebinder;
        private List<StringTable> _stringTables = new();
        private string _waitingText;
        private PlayerInput _playerInput;

        #endregion

        #region Mono

        /// <summary>
        /// Resolves required runtime dependencies and resets the overlay state.
        /// </summary>
        private void Awake()
        {
            PlayerManager playerManager = ServiceLocator.GetRequired<PlayerManager>();
            _playerInput = playerManager.GetRequiredSinglePlayerInput();
            _rebinder = GetComponent<InputActionButtonRebinder>();
            _overlay?.SetActive(false);
        }

        /// <summary>
        /// Subscribes the UI row to rebinding lifecycle callbacks.
        /// </summary>
        private void OnEnable()
        {
            UpdateImage();

            _rebinder.RebindStarted.AddListener(OnRebindProcessStart);
            _rebinder.RebindStopped.AddListener(OnRebindProcessStop);
            _rebinder.BindingUpdated.AddListener(OnUpdateDisplay);

            _rebindButton.onClick.AddListener(OnRebindButtonClicked);
            _resetButton.onClick.AddListener(OnResetButtonClicked);
        }

        /// <summary>
        /// Removes all runtime listeners owned by this row.
        /// </summary>
        private void OnDisable()
        {
            _rebinder.RebindStarted.RemoveListener(OnRebindProcessStart);
            _rebinder.RebindStopped.RemoveListener(OnRebindProcessStop);
            _rebinder.BindingUpdated.RemoveListener(OnUpdateDisplay);

            _rebindButton.onClick.RemoveListener(OnRebindButtonClicked);
            _resetButton.onClick.RemoveListener(OnResetButtonClicked);
        }

        #endregion

        #region Image

        /// <summary>
        /// Refreshes the feedback icon for the current binding and control
        /// scheme.
        /// </summary>
        private void UpdateImage()
        {
            if (_feedbackContainer == null || _buttonImage == null)
            {
                return;
            }

            if (_feedbackContainer.TrySpriteOrFallback(
                _rebinder.ActionReference.action.id,
                _playerInput.currentControlScheme,
                out Sprite sprite
            ))
            {
                _buttonImage.sprite = sprite;
            }
            else
            {
                _buttonImage.sprite = _noButtonSprite;
            }
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Shows the waiting state while a new binding is being captured.
        /// </summary>
        /// <param name="rebinder">Row rebinder that started the operation.</param>
        /// <param name="operation">Interactive rebind operation in progress.</param>
        public void OnRebindProcessStart(InputActionButtonRebinder rebinder, RebindingOperation operation)
        {
            // Bring up rebind overlay, if we have one.
            _overlay?.SetActive(true);

            DisableButtons();

            // If it's a part binding, show the name of the part in the UI.
            var partName = default(string);

            if (_rebinder.ResolveActionAndBinding(out InputAction action, out int bindingIndex))
            {
                InputBinding binding = operation.action.bindings[bindingIndex];
                if (binding.isPartOfComposite)
                    partName = $"Binding '{operation.action.bindings[bindingIndex].name}'. ";
            }

            if (_rebindText != null)
            {
                var text = !string.IsNullOrEmpty(operation.expectedControlType)
                    ? $"{partName}Waiting for {operation.expectedControlType} input..."
                    : $"{partName}Waiting for input...";

                _rebindText.text = text;
            }
        }

        /// <summary>
        /// Restores the idle UI after a binding capture completes or is
        /// cancelled.
        /// </summary>
        /// <param name="rebinder">Row rebinder that ended the operation.</param>
        /// <param name="operation">Interactive rebind operation that ended.</param>
        public void OnRebindProcessStop(InputActionButtonRebinder rebinder, RebindingOperation operation)
        {
            // Bring up rebind overlay, if we have one.
            _overlay?.SetActive(false);
            EnableButtons();
            EventSystem.current.SetSelectedGameObject(_rebindButton.gameObject);
        }

        /// <summary>
        /// Refreshes the displayed icon after a binding value changes.
        /// </summary>
        /// <param name="rebinder">Row rebinder that updated the binding.</param>
        /// <param name="action">Input action whose binding changed.</param>
        /// <param name="bindingIndex">Binding index that was updated.</param>
        public void OnUpdateDisplay(
            InputActionButtonRebinder rebinder,
            InputAction action,
            int bindingIndex
        )
        {
            UpdateImage();
        }

        /// <summary>
        /// Starts the interactive rebind flow for the current row.
        /// </summary>
        private void OnRebindButtonClicked()
        {
            if (!_rebinder.IsInitialized) return;

            _rebinder.StartInteractiveRebind();
        }

        /// <summary>
        /// Restores the current binding to its default value.
        /// </summary>
        private void OnResetButtonClicked()
        {
            if (!_rebinder.IsInitialized) return;
            _rebinder.ResetToDefault();
        }


        #endregion

        #region Buttons

        /// <summary>
        /// Prevents user input while a rebind is active.
        /// </summary>
        private void DisableButtons()
        {
            _rebindButton.interactable = false;
            _resetButton.interactable = false;
        }

        /// <summary>
        /// Restores button interactivity after the rebind finishes.
        /// </summary>
        private void EnableButtons()
        {
            _rebindButton.interactable = true;
            _resetButton.interactable = true;
        }


        #endregion
    }
}





