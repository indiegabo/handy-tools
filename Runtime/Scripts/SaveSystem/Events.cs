
using IndieGabo.HandyTools.HandyBus;

namespace IndieGabo.HandyTools.SaveSystem
{
    public class SlotEvent : IEvent
    {
        public LoadedSlot slot;
        public EventType eventType;

        public enum EventType
        {
            Loading,
            Releasing
        }
    }
}