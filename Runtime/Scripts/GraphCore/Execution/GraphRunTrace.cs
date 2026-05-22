using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Stores the ordered node-level trace emitted by one graph run.
    /// </summary>
    public sealed class GraphRunTrace
    {
        private readonly List<GraphRunTraceStep> _steps = new();

        /// <summary>
        /// Gets the ordered trace steps captured during the run.
        /// </summary>
        public IReadOnlyList<GraphRunTraceStep> Steps => _steps;

        /// <summary>
        /// Gets the terminal status of the run.
        /// </summary>
        public GraphRunStatus FinalStatus { get; private set; } = GraphRunStatus.Idle;

        /// <summary>
        /// Gets the terminal message captured when the run ended.
        /// </summary>
        public string FinalMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Appends one node-started event to the trace.
        /// </summary>
        /// <param name="nodeId">Identifier of the started node.</param>
        public void MarkNodeStarted(SerializableGuid nodeId)
        {
            _steps.Add(new GraphRunTraceStep(
                nodeId,
                GraphNodeStatus.Running,
                string.Empty,
                string.Empty));
        }

        /// <summary>
        /// Appends one node-finished event to the trace.
        /// </summary>
        /// <param name="nodeId">Identifier of the finished node.</param>
        /// <param name="status">Per-node execution status.</param>
        /// <param name="outputKey">Traversed output key when available.</param>
        /// <param name="message">Optional diagnostic or failure message.</param>
        public void MarkNodeFinished(
            SerializableGuid nodeId,
            GraphNodeStatus status,
            string outputKey,
            string message)
        {
            _steps.Add(new GraphRunTraceStep(nodeId, status, outputKey, message));
        }

        /// <summary>
        /// Appends one output-traversed event to the trace.
        /// </summary>
        /// <param name="nodeId">Identifier of the origin node.</param>
        /// <param name="outputKey">Traversed output key.</param>
        public void MarkOutputTraversed(SerializableGuid nodeId, string outputKey)
        {
            if (string.IsNullOrWhiteSpace(outputKey))
            {
                return;
            }

            _steps.Add(new GraphRunTraceStep(
                nodeId,
                GraphNodeStatus.Success,
                outputKey,
                string.Empty));
        }

        /// <summary>
        /// Records the terminal status of the run.
        /// </summary>
        /// <param name="status">Terminal run status.</param>
        /// <param name="message">Terminal message to store.</param>
        public void MarkEnded(GraphRunStatus status, string message)
        {
            FinalStatus = status;
            FinalMessage = message ?? string.Empty;
        }

        /// <summary>
        /// Gets whether the trace has already visited one node.
        /// </summary>
        /// <param name="nodeId">Node identifier to inspect.</param>
        /// <returns>True when the node appears in the trace.</returns>
        public bool HasVisited(SerializableGuid nodeId)
        {
            return _steps.Any(step => step.NodeId == nodeId);
        }
    }
}