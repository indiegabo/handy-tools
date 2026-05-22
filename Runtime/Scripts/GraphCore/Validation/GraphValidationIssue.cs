using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.GraphCore.Validation
{
    /// <summary>
    /// Represents one validation issue emitted while inspecting one graph.
    /// </summary>
    public readonly struct GraphValidationIssue
    {
        /// <summary>
        /// Initializes one validation issue.
        /// </summary>
        /// <param name="severity">Severity assigned to the issue.</param>
        /// <param name="message">Human-readable issue message.</param>
        /// <param name="nodeId">Primary node associated with the issue.</param>
        public GraphValidationIssue(
            GraphValidationSeverity severity,
            string message,
            SerializableGuid nodeId)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            NodeId = nodeId;
        }

        /// <summary>
        /// Gets the severity assigned to the issue.
        /// </summary>
        public GraphValidationSeverity Severity { get; }

        /// <summary>
        /// Gets the human-readable issue message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the primary node associated with the issue.
        /// </summary>
        public SerializableGuid NodeId { get; }
    }
}