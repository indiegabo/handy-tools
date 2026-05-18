using System;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Events
{
    /// <summary>
    /// Stores the serialized identity for one selected cutscene bus event.
    /// </summary>
    [Serializable]
    public sealed class CutsceneBusEventReference
    {
        #region Inspector

        [SerializeField] private string _eventPath;

        [SerializeField] private string _eventTypeName;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the stable logical path shown in editor pickers.
        /// </summary>
        public string EventPath => _eventPath ?? string.Empty;

        /// <summary>
        /// Gets the assembly-qualified type name cached for fast resolution.
        /// </summary>
        public string EventTypeName => _eventTypeName ?? string.Empty;

        #endregion

        #region Public API

        /// <summary>
        /// Updates the serialized identity fields with one resolved event
        /// selection.
        /// </summary>
        /// <param name="eventPath">Stable logical event path.</param>
        /// <param name="eventTypeName">Assembly-qualified event type name.</param>
        public void Assign(string eventPath, string eventTypeName)
        {
            _eventPath = eventPath ?? string.Empty;
            _eventTypeName = eventTypeName ?? string.Empty;
        }

        #endregion
    }
}