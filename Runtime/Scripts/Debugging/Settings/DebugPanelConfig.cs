using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

namespace IndieGabo.HandyTools.Debugging
{
    /// <summary>
    /// Global configuration for the runtime debug panel host.
    /// </summary>
    [GlobalConfig("Debugging/DebugPanel")]
    public sealed class DebugPanelConfig : HandyGlobalConfig<DebugPanelConfig>
    {
        #region Fields

        [BoxGroup("Panel")]
        [SerializeField]
        private bool _isEnabled = true;

        [BoxGroup("Panel")]
        [SerializeField]
        private bool _pauseGameplayWhenOpen = true;

        [BoxGroup("Panel")]
        [SerializeField]
        private bool _unlockCursorWhenOpen = true;

        [BoxGroup("Input")]
        [SerializeField]
        private InputAction _openCloseInputAction;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the debug panel may bootstrap at runtime.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetFieldValue(nameof(_isEnabled), value);
        }

        /// <summary>
        /// Gets or sets whether gameplay should be paused while the panel is
        /// open.
        /// </summary>
        public bool PauseGameplayWhenOpen
        {
            get => _pauseGameplayWhenOpen;
            set => SetFieldValue(nameof(_pauseGameplayWhenOpen), value);
        }

        /// <summary>
        /// Gets or sets whether the cursor should be unlocked and made visible
        /// while the panel is open.
        /// </summary>
        public bool UnlockCursorWhenOpen
        {
            get => _unlockCursorWhenOpen;
            set => SetFieldValue(nameof(_unlockCursorWhenOpen), value);
        }

        /// <summary>
        /// Gets or sets the action that toggles the panel open and closed.
        /// </summary>
        public InputAction OpenCloseInputAction
        {
            get => _openCloseInputAction;
            set => SetFieldValue(nameof(_openCloseInputAction), value);
        }

        #endregion
    }
}