namespace IndieGabo.HandyTools.Editor
{
    /// <summary>
    /// Centralizes top-level HandyTools editor menu paths.
    /// </summary>
    public static class HandyToolsEditorMenuPaths
    {
        /// <summary>
        /// Root menu used for HandyTools editor entries.
        /// </summary>
        public const string Root = "HandyTools/";

        /// <summary>
        /// Root submenu used for general HandyTools configuration entries.
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