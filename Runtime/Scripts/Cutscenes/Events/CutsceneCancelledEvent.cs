using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.HandyBusModule;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    public readonly struct CutsceneCancelledEvent : IEvent
    {
        public CutsceneCancelledEvent(CutsceneDirector director, CutsceneRun run, string reason)
        {
            Director = director;
            Run = run;
            Reason = reason;
        }

        public CutsceneDirector Director { get; }

        public CutsceneRun Run { get; }

        public string Reason { get; }
    }
}