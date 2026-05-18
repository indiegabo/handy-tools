using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Graph
{
    public sealed class CutsceneGraphNodeView : Node
    {
        private const float DefaultNodeWidth = 240f;

        private readonly IEdgeConnectorListener _edgeConnectorListener;
        private readonly Label _summaryLabel;
        private readonly CutsceneNodePresentationMetadata _presentation;
        private readonly Dictionary<string, Port> _outputPorts =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Image _titleIcon;

        internal CutsceneGraphNodeView(
            CutsceneNodeBase node,
            IEdgeConnectorListener edgeConnectorListener)
        {
            Node = node;
            _edgeConnectorListener = edgeConnectorListener;
            _presentation = CutsceneNodePresentationRegistry.GetMetadata(node);
            viewDataKey = node.Id.ToHexString();
            style.width = DefaultNodeWidth;

            BuildPorts();
            ApplyBaseStyle();

            _titleIcon = CreateTitleIcon();

            if (_titleIcon != null)
            {
                titleContainer.Insert(0, _titleIcon);
            }

            _summaryLabel = new Label();
            _summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            _summaryLabel.style.marginTop = 4f;
            _summaryLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            extensionContainer.Add(_summaryLabel);

            ConfigureHorizontalLayout();

            RefreshPresentation();
        }

        public CutsceneNodeBase Node { get; }

        public Port InputPort { get; private set; }

        public IReadOnlyDictionary<string, Port> OutputPorts => _outputPorts;

        internal event Action SelectionStateChanged;

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            List<Edge> incomingEdges = InputPort?.connections?.ToList() ?? new List<Edge>();
            List<Edge> outgoingEdges = _outputPorts.Values
                .SelectMany(port => port.connections)
                .Distinct()
                .ToList();

            List<Edge> allEdges = incomingEdges
                .Concat(outgoingEdges)
                .Distinct()
                .ToList();

            evt.menu.AppendSeparator();
            AppendDisconnectAction(evt, "Disconnect Incoming", incomingEdges);
            AppendDisconnectAction(evt, "Disconnect Outgoing", outgoingEdges);
            AppendDisconnectAction(evt, "Disconnect All Connections", allEdges);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            SelectionStateChanged?.Invoke();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            SelectionStateChanged?.Invoke();
        }

        private void BuildPorts()
        {
            IReadOnlyList<CutsceneNodePort> outputPorts = Node.GetOutputPorts();

            if (Node.HasInputPort)
            {
                InputPort = CutsceneGraphPort.Create(
                    Direction.Input,
                    Port.Capacity.Multi,
                    _edgeConnectorListener);
                InputPort.portName = "In";
                ConfigureHorizontalPort(InputPort);
                inputContainer.Add(InputPort);
            }

            for (int index = 0; index < outputPorts.Count; index++)
            {
                CutsceneNodePort outputPort = outputPorts[index];
                Port port = CutsceneGraphPort.Create(
                    Direction.Output,
                    Port.Capacity.Single,
                    _edgeConnectorListener);
                port.portName = outputPort.DisplayName;
                port.userData = outputPort.Key;
                ConfigureHorizontalPort(port);
                outputContainer.Add(port);
                _outputPorts[outputPort.Key] = port;
            }
        }

        private void ApplyBaseStyle()
        {
            Color extensionBackgroundColor = new Color(0.21f, 0.21f, 0.21f);
            Color portBackgroundColor = new Color(0.23f, 0.23f, 0.23f);

            mainContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            mainContainer.style.position = Position.Relative;

            titleContainer.style.paddingLeft = 8f;
            titleContainer.style.paddingRight = 8f;
            titleContainer.style.alignItems = Align.Center;

            extensionContainer.style.backgroundColor = extensionBackgroundColor;
            extensionContainer.style.paddingLeft = 8f;
            extensionContainer.style.paddingRight = 8f;
            extensionContainer.style.paddingTop = 4f;
            extensionContainer.style.paddingBottom = 6f;

            inputContainer.style.paddingLeft = 8f;
            inputContainer.style.paddingRight = 8f;
            inputContainer.style.paddingTop = 2f;
            inputContainer.style.paddingBottom = 2f;
            inputContainer.style.backgroundColor = portBackgroundColor;

            outputContainer.style.paddingLeft = 8f;
            outputContainer.style.paddingRight = 8f;
            outputContainer.style.paddingTop = 2f;
            outputContainer.style.paddingBottom = 2f;
            outputContainer.style.backgroundColor = portBackgroundColor;
        }

        private Image CreateTitleIcon()
        {
            if (_presentation.Icon == null)
            {
                return null;
            }

            Image image = new()
            {
                image = _presentation.Icon,
                scaleMode = ScaleMode.ScaleToFit,
            };

            image.style.width = 14f;
            image.style.height = 14f;
            image.style.minWidth = 14f;
            image.style.marginRight = 4f;
            image.style.flexShrink = 0f;

            return image;
        }

        private void ConfigureHorizontalLayout()
        {
            inputContainer.style.display = InputPort == null
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            outputContainer.style.display = _outputPorts.Count == 0
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        private static void ConfigureHorizontalPort(Port port)
        {
            if (port == null)
            {
                return;
            }

            port.style.marginTop = 0f;
            port.style.marginBottom = 0f;

            if (port.Q<Label>() is Label label)
            {
                label.style.color = new Color(0.86f, 0.86f, 0.86f);
            }
        }

        private void AppendDisconnectAction(
            ContextualMenuPopulateEvent evt,
            string label,
            List<Edge> edges)
        {
            if (evt == null)
            {
                return;
            }

            if (edges == null || edges.Count == 0)
            {
                evt.menu.AppendAction(label, _ => { }, _ => DropdownMenuAction.Status.Disabled);
                return;
            }

            evt.menu.AppendAction(
                label,
                _ => DisconnectEdges(edges),
                _ => DropdownMenuAction.Status.Normal);
        }

        private void DisconnectEdges(List<Edge> edges)
        {
            GraphView graphView = GetFirstAncestorOfType<GraphView>();

            if (graphView == null || edges == null || edges.Count == 0)
            {
                return;
            }

            graphView.DeleteElements(edges.Distinct().Cast<GraphElement>().ToList());
        }

        public void RefreshPresentation()
        {
            title = Node.DisplayTitle;
            _summaryLabel.text = string.IsNullOrWhiteSpace(Node.GetSummary())
                ? "No summary."
                : Node.GetSummary();
            _summaryLabel.style.color = new Color(0.86f, 0.86f, 0.86f);

            Vector2 size = GetPosition().size == Vector2.zero
                ? new Vector2(DefaultNodeWidth, 120f)
                : GetPosition().size;
            SetPosition(new Rect(Node.Position, size));

            RefreshExpandedState();
            RefreshPorts();
            ApplyAuthoringPalette();
        }

        public void SetRuntimeState(
            bool isCurrent,
            bool wasSuccessful,
            bool wasFailed,
            bool wasCancelled,
            bool wasVisited)
        {
            if (!Node.UsesRuntimeStateStyling)
            {
                ApplyAuthoringPalette();
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

        private void ApplyAuthoringPalette()
        {
            ApplyPalette(_presentation.TitleColor, _presentation.BorderColor);
        }

        private void ApplyPalette(Color titleColor, Color borderColor)
        {
            titleContainer.style.backgroundColor = titleColor;
            mainContainer.style.borderLeftWidth = 2f;
            mainContainer.style.borderRightWidth = 2f;
            mainContainer.style.borderTopWidth = 2f;
            mainContainer.style.borderBottomWidth = 2f;
            mainContainer.style.borderLeftColor = borderColor;
            mainContainer.style.borderRightColor = borderColor;
            mainContainer.style.borderTopColor = borderColor;
            mainContainer.style.borderBottomColor = borderColor;
        }

        private sealed class CutsceneGraphPort : Port
        {
            private CutsceneGraphPort(
                Orientation orientation,
                Direction direction,
                Capacity capacity,
                Type type)
                : base(orientation, direction, capacity, type)
            {
            }

            public static CutsceneGraphPort Create(
                Direction direction,
                Capacity capacity,
                IEdgeConnectorListener edgeConnectorListener)
            {
                CutsceneGraphPort port = new(
                    Orientation.Horizontal,
                    direction,
                    capacity,
                    typeof(bool))
                {
                    m_EdgeConnector = new EdgeConnector<Edge>(edgeConnectorListener),
                };

                port.AddManipulator(port.m_EdgeConnector);
                return port;
            }
        }
    }
}