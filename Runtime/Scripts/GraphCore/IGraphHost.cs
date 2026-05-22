namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Describes one owner that exposes a graph family and one Unity object
    /// responsible for hosting the graph payload.
    /// </summary>
    public interface IGraphHost
    {
        /// <summary>
        /// Gets the stable graph family id used to resolve nodes, validators,
        /// and blackboard type contributions.
        /// </summary>
        string GraphFamilyId { get; }

        /// <summary>
        /// Gets the broad ownership model used by this graph host.
        /// </summary>
        GraphHostKind HostKind { get; }

        /// <summary>
        /// Gets the Unity object that owns the serialized graph payload.
        /// </summary>
        UnityEngine.Object HostObject { get; }
    }
}