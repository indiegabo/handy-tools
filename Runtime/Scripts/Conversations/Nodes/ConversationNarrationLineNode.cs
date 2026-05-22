using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.GraphCore;
using UnityEngine;

namespace IndieGabo.HandyTools.ConversationsModule.Nodes
{
    /// <summary>
    /// Stores one authored narration line that presents text without character-bound UI.
    /// </summary>
    [System.Serializable]
    [ConversationNodeMenu("Dialogue/Narration Line", "Narration Line")]
    public sealed class ConversationNarrationLineNode : ConversationNodeBase
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
        private string _lineText = "Narration text.";

        /// <summary>
        /// Gets the authored literal narration text.
        /// </summary>
        public string LineText => _lineText ?? string.Empty;

        /// <summary>
        /// Gets the declared output ports for the node.
        /// A narration line can terminate the conversation when no next connection exists.
        /// </summary>
        /// <returns>The optional next output declaration.</returns>
        public override IReadOnlyList<GraphPortDefinition> GetOutputPorts()
        {
            return _optionalNextOutputPorts;
        }

        /// <summary>
        /// Gets one short node summary for authoring surfaces.
        /// </summary>
        /// <returns>The current authored narration summary.</returns>
        public override string GetSummary()
        {
            return string.IsNullOrWhiteSpace(_lineText)
                ? "Empty narration"
                : _lineText.Trim();
        }
    }
}