using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.RuntimeData
{
    /// <summary>
    /// Stores one optional alternate-localization overlay payload for a single conversation.
    /// </summary>
    [Serializable]
    public sealed class ConversationLocalizationData
    {
        [SerializeField]
        private SerializableGuid _conversationId;

        [SerializeField]
        private string _locale = string.Empty;

        [SerializeField]
        private List<ConversationLocalizedTextEntryData> _entries = new();

        /// <summary>
        /// Initializes an empty localization overlay DTO.
        /// </summary>
        public ConversationLocalizationData()
        {
        }

        /// <summary>
        /// Initializes one localization overlay DTO.
        /// </summary>
        /// <param name="conversationId">Conversation that owns the localized entries.</param>
        /// <param name="locale">Locale covered by the overlay.</param>
        /// <param name="entries">Localized text entries keyed by authored text id.</param>
        public ConversationLocalizationData(
            SerializableGuid conversationId,
            string locale,
            IEnumerable<ConversationLocalizedTextEntryData> entries)
        {
            _conversationId = conversationId;
            _locale = locale ?? string.Empty;
            _entries = entries == null
                ? new List<ConversationLocalizedTextEntryData>()
                : new List<ConversationLocalizedTextEntryData>(entries);
        }

        /// <summary>
        /// Gets the conversation id covered by the localization overlay.
        /// </summary>
        public SerializableGuid ConversationId => _conversationId;

        /// <summary>
        /// Gets the locale covered by the localization overlay.
        /// </summary>
        public string Locale => _locale ?? string.Empty;

        /// <summary>
        /// Gets the localized text entries stored by the overlay.
        /// </summary>
        public IReadOnlyList<ConversationLocalizedTextEntryData> Entries => _entries;
    }

    /// <summary>
    /// Stores one localized text entry keyed by one authored conversation text id.
    /// </summary>
    [Serializable]
    public sealed class ConversationLocalizedTextEntryData
    {
        [SerializeField]
        private string _textId = string.Empty;

        [SerializeField]
        [TextArea(2, 6)]
        private string _text = string.Empty;

        /// <summary>
        /// Initializes an empty localized text entry DTO.
        /// </summary>
        public ConversationLocalizedTextEntryData()
        {
        }

        /// <summary>
        /// Initializes one localized text entry DTO.
        /// </summary>
        /// <param name="textId">Authored text id used as the lookup key.</param>
        /// <param name="text">Localized text value.</param>
        public ConversationLocalizedTextEntryData(string textId, string text)
        {
            _textId = textId ?? string.Empty;
            _text = text ?? string.Empty;
        }

        /// <summary>
        /// Gets the authored text id used as the lookup key.
        /// </summary>
        public string TextId => _textId ?? string.Empty;

        /// <summary>
        /// Gets the localized text value.
        /// </summary>
        public string Text => _text ?? string.Empty;
    }
}