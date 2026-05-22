namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Identifies the logical scope targeted by one blackboard variable reference.
    /// </summary>
    public enum GraphBlackboardReferenceScope
    {
        /// <summary>
        /// The reference resolves against the graph-local blackboard.
        /// </summary>
        GraphLocal,

        /// <summary>
        /// The reference resolves through one host-provided external scope.
        /// </summary>
        External,

        /// <summary>
        /// The reference resolves through one host-provided persistent scope.
        /// </summary>
        Persistent,
    }
}