
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystemModule
{
    [System.Serializable]
    /// <summary>
    /// Stores configuration used when the multiplayer input mode is enabled.
    /// </summary>
    public class MultiplayerModeOptions
    {
        /// <summary>
        /// Automatically joins the first player when the mode starts.
        /// </summary>
        public bool autoJoinPlayerZero = true;

        /// <summary>
        /// Action used to join additional players at runtime.
        /// </summary>
        public InputActionProperty joinAction;
    }
}