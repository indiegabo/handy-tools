using System;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Stores the private undo and redo data for one successful execution.
    /// </summary>
    internal sealed class CommandHistoryEntry
    {
        /// <summary>
        /// Creates one history entry.
        /// </summary>
        /// <param name="executionId">Original execution identifier.</param>
        /// <param name="sequence">Original execution sequence.</param>
        /// <param name="request">Original command request.</param>
        /// <param name="scheduleId">Origin schedule identifier when present.</param>
        /// <param name="undoOperation">Undo operation emitted by the command.</param>
        /// <param name="allowRedo">Whether redo is allowed after undo.</param>
        /// <param name="createdAtUtc">Execution creation timestamp.</param>
        public CommandHistoryEntry(
            Guid executionId,
            long sequence,
            CommandRequest request,
            Guid? scheduleId,
            ICommandUndoOperation undoOperation,
            bool allowRedo,
            DateTimeOffset createdAtUtc)
        {
            ExecutionId = executionId;
            Sequence = sequence;
            Request = request;
            ScheduleId = scheduleId;
            UndoOperation = undoOperation;
            AllowRedo = allowRedo;
            CreatedAtUtc = createdAtUtc;
        }

        /// <summary>
        /// Gets the original execution identifier.
        /// </summary>
        public Guid ExecutionId { get; }

        /// <summary>
        /// Gets the original execution sequence.
        /// </summary>
        public long Sequence { get; }

        /// <summary>
        /// Gets the original command request.
        /// </summary>
        public CommandRequest Request { get; }

        /// <summary>
        /// Gets the origin schedule identifier when the execution was scheduled.
        /// </summary>
        public Guid? ScheduleId { get; }

        /// <summary>
        /// Gets the undo operation emitted by the command.
        /// </summary>
        public ICommandUndoOperation UndoOperation { get; }

        /// <summary>
        /// Gets whether redo is allowed after a successful undo.
        /// </summary>
        public bool AllowRedo { get; }

        /// <summary>
        /// Gets the execution creation timestamp.
        /// </summary>
        public DateTimeOffset CreatedAtUtc { get; }
    }
}