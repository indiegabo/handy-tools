namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Represents one published command lifecycle transition.
    /// </summary>
    public readonly struct CommandLifecycleEvent
    {
        /// <summary>
        /// Creates one lifecycle event payload.
        /// </summary>
        /// <param name="kind">Lifecycle event kind.</param>
        /// <param name="entry">Journal entry associated with the transition.</param>
        public CommandLifecycleEvent(
            CommandLifecycleEventKind kind,
            CommandJournalEntry entry)
        {
            Kind = kind;
            Entry = entry;
        }

        /// <summary>
        /// Gets the lifecycle event kind.
        /// </summary>
        public CommandLifecycleEventKind Kind { get; }

        /// <summary>
        /// Gets the journal entry associated with the transition.
        /// </summary>
        public CommandJournalEntry Entry { get; }
    }
}