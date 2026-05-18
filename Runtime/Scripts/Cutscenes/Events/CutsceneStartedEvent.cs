using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.HandyBusModule;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    public readonly struct CutsceneStartedEvent : IEvent
    {
        public CutsceneStartedEvent(CutsceneDirector director, CutsceneRun run)
        {
            Director = director;
            Run = run;
        }

        public CutsceneDirector Director { get; }

        public CutsceneRun Run { get; }
    }
}