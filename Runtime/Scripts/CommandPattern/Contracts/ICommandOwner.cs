namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines optional ownership metadata surfaced by one command.
    /// </summary>
    public interface ICommandOwner
    {
        /// <summary>
        /// Gets the stable owner identifier associated with the command.
        /// </summary>
        string OwnerId { get; }
    }
}