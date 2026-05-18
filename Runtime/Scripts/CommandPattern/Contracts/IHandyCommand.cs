using System.Threading;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines the minimal command contract required by the command runtime.
    /// </summary>
    public interface IHandyCommand
    {
        /// <summary>
        /// Gets the immutable descriptor used for routing and diagnostics.
        /// </summary>
        CommandDescriptor Descriptor { get; }

        /// <summary>
        /// Executes the command against the provided runtime context.
        /// </summary>
        /// <param name="context">Execution context for the current request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The execution result for the command.</returns>
        Awaitable<CommandExecutionResult> ExecuteAsync(
            ICommandExecutionContext context,
            CancellationToken cancellationToken = default);
    }
}