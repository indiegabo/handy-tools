using IndieGabo.HandyTools.HandyBus;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystem
{
    public class PlayerJoinedEvent : IEvent
    {
        public int playerIndex;
        public PlayerInput playerInput;
    }

    public class PlayerLeftEvent : IEvent
    {
        public int playerIndex;
        public PlayerInput playerInput;
    }
}