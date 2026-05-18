using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    public sealed class CutsceneRunTrace
    {
        private readonly List<CutsceneRunTraceStep> _steps = new();

        public IReadOnlyList<CutsceneRunTraceStep> Steps => _steps;

        public CutsceneRunStatus FinalStatus { get; private set; } = CutsceneRunStatus.Idle;

        public string FinalMessage { get; private set; } = string.Empty;

        public void MarkNodeStarted(SerializableGuid nodeId)
        {
            _steps.Add(new CutsceneRunTraceStep
            {
                NodeId = nodeId,
                NodeStatus = CutsceneNodeStatus.Running,
            });
        }

        public void MarkNodeFinished(SerializableGuid nodeId, CutsceneNodeStatus status, string outputKey, string message)
        {
            _steps.Add(new CutsceneRunTraceStep
            {
                NodeId = nodeId,
                NodeStatus = status,
                OutputKey = outputKey,
                Message = message,
            });
        }

        public void MarkOutputTraversed(SerializableGuid nodeId, string outputKey)
        {
            if (string.IsNullOrWhiteSpace(outputKey))
            {
                return;
            }

            _steps.Add(new CutsceneRunTraceStep
            {
                NodeId = nodeId,
                NodeStatus = CutsceneNodeStatus.Success,
                OutputKey = outputKey,
                Message = string.Empty,
            });
        }

        public void MarkEnded(CutsceneRunStatus status, string message)
        {
            FinalStatus = status;
            FinalMessage = message ?? string.Empty;
        }

        public bool HasVisited(SerializableGuid nodeId)
        {
            return _steps.Any(step => step.NodeId == nodeId);
        }
    }
}