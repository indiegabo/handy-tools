using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystem
{
    [System.Serializable]
    public class MultiplayerBinding
    {
        public PlayerInput playerInput;
        public IMultiplayerBindable bindable;
    }
}