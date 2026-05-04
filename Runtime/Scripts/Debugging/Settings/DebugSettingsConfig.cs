using UnityEngine;

namespace IndieGabo.HandyTools.Debugging
{
    [GlobalConfig("Debugging/DebugSettings")]
    public class DebugSettingsConfig : HandyGlobalConfig<DebugSettingsConfig>
    {
        [SerializeField]
        private bool _vSyncOn;

        [SerializeField]
        private string _fpsLimit = "No Constraints";

        public bool VSyncOn
        {
            get => _vSyncOn;
            set => SetFieldValue(nameof(_vSyncOn), value);
        }

        public string FpsLimit
        {
            get => _fpsLimit;
            set => SetFieldValue(nameof(_fpsLimit), value);
        }
    }
}