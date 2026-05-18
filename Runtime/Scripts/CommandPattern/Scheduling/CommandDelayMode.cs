namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines the delay source used by one scheduled command.
    /// </summary>
    public enum CommandDelayMode
    {
        Immediate = 0,
        NextFrame = 1,
        ScaledDelay = 2,
        UnscaledDelay = 3,
    }
}