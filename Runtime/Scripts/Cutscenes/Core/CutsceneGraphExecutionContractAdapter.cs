using IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    /// <summary>
    /// Converts legacy Cutscenes runtime execution contracts into GraphCore execution contracts and back.
    /// </summary>
    public static class CutsceneGraphExecutionContractAdapter
    {
        /// <summary>
        /// Converts one legacy cutscene node result into one GraphCore execution result.
        /// </summary>
        /// <param name="result">Legacy cutscene node result.</param>
        /// <returns>The converted GraphCore execution result.</returns>
        public static GraphExecutionResult ToGraphExecutionResult(CutsceneNodeResult result)
        {
            return new GraphExecutionResult(
                ToGraphNodeStatus(result.Status),
                result.OutputKey,
                result.FailureReason);
        }

        /// <summary>
        /// Converts one GraphCore execution result into one legacy cutscene node result.
        /// </summary>
        /// <param name="result">GraphCore execution result.</param>
        /// <returns>The converted cutscene node result.</returns>
        public static CutsceneNodeResult ToCutsceneNodeResult(GraphExecutionResult result)
        {
            return new CutsceneNodeResult(
                ToCutsceneNodeStatus(result.Status),
                result.OutputKey,
                result.FailureReason);
        }

        /// <summary>
        /// Converts one legacy cutscene node status into one GraphCore node status.
        /// </summary>
        /// <param name="status">Legacy cutscene node status.</param>
        /// <returns>The converted GraphCore node status.</returns>
        public static GraphNodeStatus ToGraphNodeStatus(CutsceneNodeStatus status)
        {
            return status switch
            {
                CutsceneNodeStatus.Running => GraphNodeStatus.Running,
                CutsceneNodeStatus.Success => GraphNodeStatus.Success,
                CutsceneNodeStatus.Failure => GraphNodeStatus.Failure,
                _ => GraphNodeStatus.Failure,
            };
        }

        /// <summary>
        /// Converts one GraphCore node status into one legacy cutscene node status.
        /// </summary>
        /// <param name="status">GraphCore node status.</param>
        /// <returns>The converted cutscene node status.</returns>
        public static CutsceneNodeStatus ToCutsceneNodeStatus(GraphNodeStatus status)
        {
            return status switch
            {
                GraphNodeStatus.Running => CutsceneNodeStatus.Running,
                GraphNodeStatus.Success => CutsceneNodeStatus.Success,
                GraphNodeStatus.Failure => CutsceneNodeStatus.Failure,
                _ => CutsceneNodeStatus.Failure,
            };
        }

        /// <summary>
        /// Converts one legacy cutscene run status into one GraphCore run status.
        /// </summary>
        /// <param name="status">Legacy cutscene run status.</param>
        /// <returns>The converted GraphCore run status.</returns>
        public static GraphRunStatus ToGraphRunStatus(CutsceneRunStatus status)
        {
            return status switch
            {
                CutsceneRunStatus.Idle => GraphRunStatus.Idle,
                CutsceneRunStatus.Running => GraphRunStatus.Running,
                CutsceneRunStatus.Success => GraphRunStatus.Success,
                CutsceneRunStatus.Failed => GraphRunStatus.Failed,
                CutsceneRunStatus.Cancelled => GraphRunStatus.Cancelled,
                _ => GraphRunStatus.Failed,
            };
        }

        /// <summary>
        /// Converts one GraphCore run status into one legacy cutscene run status.
        /// </summary>
        /// <param name="status">GraphCore run status.</param>
        /// <returns>The converted cutscene run status.</returns>
        public static CutsceneRunStatus ToCutsceneRunStatus(GraphRunStatus status)
        {
            return status switch
            {
                GraphRunStatus.Idle => CutsceneRunStatus.Idle,
                GraphRunStatus.Running => CutsceneRunStatus.Running,
                GraphRunStatus.Success => CutsceneRunStatus.Success,
                GraphRunStatus.Failed => CutsceneRunStatus.Failed,
                GraphRunStatus.Cancelled => CutsceneRunStatus.Cancelled,
                _ => CutsceneRunStatus.Failed,
            };
        }
    }
}