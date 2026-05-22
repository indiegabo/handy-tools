namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Handles the tick phase for one GraphCore-native runtime node.
    /// </summary>
    public interface IGraphTickNode : IGraphRuntimeNode
    {
        /// <summary>
        /// Attempts to tick the node through one graph-neutral execution context.
        /// </summary>
        /// <param name="context">Execution context bound to the active node instance.</param>
        /// <returns>True when the tick step was accepted.</returns>
        bool TryTick(IGraphNodeExecutionContext context);
    }
}