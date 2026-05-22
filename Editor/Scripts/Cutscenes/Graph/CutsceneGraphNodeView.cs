using System;
using System.Linq;
using IndieGabo.HandyTools.Editor.GraphCore;
using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Graph
{
    public sealed class CutsceneGraphNodeView : GraphCanvasNodeView
    {
        private readonly CutsceneNodePresentationMetadata _presentation;

        internal CutsceneGraphNodeView(
            CutsceneNodeBase node,
            IEdgeConnectorListener edgeConnectorListener)
            : base(CreateConfiguration(node), edgeConnectorListener)
        {
            Node = node;
            _presentation = CutsceneNodePresentationRegistry.GetMetadata(node);
        }

        public CutsceneNodeBase Node { get; }

        public void SetRuntimeState(
            bool isCurrent,
            bool wasSuccessful,
            bool wasFailed,
            bool wasCancelled,
            bool wasVisited)
        {
            if (!Node.UsesRuntimeStateStyling)
            {
                base.ApplyAuthoringPalette();
                return;
            }

            Color titleColor = _presentation.TitleColor;
            Color borderColor = _presentation.BorderColor;

            if (isCurrent)
            {
                titleColor = new Color(0.16f, 0.32f, 0.68f);
                borderColor = new Color(0.37f, 0.60f, 0.92f);
            }
            else if (wasFailed)
            {
                titleColor = new Color(0.56f, 0.18f, 0.18f);
                borderColor = new Color(0.83f, 0.36f, 0.36f);
            }
            else if (wasCancelled)
            {
                titleColor = new Color(0.48f, 0.30f, 0.10f);
                borderColor = new Color(0.78f, 0.58f, 0.28f);
            }
            else if (wasSuccessful)
            {
                titleColor = new Color(0.16f, 0.43f, 0.21f);
                borderColor = new Color(0.42f, 0.78f, 0.48f);
            }
            else if (wasVisited)
            {
                titleColor = new Color(0.24f, 0.28f, 0.18f);
                borderColor = new Color(0.53f, 0.61f, 0.36f);
            }

            ApplyPalette(titleColor, borderColor);
        }
        private static Configuration CreateConfiguration(CutsceneNodeBase node)
        {
            CutsceneNodePresentationMetadata presentation =
                CutsceneNodePresentationRegistry.GetMetadata(node);

            return new Configuration
            {
                NodeId = node.Id,
                ViewDataKey = node.Id.ToHexString(),
                HasInputPort = node.HasInputPort,
                DisplayTitleProvider = () => node.DisplayTitle,
                SummaryProvider = () => node.GetSummary(),
                PositionProvider = () => node.Position,
                OutputPortDefinitionsProvider = () => node.GetOutputPorts()
                    .Select(port => new OutputPortDefinition(port.Key, port.DisplayName))
                    .ToList(),
                TitleIcon = presentation.Icon,
                AuthoringTitleColor = presentation.TitleColor,
                AuthoringBorderColor = presentation.BorderColor,
            };
        }
    }
}