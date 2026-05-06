using System;
using IndieGabo.HandyTools.HandyBusModule;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystemModule
{
    /// <summary>
    /// Published when one player joins the multiplayer input flow.
    /// </summary>
    public class PlayerJoinedEvent : IEvent
    {
        /// <summary>
        /// Index assigned to the joined player.
        /// </summary>
        public int playerIndex;

        /// <summary>
        /// Persistent GUID assigned to the joined player registration.
        /// </summary>
        public Guid persistentGuid;

        /// <summary>
        /// PlayerInput instance created for the joined player.
        /// </summary>
        public PlayerInput playerInput;
    }

    /// <summary>
    /// Published when one player leaves the multiplayer input flow.
    /// </summary>
    public class PlayerLeftEvent : IEvent
    {
        /// <summary>
        /// Index assigned to the leaving player.
        /// </summary>
        public int playerIndex;

        /// <summary>
        /// Persistent GUID assigned to the leaving player registration.
        /// </summary>
        public Guid persistentGuid;

        /// <summary>
        /// PlayerInput instance associated with the leaving player.
        /// </summary>
        public PlayerInput playerInput;
    }
}