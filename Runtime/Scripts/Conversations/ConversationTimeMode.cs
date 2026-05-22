namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Identifies the time source used by Conversations runtime wait nodes.
    /// </summary>
    public enum ConversationTimeMode
    {
        /// <summary>
        /// Uses scaled delta time.
        /// </summary>
        Scaled,

        /// <summary>
        /// Uses unscaled delta time.
        /// </summary>
        Unscaled,
    }
}