using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Represents one immutable grouped snapshot of the command journal.
    /// </summary>
    public readonly struct CommandJournalSnapshot
    {
        /// <summary>
        /// Creates one journal snapshot.
        /// </summary>
        public CommandJournalSnapshot(
            DateTimeOffset generatedAtUtc,
            IReadOnlyList<CommandJournalEntry> pending,
            IReadOnlyList<CommandJournalEntry> running,
            IReadOnlyList<CommandJournalEntry> completed,
            IReadOnlyList<CommandJournalEntry> failed,
            IReadOnlyList<CommandJournalEntry> cancelled,
            IReadOnlyList<CommandJournalEntry> undone,
            IReadOnlyList<CommandJournalEntry> redone)
        {
            GeneratedAtUtc = generatedAtUtc;
            Pending = pending ?? Array.Empty<CommandJournalEntry>();
            Running = running ?? Array.Empty<CommandJournalEntry>();
            Completed = completed ?? Array.Empty<CommandJournalEntry>();
            Failed = failed ?? Array.Empty<CommandJournalEntry>();
            Cancelled = cancelled ?? Array.Empty<CommandJournalEntry>();
            Undone = undone ?? Array.Empty<CommandJournalEntry>();
            Redone = redone ?? Array.Empty<CommandJournalEntry>();
        }

        public DateTimeOffset GeneratedAtUtc { get; }
        public IReadOnlyList<CommandJournalEntry> Pending { get; }
        public IReadOnlyList<CommandJournalEntry> Running { get; }
        public IReadOnlyList<CommandJournalEntry> Completed { get; }
        public IReadOnlyList<CommandJournalEntry> Failed { get; }
        public IReadOnlyList<CommandJournalEntry> Cancelled { get; }
        public IReadOnlyList<CommandJournalEntry> Undone { get; }
        public IReadOnlyList<CommandJournalEntry> Redone { get; }
    }
}