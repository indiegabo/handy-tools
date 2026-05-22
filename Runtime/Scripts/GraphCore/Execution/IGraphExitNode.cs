namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Handles the exit phase for one GraphCore-native runtime node.
    /// </summary>
    public interface IGraphExitNode : IGraphRuntimeNode
    {
        /// <summary>
        /// Attempts to exit the node through one graph-neutral execution context.
        /// </summary>
        /// <param name="context">Execution context bound to the active node instance.</param>
        /// <returns>True when the exit step was accepted.</returns>
        bool TryExit(IGraphNodeExecutionContext context);
    }
}