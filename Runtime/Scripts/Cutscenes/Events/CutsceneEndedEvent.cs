using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.HandyBusModule;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    public readonly struct CutsceneEndedEvent : IEvent
    {
        public CutsceneEndedEvent(CutsceneDirector director, CutsceneRun run, CutsceneRunStatus status, string message)
        {
            Director = director;
            Run = run;
            Status = status;
            Message = message;
        }

        public CutsceneDirector Director { get; }

        public CutsceneRun Run { get; }

        public CutsceneRunStatus Status { get; }

        public string Message { get; }
    }
}