namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Describes why one pending command was cancelled or rejected.
    /// </summary>
    public enum CommandCancellationReason
    {
        None = 0,
        UserRequested = 1,
        QueueRejected = 2,
        ScopeCancelled = 3,
        OwnerCancelled = 4,
        TagCancelled = 5,
        ServiceReset = 6,
    }
}