namespace IndieGabo.HandyTools.Editor
{
    /// <summary>
    /// Centralizes top-level Handy Tools editor menu paths.
    /// </summary>
    public static class HandyToolsEditorMenuPaths
    {
        /// <summary>
        /// Root menu used for Handy Tools editor entries.
        /// </summary>
        public const string Root = "Handy Tools/";

        /// <summary>
        /// Root submenu used for general Handy Tools configuration entries.
        /// </summary>
        public const string ConfigurationRoot = Root + "Configuration/";

        /// <summary>
        /// Menu path for the unified modules configuration window.
        /// </summary>
        public const string Modules = Root + "Modules";

        /// <summary>
        /// Menu path for the general scripting define configuration window.
        /// </summary>
        public const string ScriptingDefines =
            ConfigurationRoot + "Scripting Defines";
    }
}