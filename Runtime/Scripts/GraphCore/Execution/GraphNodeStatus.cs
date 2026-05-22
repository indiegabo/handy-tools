namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Describes the per-node execution outcome used by runtime traces.
    /// </summary>
    public enum GraphNodeStatus
    {
        /// <summary>
        /// The node has started and is still running.
        /// </summary>
        Running,

        /// <summary>
        /// The node completed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// The node completed with a failure outcome.
        /// </summary>
        Failure,
    }
}