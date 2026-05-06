using IndieGabo.HandyTools.GameplayModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.DebuggingModule
{
    /// <summary>
    /// Runtime host for the HandyTools debug panel.
    /// The panel stays hidden until its configured toggle action is performed.
    /// While open, it behaves like a debug-specific pause menu.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DebugPanel : MonoBehaviour
    {
        #region Fields

        private UIDocument _document;
        private VisualElement _mainContainer;
        private VisualElement _panelContainer;
        private VisualElement _sectionsBody;
        private Button _buttonQuit;

        private InputAction _openCloseAction;
        private bool _enabledOpenCloseAction;

        private GameplayService _gameplayService;
        private DebugPanelConfig _config;

        private bool _isOpen;
        private bool _pausedGameplayService;
        private bool _pausedTimeScale;
        private float _cachedTimeScale = 1f;

        private bool _cursorStateCaptured;
        private bool _cachedCursorVisible;
        private CursorLockMode _cachedCursorLockMode;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the debug panel is currently open.
        /// </summary>
        public bool IsOpen => _isOpen;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (DebugPanelBootstrapper.IsAvailableInCurrentBuild) return;

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            UnbindOpenCloseAction();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the panel runtime, binds the toggle action, and hides
        /// the panel until explicitly opened.
        /// </summary>
        /// <param name="config">Resolved panel configuration.</param>
        public void Initialize(DebugPanelConfig config)
        {
            _config = config;

            QueryMainElements();
            CacheServices();
            SetVisibility(false);
            BindOpenCloseAction(config?.OpenCloseInputAction);
        }

        /// <summary>
        /// Adds a new section element to the panel body.
        /// </summary>
        /// <param name="sectionElement">Visual element created by a section.</param>
        public void AddSection(VisualElement sectionElement)
        {
            if (sectionElement == null) return;

            _sectionsBody.Add(sectionElement);
        }

        private void QueryMainElements()
        {
            _document = GetComponent<UIDocument>();
            _document.rootVisualElement.style.height = Length.Percent(100);

            _mainContainer = _document.rootVisualElement.Q<VisualElement>(
                "main-container"
            );
            _panelContainer = _document.rootVisualElement.Q<VisualElement>(
                "panel-container"
            );
            _sectionsBody = _document.rootVisualElement.Q<VisualElement>(
                "sections-body"
            );
            _buttonQuit = _document.rootVisualElement.Q<Button>("button-quit");

            if (_panelContainer != null)
            {
                _panelContainer.focusable = true;
            }

            if (_buttonQuit != null)
            {
                _buttonQuit.clicked += OnQuitButtonClicked;
            }
        }

        private void CacheServices()
        {
            ServiceLocator.TryGet(out _gameplayService);
        }

        #endregion

        #region Visibility

        /// <summary>
        /// Toggles the panel visibility.
        /// </summary>
        public void Toggle()
        {
            if (_isOpen)
            {
                Close();
                return;
            }

            Open();
        }

        /// <summary>
        /// Opens the panel, pauses gameplay if configured, and unlocks the
        /// cursor for debug interactions.
        /// </summary>
        public void Open()
        {
            if (_isOpen) return;

            CaptureCursorState();
            PauseGameplayIfNeeded();
            SetVisibility(true);

            _isOpen = true;
            _panelContainer?.Focus();
        }

        /// <summary>
        /// Closes the panel and restores gameplay and cursor state.
        /// </summary>
        public void Close()
        {
            if (!_isOpen) return;

            SetVisibility(false);
            ResumeGameplayIfNeeded();
            RestoreCursorState();

            _isOpen = false;
        }

        private void SetVisibility(bool isVisible)
        {
            if (_mainContainer == null) return;

            _mainContainer.style.display = isVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        #endregion

        #region Input

        private void BindOpenCloseAction(InputAction action)
        {
            UnbindOpenCloseAction();

            _openCloseAction = action;

            if (_openCloseAction == null)
            {
                Debug.LogWarning(
                    $"[{nameof(DebugPanel)}] No open/close input action was configured."
                );
                return;
            }

            _openCloseAction.performed += OnOpenCloseActionPerformed;

            if (_openCloseAction.enabled) return;

            _openCloseAction.Enable();
            _enabledOpenCloseAction = true;
        }

        private void UnbindOpenCloseAction()
        {
            if (_openCloseAction == null) return;

            _openCloseAction.performed -= OnOpenCloseActionPerformed;

            if (_enabledOpenCloseAction)
            {
                _openCloseAction.Disable();
            }

            _openCloseAction = null;
            _enabledOpenCloseAction = false;
        }

        private void OnOpenCloseActionPerformed(InputAction.CallbackContext context)
        {
            Toggle();
        }

        #endregion

        #region Gameplay State

        private void PauseGameplayIfNeeded()
        {
            if (_config == null || !_config.PauseGameplayWhenOpen) return;

            if (_gameplayService != null && _gameplayService.IsOn)
            {
                _pausedGameplayService = true;
                _ = _gameplayService.PauseGameplay(this);
                return;
            }

            if (Mathf.Approximately(Time.timeScale, 0f)) return;

            _cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _pausedTimeScale = true;
        }

        private void ResumeGameplayIfNeeded()
        {
            if (_config == null || !_config.PauseGameplayWhenOpen) return;

            if (_pausedGameplayService)
            {
                _pausedGameplayService = false;

                if (_gameplayService != null)
                {
                    _ = _gameplayService.ResumeGameplay(this);
                }
            }

            if (!_pausedTimeScale) return;

            Time.timeScale = _cachedTimeScale;
            _pausedTimeScale = false;
        }

        private void CaptureCursorState()
        {
            if (_config == null || !_config.UnlockCursorWhenOpen) return;

            _cachedCursorVisible = UnityEngine.Cursor.visible;
            _cachedCursorLockMode = UnityEngine.Cursor.lockState;
            _cursorStateCaptured = true;

            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }

        private void RestoreCursorState()
        {
            if (!_cursorStateCaptured) return;

            UnityEngine.Cursor.visible = _cachedCursorVisible;
            UnityEngine.Cursor.lockState = _cachedCursorLockMode;
            _cursorStateCaptured = false;
        }

        #endregion

        #region Buttons

        private void OnQuitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}