using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Stores the authored table and stable conversation identifier required to resolve one
    /// conversation selection at runtime.
    /// </summary>
    [System.Serializable]
    public sealed class ConversationReference
    {
        #region Fields

        [SerializeField]
        private ConversationTable _table;

        [SerializeField]
        private SerializableGuid _conversationId;

        [SerializeField]
        private string _conversationTitle = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the authored table that owns the referenced conversation.
        /// </summary>
        public ConversationTable Table => _table;

        /// <summary>
        /// Gets the stable authored conversation identifier.
        /// </summary>
        public SerializableGuid ConversationId => _conversationId;

        /// <summary>
        /// Gets the last known authored conversation title path.
        /// </summary>
        public string ConversationTitle => _conversationTitle?.Trim() ?? string.Empty;

        #endregion

        #region Public API

        /// <summary>
        /// Stores one authored conversation selection.
        /// </summary>
        /// <param name="table">Table that owns the selected conversation.</param>
        /// <param name="conversation">Selected authored conversation.</param>
        public void SetSelection(
            ConversationTable table,
            ConversationDefinition conversation)
        {
            SetSelection(
                table,
                conversation?.ConversationId ?? SerializableGuid.Empty,
                conversation?.Title ?? string.Empty);
        }

        /// <summary>
        /// Stores one conversation selection from its authored table, stable identifier, and
        /// fallback title path.
        /// </summary>
        /// <param name="table">Table that owns the selected conversation.</param>
        /// <param name="conversationId">Stable conversation identifier.</param>
        /// <param name="conversationTitle">Fallback authored title path.</param>
        public void SetSelection(
            ConversationTable table,
            SerializableGuid conversationId,
            string conversationTitle)
        {
            _table = table;
            _conversationId = conversationId;
            _conversationTitle = conversationTitle?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Copies one authored conversation selection into this reference.
        /// </summary>
        /// <param name="reference">Source selection that should be copied.</param>
        public void CopyFrom(ConversationReference reference)
        {
            if (reference == null)
            {
                Clear();
                return;
            }

            SetSelection(
                reference.Table,
                reference.ConversationId,
                reference.ConversationTitle);
        }

        /// <summary>
        /// Clears the stored conversation selection.
        /// </summary>
        public void Clear()
        {
            _table = null;
            _conversationId = SerializableGuid.Empty;
            _conversationTitle = string.Empty;
        }

        #endregion
    }
}