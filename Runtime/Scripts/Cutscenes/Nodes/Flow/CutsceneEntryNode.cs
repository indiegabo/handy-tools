using IndieGabo.HandyTools.CutscenesModule.Core;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Flow
{
    [System.Serializable]
    [CutsceneNodeMenu("Flow/Entry", "Entry")]
    public sealed class CutsceneEntryNode : CutsceneNodeBase
    {
        public override void OnEnter(CutsceneExecutionContext context)
        {
            context.TryComplete(CutsceneNodeResult.Success(CutsceneNodePorts.Next));
        }
    }
}