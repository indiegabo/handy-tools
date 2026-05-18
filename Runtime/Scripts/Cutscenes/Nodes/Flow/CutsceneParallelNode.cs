using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Flow
{
    [System.Serializable]
    [CutsceneNodeMenu("Flow/Fork", "Fork")]
    public sealed class CutsceneParallelNode : CutsceneNodeBase
    {
        private const int MinimumBranchCount = 2;

        [Min(2)]
        [SerializeField]
        private int _branchCount = MinimumBranchCount;

        public int BranchCount => Mathf.Max(MinimumBranchCount, _branchCount);

        public override IReadOnlyList<CutsceneNodePort> GetOutputPorts()
        {
            List<CutsceneNodePort> outputPorts = new(BranchCount);

            for (int index = 0; index < BranchCount; index++)
            {
                outputPorts.Add(new CutsceneNodePort(
                    GetOutputKey(index),
                    $"Branch {index + 1}"));
            }

            return outputPorts;
        }

        public override string GetSummary()
        {
            return $"Forks execution into {BranchCount} parallel branches.";
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            context.TryComplete(CutsceneNodeResult.Success());
        }

        public static string GetOutputKey(int index)
        {
            return $"Branch{index + 1}";
        }
    }
}