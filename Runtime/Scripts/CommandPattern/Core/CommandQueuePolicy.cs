namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines the queue arbitration policy for one command request.
    /// </summary>
    public enum CommandQueuePolicy
    {
        Parallel = 0,
        Serial = 1,
        RejectWhenBusy = 2,
    }
}