using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    public readonly struct CutsceneNodeFinishedEvent : IEvent
    {
        public CutsceneNodeFinishedEvent(
            CutsceneDirector director,
            CutsceneRun run,
            SerializableGuid nodeId,
            CutsceneNodeStatus status,
            string outputKey,
            string message)
        {
            Director = director;
            Run = run;
            NodeId = nodeId;
            Status = status;
            OutputKey = outputKey;
            Message = message;
        }

        public CutsceneDirector Director { get; }

        public CutsceneRun Run { get; }

        public SerializableGuid NodeId { get; }

        public CutsceneNodeStatus Status { get; }

        public string OutputKey { get; }

        public string Message { get; }
    }
}