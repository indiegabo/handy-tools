using System.Threading;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines the runtime undo operation emitted by one successful command.
    /// </summary>
    public interface ICommandUndoOperation
    {
        /// <summary>
        /// Reverses the command effects for the provided execution context.
        /// </summary>
        /// <param name="context">Execution context used for the undo.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An awaitable that completes when the undo finishes.</returns>
        Awaitable UndoAsync(
            ICommandExecutionContext context,
            CancellationToken cancellationToken = default);
    }
}