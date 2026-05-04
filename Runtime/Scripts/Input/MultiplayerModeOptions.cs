
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystem
{
    [System.Serializable]
    public class MultiplayerModeOptions
    {
        public bool autoJoinPlayerZero = true;
        public InputActionProperty joinAction;
    }
}