
using IndieGabo.HandyTools.HandyBus;

namespace IndieGabo.HandyTools.SaveSystem
{
    /// <summary>
    /// Describes one save-slot lifecycle transition published through the event bus.
    /// </summary>
    public class SlotEvent : IEvent
    {
        /// <summary>
        /// Slot involved in the event.
        /// </summary>
        public LoadedSlot slot;

        /// <summary>
        /// Type of lifecycle transition represented by the event.
        /// </summary>
        public EventType eventType;

        /// <summary>
        /// Enumerates the slot lifecycle transitions emitted by the save system.
        /// </summary>
        public enum EventType
        {
            Loading,
            Releasing
        }
    }
}