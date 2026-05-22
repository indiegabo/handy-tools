using System;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Represents the output chosen by one node execution step.
    /// </summary>
    [Serializable]
    public readonly struct GraphExecutionResult
    {
        /// <summary>
        /// Initializes one execution result.
        /// </summary>
        /// <param name="status">Per-node execution status.</param>
        /// <param name="outputKey">Output key chosen by the node.</param>
        /// <param name="failureReason">Failure message when execution failed.</param>
        public GraphExecutionResult(
            GraphNodeStatus status,
            string outputKey,
            string failureReason)
        {
            Status = status;
            OutputKey = outputKey;
            FailureReason = failureReason;
        }

        /// <summary>
        /// Gets the per-node execution status.
        /// </summary>
        public GraphNodeStatus Status { get; }

        /// <summary>
        /// Gets the output key chosen by the node.
        /// </summary>
        public string OutputKey { get; }

        /// <summary>
        /// Gets the failure message when execution failed.
        /// </summary>
        public string FailureReason { get; }

        /// <summary>
        /// Creates one successful execution result.
        /// </summary>
        /// <param name="outputKey">Output key chosen by the node.</param>
        /// <returns>One success result.</returns>
        public static GraphExecutionResult Success(string outputKey = GraphPortKeys.Next)
        {
            return new GraphExecutionResult(
                GraphNodeStatus.Success,
                outputKey,
                string.Empty);
        }

        /// <summary>
        /// Creates one failed execution result.
        /// </summary>
        /// <param name="failureReason">Failure message to store.</param>
        /// <returns>One failure result.</returns>
        public static GraphExecutionResult Failure(string failureReason)
        {
            return new GraphExecutionResult(
                GraphNodeStatus.Failure,
                string.Empty,
                failureReason ?? string.Empty);
        }
    }
}