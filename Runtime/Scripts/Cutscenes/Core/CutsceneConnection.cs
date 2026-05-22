using System;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [Serializable]
    public sealed class CutsceneConnection : GraphConnection
    {
        public CutsceneConnection(SerializableGuid fromNodeId, string outputKey, SerializableGuid toNodeId)
            : base(fromNodeId, outputKey, toNodeId)
        {
        }
    }
}