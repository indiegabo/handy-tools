using System;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    /// <summary>
    /// Stores editor-facing metadata for one registered cutscene bus event
    /// type.
    /// </summary>
    public sealed class CutsceneBusEventMetadata
    {
        #region Initialization

        /// <summary>
        /// Creates metadata for one registered cutscene bus event type.
        /// </summary>
        /// <param name="eventType">Concrete event type.</param>
        /// <param name="path">Stable logical path.</param>
        /// <param name="displayName">Human-readable display name.</param>
        /// <param name="description">Human-readable description.</param>
        public CutsceneBusEventMetadata(
            Type eventType,
            string path,
            string displayName,
            string description)
        {
            EventType = eventType;
            Path = path ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the concrete event type.
        /// </summary>
        public Type EventType { get; }

        /// <summary>
        /// Gets the stable logical path used by inspector pickers.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the human-readable display name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the human-readable description.
        /// </summary>
        public string Description { get; }

        #endregion
    }
}