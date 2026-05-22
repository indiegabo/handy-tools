namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Exposes shared authoring metadata used by graph node creation catalogs.
    /// </summary>
    public interface IGraphNodeMenuMetadata
    {
        /// <summary>
        /// Gets the menu path exposed by authoring surfaces.
        /// </summary>
        string MenuPath { get; }

        /// <summary>
        /// Gets the fallback node title declared by the menu metadata.
        /// </summary>
        string DefaultTitle { get; }
    }
}