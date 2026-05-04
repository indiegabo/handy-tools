namespace IndieGabo.HandyTools.Editor.Modules
{
    /// <summary>
    /// Describes whether a module configuration panel is currently locked and
    /// why editing is unavailable.
    /// </summary>
    public readonly struct HandyModulePanelLockState
    {
        /// <summary>
        /// Creates a panel lock state.
        /// </summary>
        /// <param name="isLocked">Whether the panel is currently locked.</param>
        /// <param name="title">Short status title shown to the user.</param>
        /// <param name="message">Detailed reason for the current lock state.</param>
        public HandyModulePanelLockState(bool isLocked, string title, string message)
        {
            IsLocked = isLocked;
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// Gets an unlocked state with no explanatory message.
        /// </summary>
        public static HandyModulePanelLockState Unlocked =>
            new(false, string.Empty, string.Empty);

        /// <summary>
        /// Gets whether the panel is currently locked.
        /// </summary>
        public bool IsLocked { get; }

        /// <summary>
        /// Gets the short status title shown to the user.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the detailed reason for the current lock state.
        /// </summary>
        public string Message { get; }
    }
}