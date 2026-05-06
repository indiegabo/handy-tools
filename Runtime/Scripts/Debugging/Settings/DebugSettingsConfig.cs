using UnityEngine;
using Sirenix.OdinInspector;

namespace IndieGabo.HandyTools.DebuggingModule
{
    [GlobalConfig("Debugging/DebugSettings")]
    /// <summary>
    /// Stores runtime debug settings shared by the debug panel sections.
    /// </summary>
    public class DebugSettingsConfig : HandyGlobalConfig<DebugSettingsConfig>
    {
        [BoxGroup("Settings")]
        [SerializeField]
        private bool _vSyncOn;

        [BoxGroup("Settings")]
        [SerializeField]
        private string _fpsLimit = "No Constraints";

        /// <summary>
        /// Gets or sets whether vertical sync is enabled.
        /// </summary>
        public bool VSyncOn
        {
            get => _vSyncOn;
            set => SetFieldValue(nameof(_vSyncOn), value);
        }

        /// <summary>
        /// Gets or sets the selected frame rate cap label.
        /// </summary>
        public string FpsLimit
        {
            get => _fpsLimit;
            set => SetFieldValue(nameof(_fpsLimit), value);
        }
    }
}