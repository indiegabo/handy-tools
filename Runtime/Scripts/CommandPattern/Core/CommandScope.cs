namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines the default public routing keys used by the command runtime.
    /// </summary>
    public static class CommandScope
    {
        /// <summary>
        /// Default history scope used when no explicit scope is supplied.
        /// </summary>
        public const string Global = "global";

        /// <summary>
        /// Default queue name used when no explicit queue is supplied.
        /// </summary>
        public const string DefaultQueue = "default";
    }
}