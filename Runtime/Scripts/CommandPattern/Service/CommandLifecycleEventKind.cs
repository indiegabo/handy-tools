namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines the lifecycle transition published by the command service.
    /// </summary>
    public enum CommandLifecycleEventKind
    {
        Queued = 0,
        Scheduled = 1,
        Started = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5,
        Undone = 6,
        Redone = 7,
    }
}