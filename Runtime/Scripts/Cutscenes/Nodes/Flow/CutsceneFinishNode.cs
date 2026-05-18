using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Core;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Flow
{
    [System.Serializable]
    [CutsceneNodeMenu("Flow/Finish", "Finish")]
    public sealed class CutsceneFinishNode : CutsceneNodeBase
    {
        public override IReadOnlyList<CutsceneNodePort> GetOutputPorts()
        {
            return CutsceneNodePort.None;
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            context.TryComplete(CutsceneNodeResult.Success(CutsceneNodePorts.Complete));
        }
    }
}