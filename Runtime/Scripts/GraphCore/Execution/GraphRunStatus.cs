namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Describes the overall status of one graph run.
    /// </summary>
    public enum GraphRunStatus
    {
        /// <summary>
        /// The run has not started yet.
        /// </summary>
        Idle,

        /// <summary>
        /// The run is currently executing.
        /// </summary>
        Running,

        /// <summary>
        /// The run completed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// The run ended with a failure outcome.
        /// </summary>
        Failed,

        /// <summary>
        /// The run ended because execution was cancelled.
        /// </summary>
        Cancelled,
    }
}