using System;
using System.Threading;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines the global runtime service responsible for command execution,
    /// scheduling, history, and diagnostics.
    /// </summary>
    public interface ICommandService
    {
        /// <summary>
        /// Executes one command request immediately or enqueues it according to
        /// the request queue policy.
        /// </summary>
        /// <param name="request">Command request to execute.</param>
        /// <returns>A handle that exposes the execution completion awaitable.</returns>
        CommandExecutionHandle Execute(in CommandRequest request);

        /// <summary>
        /// Schedules one command request for future execution.
        /// </summary>
        /// <param name="request">Scheduled command request.</param>
        /// <returns>A handle that identifies the pending scheduled entry.</returns>
        CommandScheduleHandle Schedule(in CommandScheduleRequest request);

        /// <summary>
        /// Attempts to cancel one pending scheduled command.
        /// </summary>
        /// <param name="handle">Scheduled command handle.</param>
        /// <param name="reason">Reason recorded for the cancellation.</param>
        /// <returns>True when the pending entry was cancelled.</returns>
        bool TryCancelScheduled(
            in CommandScheduleHandle handle,
            CommandCancellationReason reason =
                CommandCancellationReason.UserRequested);

        /// <summary>
        /// Undoes the latest eligible command in the requested history scope.
        /// </summary>
        /// <param name="request">Undo request metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the undo operation.</returns>
        Awaitable<CommandUndoResult> UndoAsync(
            CommandUndoRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Redoes the latest eligible command in the requested history scope.
        /// </summary>
        /// <param name="request">Redo request metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the redo operation.</returns>
        Awaitable<CommandRedoResult> RedoAsync(
            CommandRedoRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns an immutable journal snapshot for the requested filter.
        /// </summary>
        /// <param name="query">Snapshot filter.</param>
        /// <returns>The generated journal snapshot.</returns>
        CommandJournalSnapshot GetSnapshot(in CommandQuery query);

        /// <summary>
        /// Registers one middleware component in the execution pipeline.
        /// </summary>
        /// <param name="middleware">Middleware to register.</param>
        void RegisterMiddleware(ICommandMiddleware middleware);

        /// <summary>
        /// Removes one middleware component from the execution pipeline.
        /// </summary>
        /// <param name="middleware">Middleware to remove.</param>
        /// <returns>True when the middleware was removed.</returns>
        bool DeregisterMiddleware(ICommandMiddleware middleware);

        /// <summary>
        /// Publishes typed lifecycle updates whenever command state changes.
        /// </summary>
        event Action<CommandLifecycleEvent> LifecycleEventPublished;
    }
}