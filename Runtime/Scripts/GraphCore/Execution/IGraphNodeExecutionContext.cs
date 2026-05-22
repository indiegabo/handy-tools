using System;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Exposes graph-neutral execution services used by reusable runtime nodes.
    /// </summary>
    public interface IGraphNodeExecutionContext
    {
        /// <summary>
        /// Gets the identifier of the node currently being executed.
        /// </summary>
        SerializableGuid CurrentNodeId { get; }

        /// <summary>
        /// Gets the identifier of the current node-execution instance.
        /// </summary>
        SerializableGuid CurrentNodeExecutionId { get; }

        /// <summary>
        /// Gets the runtime blackboard exposed to the active graph run.
        /// </summary>
        GraphBlackboard RuntimeBlackboard { get; }

        /// <summary>
        /// Attempts to complete the current node execution.
        /// </summary>
        /// <param name="result">Execution result to publish.</param>
        /// <returns>True when the completion was accepted.</returns>
        bool TryComplete(GraphExecutionResult result);

        /// <summary>
        /// Attempts to complete one specific node-execution instance.
        /// </summary>
        /// <param name="executionId">Execution identifier to complete.</param>
        /// <param name="result">Execution result to publish.</param>
        /// <returns>True when the completion was accepted.</returns>
        bool TryCompleteNode(SerializableGuid executionId, GraphExecutionResult result);

        /// <summary>
        /// Returns an existing node-state value or creates one through the provided factory.
        /// </summary>
        /// <typeparam name="T">Runtime state type.</typeparam>
        /// <param name="key">Stable state key scoped to the current node.</param>
        /// <param name="factory">Factory used when no state value exists yet.</param>
        /// <returns>The existing or created node-state value.</returns>
        T GetOrCreateNodeState<T>(string key, Func<T> factory);

        /// <summary>
        /// Attempts to read one node-state value.
        /// </summary>
        /// <typeparam name="T">Expected runtime state type.</typeparam>
        /// <param name="key">Stable state key scoped to the current node.</param>
        /// <param name="value">Resolved state value when available.</param>
        /// <returns>True when one compatible state value exists.</returns>
        bool TryGetNodeState<T>(string key, out T value);

        /// <summary>
        /// Stores one node-state value under the provided key.
        /// </summary>
        /// <typeparam name="T">Runtime state type.</typeparam>
        /// <param name="key">Stable state key scoped to the current node.</param>
        /// <param name="value">State value that should be stored.</param>
        void SetNodeState<T>(string key, T value);

        /// <summary>
        /// Removes one node-state value from the current node scope.
        /// </summary>
        /// <param name="key">Stable state key scoped to the current node.</param>
        void RemoveNodeState(string key);
    }
}