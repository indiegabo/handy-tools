namespace IndieGabo.HandyTools.GraphCore.Validation
{
    /// <summary>
    /// Identifies the severity assigned to one graph validation issue.
    /// </summary>
    public enum GraphValidationSeverity
    {
        /// <summary>
        /// Informational issue that does not block graph authoring.
        /// </summary>
        Info,

        /// <summary>
        /// Warning issue that highlights a likely authoring problem.
        /// </summary>
        Warning,

        /// <summary>
        /// Error issue that indicates invalid graph topology.
        /// </summary>
        Error,
    }
}