using System;
using System.Text;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.Core
{
    /// <summary>
    /// Stores one authored actor entry shared by the conversations indexed in one table.
    /// </summary>
    [Serializable]
    public sealed class ConversationActorDefinition
    {
        private const string DefaultKey = "actor";

        [SerializeField]
        private SerializableGuid _actorId;

        [SerializeField]
        private string _key = DefaultKey;

        [SerializeField]
        private string _displayName = string.Empty;

        [SerializeField]
        private Sprite _portrait;

        [SerializeField]
        private Color _themeColor = Color.white;

        [SerializeField]
        [TextArea(3, 8)]
        private string _notes = string.Empty;

        /// <summary>
        /// Gets the stable actor identifier.
        /// </summary>
        public SerializableGuid ActorId => _actorId;

        /// <summary>
        /// Gets the authored actor key used by references and tooling.
        /// </summary>
        public string Key => NormalizeKey(_key);

        /// <summary>
        /// Gets the authored actor display name.
        /// </summary>
        public string DisplayName => _displayName ?? string.Empty;

        /// <summary>
        /// Gets the authored portrait reference.
        /// </summary>
        public Sprite Portrait => _portrait;

        /// <summary>
        /// Gets the authored theme color.
        /// </summary>
        public Color ThemeColor => _themeColor;

        /// <summary>
        /// Gets the authored implementation notes.
        /// </summary>
        public string Notes => _notes ?? string.Empty;

        /// <summary>
        /// Creates one default authored conversant entry.
        /// </summary>
        /// <param name="key">Optional authored key.</param>
        /// <returns>The created conversant definition.</returns>
        public static ConversationActorDefinition CreateDefault(string key = null)
        {
            ConversationActorDefinition actor = new()
            {
                _key = NormalizeKey(key),
                _themeColor = Color.white,
            };

            actor.EnsureId();
            return actor;
        }

        /// <summary>
        /// Creates one authored duplicate while preserving the authored presentation data.
        /// </summary>
        /// <param name="key">Authored key for the duplicated entry.</param>
        /// <returns>The duplicated conversant definition.</returns>
        public ConversationActorDefinition Duplicate(string key)
        {
            ConversationActorDefinition duplicate = new()
            {
                _key = NormalizeKey(
                    string.IsNullOrWhiteSpace(key)
                        ? Key
                        : key),
                _displayName = _displayName,
                _portrait = _portrait,
                _themeColor = _themeColor,
                _notes = _notes,
            };

            duplicate.EnsureId();
            return duplicate;
        }

        /// <summary>
        /// Normalizes one authored actor key into the supported lowercase slug format.
        /// </summary>
        /// <param name="key">Candidate authored key.</param>
        /// <param name="fallbackToDefault">Whether empty input should collapse to the default key.</param>
        /// <returns>The normalized key using lowercase letters, digits, dashes, and underscores.</returns>
        public static string NormalizeKey(string key, bool fallbackToDefault = true)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallbackToDefault ? DefaultKey : string.Empty;
            }

            StringBuilder builder = new(key.Length);
            ReadOnlySpan<char> source = key.AsSpan();

            for (int index = 0; index < source.Length; index++)
            {
                char currentChar = source[index];

                if ((currentChar >= 'a' && currentChar <= 'z')
                    || (currentChar >= '0' && currentChar <= '9')
                    || currentChar == '_')
                {
                    builder.Append(currentChar);
                    continue;
                }

                if (currentChar >= 'A' && currentChar <= 'Z')
                {
                    builder.Append(char.ToLowerInvariant(currentChar));
                    continue;
                }

                if (currentChar != '-' && !char.IsWhiteSpace(currentChar))
                {
                    continue;
                }

                if (builder.Length == 0 || builder[builder.Length - 1] == '-')
                {
                    continue;
                }

                builder.Append('-');
            }

            if (builder.Length > 0)
            {
                return builder.ToString();
            }

            return fallbackToDefault ? DefaultKey : string.Empty;
        }

        /// <summary>
        /// Ensures the authored actor keeps a stable identifier and fallback key.
        /// </summary>
        public void EnsureId()
        {
            if (_actorId == SerializableGuid.Empty)
            {
                _actorId = SerializableGuid.NewGuid();
            }

            _key = NormalizeKey(_key, fallbackToDefault: false);

            _displayName ??= string.Empty;
            _notes ??= string.Empty;
        }
    }
}