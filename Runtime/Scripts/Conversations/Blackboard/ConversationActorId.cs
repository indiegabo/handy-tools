using System;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.Blackboard
{
    /// <summary>
    /// Stores one stable authored actor identifier used by Conversations graphs.
    /// </summary>
    [Serializable]
    public struct ConversationActorId
    {
        [SerializeField]
        private string _value;

        /// <summary>
        /// Initializes one actor identifier value.
        /// </summary>
        /// <param name="value">Authored actor identifier text.</param>
        public ConversationActorId(string value)
        {
            _value = value ?? string.Empty;
        }

        /// <summary>
        /// Gets the authored actor identifier text.
        /// </summary>
        public string Value => _value ?? string.Empty;

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }
    }
}