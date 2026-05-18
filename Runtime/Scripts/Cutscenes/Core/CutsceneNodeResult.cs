using System;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [Serializable]
    public readonly struct CutsceneNodeResult
    {
        public CutsceneNodeResult(CutsceneNodeStatus status, string outputKey, string failureReason)
        {
            Status = status;
            OutputKey = outputKey;
            FailureReason = failureReason;
        }

        public CutsceneNodeStatus Status { get; }

        public string OutputKey { get; }

        public string FailureReason { get; }

        public static CutsceneNodeResult Success(string outputKey = CutsceneNodePorts.Next)
        {
            return new CutsceneNodeResult(CutsceneNodeStatus.Success, outputKey, string.Empty);
        }

        public static CutsceneNodeResult Failure(string failureReason)
        {
            return new CutsceneNodeResult(CutsceneNodeStatus.Failure, string.Empty, failureReason ?? string.Empty);
        }
    }
}