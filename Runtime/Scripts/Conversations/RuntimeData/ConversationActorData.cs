using System;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.RuntimeData
{
    /// <summary>
    /// Stores one exported conversant available to one runtime conversation payload.
    /// </summary>
    [Serializable]
    public sealed class ConversationActorData
    {
        [SerializeField]
        private SerializableGuid _actorId;

        [SerializeField]
        private string _key = string.Empty;

        [SerializeField]
        private string _displayName = string.Empty;

        [SerializeField]
        private Color _accentColor = Color.white;

        /// <summary>
        /// Initializes an empty actor DTO.
        /// </summary>
        public ConversationActorData()
        {
        }

        /// <summary>
        /// Initializes one exported actor DTO.
        /// </summary>
        /// <param name="actorId">Stable conversant identifier.</param>
        /// <param name="key">Normalized authored conversant key.</param>
        /// <param name="displayName">Resolved display name used by runtime presenters.</param>
        /// <param name="accentColor">Authored accent color used by runtime presenters.</param>
        public ConversationActorData(
            SerializableGuid actorId,
            string key,
            string displayName,
            Color accentColor)
        {
            _actorId = actorId;
            _key = key ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _accentColor = accentColor;
        }

        /// <summary>
        /// Gets the stable identifier of the conversant.
        /// </summary>
        public SerializableGuid ActorId => _actorId;

        /// <summary>
        /// Gets the authored conversant key used by runtime bindings.
        /// </summary>
        public string Key => _key ?? string.Empty;

        /// <summary>
        /// Gets the resolved display name used by runtime presenters.
        /// </summary>
        public string DisplayName => _displayName ?? string.Empty;

        /// <summary>
        /// Gets the authored accent color used by runtime presenters.
        /// </summary>
        public Color AccentColor => _accentColor;
    }
}