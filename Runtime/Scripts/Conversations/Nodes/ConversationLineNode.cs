using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Identifies how one conversant should be mapped into the presenter-side layout.
    /// </summary>
    public enum ConversationParticipantSlot
    {
        /// <summary>
        /// Resolves the slot automatically from the participant role.
        /// Speakers default to the left and listeners default to the right.
        /// </summary>
        Auto,

        /// <summary>
        /// Pins the conversant to the left presenter slot.
        /// </summary>
        Left,

        /// <summary>
        /// Pins the conversant to the right presenter slot.
        /// </summary>
        Right,
    }

    /// <summary>
    /// Resolves authored participant-slot assignments into explicit presenter sides.
    /// </summary>
    public static class ConversationParticipantSlotUtility
    {
        /// <summary>
        /// Resolves one authored slot selection into one concrete presenter side.
        /// </summary>
        /// <param name="slot">Authored slot selection.</param>
        /// <param name="fallbackSlot">Role-based fallback side used by auto assignments.</param>
        /// <returns>The resolved concrete presenter side.</returns>
        public static ConversationParticipantSlot ResolveSlot(
            ConversationParticipantSlot slot,
            ConversationParticipantSlot fallbackSlot)
        {
            return slot == ConversationParticipantSlot.Auto
                ? fallbackSlot
                : slot;
        }
    }
}

namespace IndieGabo.HandyTools.ConversationsModule.Nodes
{
    /// <summary>
    /// Stores one authored spoken line inside a Conversations graph.
    /// </summary>
    [System.Serializable]
    [ConversationNodeMenu("Dialogue/Spoken Line", "Spoken Line")]
    public sealed class ConversationLineNode : ConversationNodeBase
    {
        private static readonly IReadOnlyList<GraphPortDefinition> _optionalNextOutputPorts =
            new[]
            {
                new GraphPortDefinition(
                    GraphPortKeys.Next,
                    GraphPortKeys.Next,
                    isMandatory: false),
            };

        [SerializeField]
        [TextArea(3, 8)]
        private string _lineText = "Hello, world.";

        [SerializeField]
        private SerializableGuid _speakerActorId;

        [SerializeField]
        private SerializableGuid _listenerActorId;

        [SerializeField]
        private ConversationParticipantSlot _speakerSlot = ConversationParticipantSlot.Auto;

        [SerializeField]
        private ConversationParticipantSlot _listenerSlot = ConversationParticipantSlot.Auto;

        /// <summary>
        /// Gets the authored literal spoken-line text.
        /// </summary>
        public string LineText => _lineText ?? string.Empty;

        /// <summary>
        /// Gets the required speaking conversant identifier.
        /// </summary>
        public SerializableGuid SpeakerActorId => _speakerActorId;

        /// <summary>
        /// Gets the optional listening conversant identifier.
        /// </summary>
        public SerializableGuid ListenerActorId => _listenerActorId;

        /// <summary>
        /// Gets the authored presenter-slot selection for the speaking conversant.
        /// </summary>
        public ConversationParticipantSlot SpeakerSlot => _speakerSlot;

        /// <summary>
        /// Gets the authored presenter-slot selection for the listening conversant.
        /// </summary>
        public ConversationParticipantSlot ListenerSlot => _listenerSlot;

        /// <summary>
        /// Gets the resolved presenter slot for the speaking conversant.
        /// </summary>
        public ConversationParticipantSlot ResolvedSpeakerSlot =>
            ConversationParticipantSlotUtility.ResolveSlot(
                _speakerSlot,
                ConversationParticipantSlot.Left);

        /// <summary>
        /// Gets the resolved presenter slot for the listening conversant.
        /// </summary>
        public ConversationParticipantSlot ResolvedListenerSlot =>
            ConversationParticipantSlotUtility.ResolveSlot(
                _listenerSlot,
                ConversationParticipantSlot.Right);

        /// <summary>
        /// Gets the declared output ports for the node.
        /// A dialogue line can terminate the conversation when no next connection exists.
        /// </summary>
        /// <returns>The optional next output declaration.</returns>
        public override IReadOnlyList<GraphPortDefinition> GetOutputPorts()
        {
            return _optionalNextOutputPorts;
        }

        /// <summary>
        /// Gets one short node summary for authoring surfaces.
        /// </summary>
        /// <returns>The current authored line summary.</returns>
        public override string GetSummary()
        {
            if (!string.IsNullOrWhiteSpace(_lineText))
            {
                return _lineText.Trim();
            }

            return "Empty line";
        }
    }
}