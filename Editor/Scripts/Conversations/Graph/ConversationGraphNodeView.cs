using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using IndieGabo.HandyTools.Editor.GraphCore;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Graph
{
    /// <summary>
    /// Renders one authored Conversations node inside the shared graph canvas shell.
    /// </summary>
    public sealed class ConversationGraphNodeView : GraphCanvasNodeView
    {
        private static readonly Color DefaultTitleColor = new(0.25f, 0.34f, 0.46f);
        private static readonly Color DefaultBorderColor = new(0.45f, 0.62f, 0.85f);
        private static readonly Color EntryTitleColor = new(0.19f, 0.39f, 0.24f);
        private static readonly Color EntryBorderColor = new(0.39f, 0.77f, 0.48f);
        private static readonly Color EndTitleColor = new(0.42f, 0.22f, 0.22f);
        private static readonly Color EndBorderColor = new(0.84f, 0.42f, 0.42f);

        /// <summary>
        /// Initializes one Conversations node view.
        /// </summary>
        /// <param name="node">Authored node represented by the view.</param>
        /// <param name="edgeConnectorListener">Shared edge connector listener.</param>
        public ConversationGraphNodeView(
            ConversationNodeBase node,
            IEdgeConnectorListener edgeConnectorListener)
            : base(CreateConfiguration(node), edgeConnectorListener)
        {
            Node = node;
        }

        /// <summary>
        /// Gets the authored node represented by the view.
        /// </summary>
        public ConversationNodeBase Node { get; }

        private static Configuration CreateConfiguration(ConversationNodeBase node)
        {
            IReadOnlyList<OutputPortDefinition> outputPorts = node?.GetOutputPorts()
                ?.Select(port => new OutputPortDefinition(port.Key, port.DisplayName))
                .ToList()
                ?? GraphPortDefinition.None
                    .Select(port => new OutputPortDefinition(port.Key, port.DisplayName))
                    .ToList();

            ResolvePalette(node, out Color titleColor, out Color borderColor);

            return new Configuration
            {
                NodeId = node?.Id ?? default,
                ViewDataKey = node?.Id.ToHexString() ?? string.Empty,
                HasInputPort = node?.HasInputPort ?? false,
                OutputPortDefinitionsProvider = () => outputPorts,
                DisplayTitleProvider = () => node?.DisplayTitle ?? string.Empty,
                SummaryProvider = () => node?.GetSummary() ?? string.Empty,
                PositionProvider = () => node?.Position ?? Vector2.zero,
                AuthoringTitleColor = titleColor,
                AuthoringBorderColor = borderColor,
            };
        }

        private static void ResolvePalette(
            ConversationNodeBase node,
            out Color titleColor,
            out Color borderColor)
        {
            if (node != null && !node.HasInputPort)
            {
                titleColor = EntryTitleColor;
                borderColor = EntryBorderColor;
                return;
            }

            if (node?.GetOutputPorts()?.Count <= 0)
            {
                titleColor = EndTitleColor;
                borderColor = EndBorderColor;
                return;
            }

            titleColor = DefaultTitleColor;
            borderColor = DefaultBorderColor;
        }
    }
}