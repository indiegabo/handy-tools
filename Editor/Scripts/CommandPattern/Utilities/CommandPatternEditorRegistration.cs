using IndieGabo.HandyTools.Editor;

namespace IndieGabo.HandyTools.Editor.CommandPatternModule
{
    /// <summary>
    /// Centralizes menu and refresh constants for Command Pattern editor tools.
    /// </summary>
    internal static class CommandPatternEditorRegistration
    {
        /// <summary>
        /// Menu path for the play-mode command monitor window.
        /// </summary>
        public const string MonitorMenuPath =
            HandyToolsEditorMenuPaths.Root + "Command Pattern/Monitor";

        /// <summary>
        /// Refresh cadence for the monitor polling loop.
        /// </summary>
        public const int RefreshIntervalMilliseconds = 250;
    }
}