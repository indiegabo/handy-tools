namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Identifies the broad ownership model used by one graph host.
    /// </summary>
    public enum GraphHostKind
    {
        /// <summary>
        /// The graph is owned by one scene object or component.
        /// </summary>
        SceneObject,

        /// <summary>
        /// The graph is owned by one project asset.
        /// </summary>
        Asset,

        /// <summary>
        /// The graph is owned by one custom host that does not fit the default
        /// scene or asset categories.
        /// </summary>
        Custom,
    }
}