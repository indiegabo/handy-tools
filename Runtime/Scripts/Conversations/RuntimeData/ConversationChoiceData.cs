using System;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.RuntimeData
{
    /// <summary>
    /// Stores one exported choice option belonging to one runtime conversation node.
    /// </summary>
    [Serializable]
    public sealed class ConversationChoiceData
    {
        [SerializeField]
        private SerializableGuid _choiceId;

        [SerializeField]
        private SerializableGuid _ownerNodeId;

        [SerializeField]
        private SerializableGuid _nextNodeId;

        [SerializeField]
        private ConversationStringValueData _text = ConversationStringValueData.CreateDirect(
            string.Empty);

        /// <summary>
        /// Initializes an empty choice DTO.
        /// </summary>
        public ConversationChoiceData()
        {
        }

        /// <summary>
        /// Initializes one exported choice DTO.
        /// </summary>
        /// <param name="choiceId">Stable choice identifier.</param>
        /// <param name="ownerNodeId">Node that owns the choice option.</param>
        /// <param name="nextNodeId">Target node selected by the choice.</param>
        /// <param name="text">Display text payload for the option.</param>
        public ConversationChoiceData(
            SerializableGuid choiceId,
            SerializableGuid ownerNodeId,
            SerializableGuid nextNodeId,
            ConversationStringValueData text)
        {
            _choiceId = choiceId;
            _ownerNodeId = ownerNodeId;
            _nextNodeId = nextNodeId;
            _text = text ?? ConversationStringValueData.CreateDirect(string.Empty);
        }

        /// <summary>
        /// Gets the stable identifier of the exported option.
        /// </summary>
        public SerializableGuid ChoiceId => _choiceId;

        /// <summary>
        /// Gets the stable identifier of the owning runtime node.
        /// </summary>
        public SerializableGuid OwnerNodeId => _ownerNodeId;

        /// <summary>
        /// Gets the downstream node selected by this option.
        /// </summary>
        public SerializableGuid NextNodeId => _nextNodeId;

        /// <summary>
        /// Gets the exported text source used by the option label.
        /// </summary>
        public ConversationStringValueData Text => _text;
    }
}