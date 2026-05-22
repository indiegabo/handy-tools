namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Provides the built-in output keys commonly used by flow graphs.
    /// </summary>
    public static class GraphPortKeys
    {
        /// <summary>
        /// Gets the default forward-flow output key.
        /// </summary>
        public const string Next = "Next";

        /// <summary>
        /// Gets the built-in conditional true output key.
        /// </summary>
        public const string True = "True";

        /// <summary>
        /// Gets the built-in conditional false output key.
        /// </summary>
        public const string False = "False";

        /// <summary>
        /// Gets the built-in completion output key.
        /// </summary>
        public const string Complete = "Complete";
    }
}