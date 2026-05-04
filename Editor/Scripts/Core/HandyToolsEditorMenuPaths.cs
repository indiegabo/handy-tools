namespace IndieGabo.HandyTools.Editor
{
    /// <summary>
    /// Centralizes top-level HandyTools editor menu paths.
    /// </summary>
    public static class HandyToolsEditorMenuPaths
    {
        /// <summary>
        /// Root submenu used for all module configuration entries.
        /// </summary>
        public const string ModulesRoot = "HandyTools/Modules/";

        /// <summary>
        /// Root submenu used for general HandyTools configuration entries.
        /// </summary>
        public const string ConfigurationRoot = "HandyTools/Configuration/";

        /// <summary>
        /// Menu path for the unified modules configuration window.
        /// </summary>
        public const string Modules = ModulesRoot + "Configuration";

        /// <summary>
        /// Menu path for the Gameplay module panel.
        /// </summary>
        public const string Gameplay = ModulesRoot + "Gameplay";

        /// <summary>
        /// Menu path for the Input module panel.
        /// </summary>
        public const string Input = ModulesRoot + "Input";

        /// <summary>
        /// Menu path for the Save System module panel.
        /// </summary>
        public const string SaveSystem = ModulesRoot + "Save System";

        /// <summary>
        /// Menu path for the Debugging module panel.
        /// </summary>
        public const string Debugging = ModulesRoot + "Debugging";

        /// <summary>
        /// Menu path for the Logging module panel.
        /// </summary>
        public const string Logging = ModulesRoot + "Logging";

        /// <summary>
        /// Menu path for the Globals module panel.
        /// </summary>
        public const string Globals = ModulesRoot + "Globals";

        /// <summary>
        /// Menu path for the Steam module panel.
        /// </summary>
        public const string Steam = ModulesRoot + "Steam";

        /// <summary>
        /// Menu path for the ScreenShooter module panel.
        /// </summary>
        public const string ScreenShooter = ModulesRoot + "ScreenShooter";

        /// <summary>
        /// Menu path for the general scripting define configuration window.
        /// </summary>
        public const string ScriptingDefines =
            ConfigurationRoot + "Scripting Defines";
    }
}