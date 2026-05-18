using System.Threading;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines one cross-cutting execution middleware for command requests.
    /// </summary>
    public interface ICommandMiddleware
    {
        /// <summary>
        /// Wraps one command request before it reaches the command body.
        /// </summary>
        /// <param name="request">Current command request.</param>
        /// <param name="next">Delegate that continues the pipeline.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The resulting command execution result.</returns>
        Awaitable<CommandExecutionResult> InvokeAsync(
            CommandRequest request,
            CommandPipelineDelegate next,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the next delegate in the command middleware pipeline.
    /// </summary>
    /// <param name="request">Current command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resulting command execution result.</returns>
    public delegate Awaitable<CommandExecutionResult> CommandPipelineDelegate(
        CommandRequest request,
        CancellationToken cancellationToken = default);
}