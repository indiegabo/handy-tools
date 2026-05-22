namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Executes one GraphCore-native runtime node that completes immediately.
    /// </summary>
    public interface IGraphExecuteNode : IGraphRuntimeNode
    {
        /// <summary>
        /// Attempts to execute the node through one graph-neutral execution context.
        /// </summary>
        /// <param name="context">Execution context bound to the active node instance.</param>
        /// <returns>True when the execution step was accepted.</returns>
        bool TryExecute(IGraphNodeExecutionContext context);
    }
}