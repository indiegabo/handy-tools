namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Resolves the graph-binding context for serialized properties hosted by one object
    /// that can contain more than one authored graph.
    /// </summary>
    public interface IGraphPropertyBindingContextProvider
    {
        /// <summary>
        /// Attempts to resolve the graph, owner, and family metadata bound to one
        /// serialized property path.
        /// </summary>
        /// <param name="propertyPath">Serialized property path being drawn.</param>
        /// <param name="graph">Resolved authored graph for the property path.</param>
        /// <param name="hostOwner">Resolved host-owner token used for drag-session isolation.</param>
        /// <param name="familyId">Resolved graph family identifier.</param>
        /// <returns>True when the property path maps to one authored graph context.</returns>
        bool TryResolveGraphPropertyBinding(
            string propertyPath,
            out GraphDefinition graph,
            out object hostOwner,
            out string familyId);
    }
}