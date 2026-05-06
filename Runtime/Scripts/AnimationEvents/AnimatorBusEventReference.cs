using System;
using UnityEngine;

namespace IndieGabo.HandyTools.AnimationEventsModule
{
    /// <summary>
    /// Stores the serialized identity for one selected animation bus event.
    /// </summary>
    [Serializable]
    public sealed class AnimatorBusEventReference
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
    }
}