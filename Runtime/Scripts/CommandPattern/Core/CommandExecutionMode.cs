namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines why a command or undo operation is currently running.
    /// </summary>
    public enum CommandExecutionMode
    {
        Normal = 0,
        Undo = 1,
        Redo = 2,
    }
}