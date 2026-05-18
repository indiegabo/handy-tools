namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Represents the observable lifecycle status of one command journal entry.
    /// </summary>
    public enum CommandStatus
    {
        Pending = 0,
        Running = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4,
        Undone = 5,
        Redone = 6,
    }
}