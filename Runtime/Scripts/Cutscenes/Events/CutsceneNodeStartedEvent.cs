using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    public readonly struct CutsceneNodeStartedEvent : IEvent
    {
        public CutsceneNodeStartedEvent(CutsceneDirector director, CutsceneRun run, SerializableGuid nodeId)
        {
            Director = director;
            Run = run;
            NodeId = nodeId;
        }

        public CutsceneDirector Director { get; }

        public CutsceneRun Run { get; }

        public SerializableGuid NodeId { get; }
    }
}