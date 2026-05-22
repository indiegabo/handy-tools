namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Handles the enter phase for one GraphCore-native runtime node.
    /// </summary>
    public interface IGraphEnterNode : IGraphRuntimeNode
    {
        /// <summary>
        /// Attempts to enter the node through one graph-neutral execution context.
        /// </summary>
        /// <param name="context">Execution context bound to the active node instance.</param>
        /// <returns>True when the enter step was accepted.</returns>
        bool TryEnter(IGraphNodeExecutionContext context);
    }
}