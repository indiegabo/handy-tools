using System;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [Serializable]
    public sealed class CutsceneRunTraceStep
    {
        public SerializableGuid NodeId;
        public CutsceneNodeStatus NodeStatus;
        public string OutputKey;
        public string Message;
    }
}