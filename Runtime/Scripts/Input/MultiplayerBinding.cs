using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystemModule
{
    [System.Serializable]
    /// <summary>
    /// Associates one PlayerInput instance with one bindable multiplayer target.
    /// </summary>
    public class MultiplayerBinding
    {
        /// <summary>
        /// Player input driving the binding.
        /// </summary>
        public PlayerInput playerInput;

        /// <summary>
        /// Target object that consumes the multiplayer binding.
        /// </summary>
        public IMultiplayerBindable bindable;
    }
}