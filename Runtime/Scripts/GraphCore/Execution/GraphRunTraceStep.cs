using System;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Stores one node-level event captured during one graph run.
    /// </summary>
    [Serializable]
    public sealed class GraphRunTraceStep
    {
        [SerializeField] private SerializableGuid _nodeId;
        [SerializeField] private GraphNodeStatus _nodeStatus;
        [SerializeField] private string _outputKey = string.Empty;
        [SerializeField] private string _message = string.Empty;

        /// <summary>
        /// Initializes one empty trace step for Unity serialization.
        /// </summary>
        public GraphRunTraceStep()
        {
        }

        /// <summary>
        /// Initializes one node trace step.
        /// </summary>
        /// <param name="nodeId">Node identifier associated with the step.</param>
        /// <param name="nodeStatus">Per-node execution status.</param>
        /// <param name="outputKey">Traversed output key when available.</param>
        /// <param name="message">Optional diagnostic or failure message.</param>
        public GraphRunTraceStep(
            SerializableGuid nodeId,
            GraphNodeStatus nodeStatus,
            string outputKey,
            string message)
        {
            _nodeId = nodeId;
            _nodeStatus = nodeStatus;
            _outputKey = outputKey ?? string.Empty;
            _message = message ?? string.Empty;
        }

        /// <summary>
        /// Gets the node identifier associated with the step.
        /// </summary>
        public SerializableGuid NodeId => _nodeId;

        /// <summary>
        /// Gets the per-node execution status.
        /// </summary>
        public GraphNodeStatus NodeStatus => _nodeStatus;

        /// <summary>
        /// Gets the traversed output key when available.
        /// </summary>
        public string OutputKey => _outputKey;

        /// <summary>
        /// Gets the optional diagnostic or failure message.
        /// </summary>
        public string Message => _message;
    }
}