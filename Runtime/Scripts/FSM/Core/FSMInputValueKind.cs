namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Describes how one input value should be interpreted by the FSM input cache.
    /// </summary>
    public enum FSMInputValueKind
    {
        /// <summary>
        /// The input represents a pressed or released state.
        /// </summary>
        Button,

        /// <summary>
        /// The input represents one scalar value.
        /// </summary>
        Float,

        /// <summary>
        /// The input represents one two-dimensional vector.
        /// </summary>
        Vector2
    }
}