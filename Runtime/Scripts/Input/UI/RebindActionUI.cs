using System.Collections.Generic;
using IndieGabo.HandyTools.HandyInputSystem;
using IndieGabo.HandyTools.HandyInputSystem.Feedbacks;
using IndieGabo.HandyTools.HandyServiceLocator;
using IndieGabo.HandyTools.Input.Bindings;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

namespace IndieGabo.HandyTools.Input.UI
{
    [RequireComponent(typeof(InputActionButtonRebinder))]
    public class RebindActionUI : HandyBehaviour
    {
        #region Inspector

        [TabGroup("Dependencies")]
        [Tooltip("A GameObject to be activated and deactivated based on the rebinding processes status.")]
        [SerializeField]
        private GameObject _overlay;

        [TabGroup("Dependencies")]
        [SerializeField]
        private FeedbackContainer _feedbackContainer;

        [TabGroup("Feedbacks")]
        [Tooltip("The image wich represents the button or key on that device")]
        [SerializeField]
        private Image _buttonImage;

        [TabGroup("Feedbacks")]
        [SerializeField]
        private Sprite _noButtonSprite;

        [TabGroup("Feedbacks")]
        [Tooltip("Optional text label that will be updated with prompt for user input.")]
        [SerializeField]
        private TextMeshProUGUI _rebindText;

        [TabGroup("Feedbacks")]
        [SerializeField]
        private Button _rebindButton;

        [TabGroup("Feedbacks")]
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

        private void Awake()
        {
            ServiceLocator.Global.Get(
                PlayerManager.SinglePlayerServiceName,
                out _playerInput
            );
            _rebinder = GetComponent<InputActionButtonRebinder>();
            _overlay?.SetActive(false);
        }

        private void OnEnable()
        {
            UpdateImage();

            _rebinder.RebindStarted.AddListener(OnRebindProcessStart);
            _rebinder.RebindStopped.AddListener(OnRebindProcessStop);
            _rebinder.BindingUpdated.AddListener(OnUpdateDisplay);

            _rebindButton.onClick.AddListener(OnRebindButtonClicked);
            _resetButton.onClick.AddListener(OnResetButtonClicked);
        }

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

        private void UpdateImage()
        {
            if (!_feedbackContainer.TrySpriteOrFallback(
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

        public void OnRebindProcessStop(InputActionButtonRebinder rebinder, RebindingOperation operation)
        {
            // Bring up rebind overlay, if we have one.
            _overlay?.SetActive(false);
            EnableButtons();
            EventSystem.current.SetSelectedGameObject(_rebindButton.gameObject);
        }

        public void OnUpdateDisplay(
            InputActionButtonRebinder rebinder, 
            InputAction action, 
            int bindingIndex
        )
        {
            UpdateImage();
        }

        private void OnRebindButtonClicked()
        {
            if (!_rebinder.IsInitialized) return;

            _rebinder.StartInteractiveRebind();
        }

        private void OnResetButtonClicked()
        {
            if (!_rebinder.IsInitialized) return;
            _rebinder.ResetToDefault();
        }


        #endregion

        #region Buttons

        private void DisableButtons()
        {
            _rebindButton.interactable = false;
            _resetButton.interactable = false;
        }

        private void EnableButtons()
        {
            _rebindButton.interactable = true;
            _resetButton.interactable = true;
        }


        #endregion
    }
}





