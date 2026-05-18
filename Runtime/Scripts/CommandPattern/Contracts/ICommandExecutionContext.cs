using System;
using System.Threading;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Exposes immutable execution metadata and service resolution helpers to
    /// commands and undo operations.
    /// </summary>
    public interface ICommandExecutionContext
    {
        /// <summary>
        /// Gets the original command request that owns the execution.
        /// </summary>
        CommandRequest Request { get; }

        /// <summary>
        /// Gets the unique execution identifier.
        /// </summary>
        Guid ExecutionId { get; }

        /// <summary>
        /// Gets the originating schedule identifier when the execution was
        /// created from scheduled work.
        /// </summary>
        Guid ScheduleId { get; }

        /// <summary>
        /// Gets the monotonic runtime sequence assigned to the execution.
        /// </summary>
        long Sequence { get; }

        /// <summary>
        /// Gets the execution mode for the current invocation.
        /// </summary>
        CommandExecutionMode Mode { get; }

        /// <summary>
        /// Gets the UTC timestamp when the execution record was created.
        /// </summary>
        DateTimeOffset CreatedAtUtc { get; }

        /// <summary>
        /// Gets the UTC timestamp when execution began.
        /// </summary>
        DateTimeOffset StartedAtUtc { get; }

        /// <summary>
        /// Gets the cancellation token for the current operation.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Attempts to resolve one runtime service from the global locator.
        /// </summary>
        /// <typeparam name="T">Service type.</typeparam>
        /// <param name="service">Resolved service when available.</param>
        /// <returns>True when the service exists.</returns>
        bool TryGetService<T>(out T service)
            where T : class;

        /// <summary>
        /// Resolves one required runtime service from the global locator.
        /// </summary>
        /// <typeparam name="T">Service type.</typeparam>
        /// <returns>The resolved service.</returns>
        T GetRequiredService<T>()
            where T : class;
    }
}