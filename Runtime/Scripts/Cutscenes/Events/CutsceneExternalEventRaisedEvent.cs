using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.HandyBusModule;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    public readonly struct CutsceneExternalEventRaisedEvent : IEvent
    {
        public CutsceneExternalEventRaisedEvent(CutsceneDirector director, string eventName)
        {
            Director = director;
            EventName = eventName;
        }

        public CutsceneDirector Director { get; }

        public string EventName { get; }
    }
}