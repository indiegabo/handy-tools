using System;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    /// <summary>
    /// Declares the inspector path and descriptive metadata for one cutscene
    /// HandyBus event available to cutscene nodes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CutsceneBusEventAttribute : Attribute
    {
        #region Initialization

        /// <summary>
        /// Creates the attribute with a unique logical path.
        /// </summary>
        /// <param name="path">Stable logical path shown in editor pickers.</param>
        public CutsceneBusEventAttribute(string path)
        {
            Path = path ?? string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the stable logical path shown in editor pickers.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets or sets the display name shown in editor UI.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the descriptive text shown in editor UI.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        #endregion
    }
}