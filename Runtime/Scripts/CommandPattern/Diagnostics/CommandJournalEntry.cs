using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Represents one immutable command journal snapshot entry.
    /// </summary>
    public readonly struct CommandJournalEntry
    {
        /// <summary>
        /// Creates one journal entry.
        /// </summary>
        public CommandJournalEntry(
            long sequence,
            Guid executionId,
            Guid? scheduleId,
            string commandType,
            string displayName,
            CommandStatus status,
            string scope,
            string queue,
            string ownerId,
            IReadOnlyList<string> tags,
            CommandDelayMode delayMode,
            DateTimeOffset createdAtUtc,
            DateTimeOffset? scheduledForUtc,
            DateTimeOffset? startedAtUtc,
            DateTimeOffset? completedAtUtc,
            bool isUndoable,
            bool canRedo,
            string failureReason,
            string cancellationReason,
            IReadOnlyDictionary<string, string> metadata)
        {
            Sequence = sequence;
            ExecutionId = executionId;
            ScheduleId = scheduleId;
            CommandType = commandType ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Status = status;
            Scope = scope ?? string.Empty;
            Queue = queue ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
            Tags = tags ?? Array.Empty<string>();
            DelayMode = delayMode;
            CreatedAtUtc = createdAtUtc;
            ScheduledForUtc = scheduledForUtc;
            StartedAtUtc = startedAtUtc;
            CompletedAtUtc = completedAtUtc;
            IsUndoable = isUndoable;
            CanRedo = canRedo;
            FailureReason = failureReason ?? string.Empty;
            CancellationReason = cancellationReason ?? string.Empty;
            Metadata = metadata ?? new Dictionary<string, string>(0);
        }

        public long Sequence { get; }
        public Guid ExecutionId { get; }
        public Guid? ScheduleId { get; }
        public string CommandType { get; }
        public string DisplayName { get; }
        public CommandStatus Status { get; }
        public string Scope { get; }
        public string Queue { get; }
        public string OwnerId { get; }
        public IReadOnlyList<string> Tags { get; }
        public CommandDelayMode DelayMode { get; }
        public DateTimeOffset CreatedAtUtc { get; }
        public DateTimeOffset? ScheduledForUtc { get; }
        public DateTimeOffset? StartedAtUtc { get; }
        public DateTimeOffset? CompletedAtUtc { get; }
        public bool IsUndoable { get; }
        public bool CanRedo { get; }
        public string FailureReason { get; }
        public string CancellationReason { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
    }
}