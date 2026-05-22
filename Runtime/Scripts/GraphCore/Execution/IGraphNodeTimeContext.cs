namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Exposes time data required by runtime nodes that advance across ticks.
    /// </summary>
    public interface IGraphNodeTimeContext
    {
        /// <summary>
        /// Gets the scaled delta time for the current update step.
        /// </summary>
        float DeltaTime { get; }

        /// <summary>
        /// Gets the unscaled delta time for the current update step.
        /// </summary>
        float UnscaledDeltaTime { get; }
    }
}