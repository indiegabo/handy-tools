using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Utils;
using GraphViewDirection = UnityEditor.Experimental.GraphView.Direction;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Provides one reusable GraphView node shell with shared port layout,
    /// summary rendering, and connection-disconnect context actions.
    /// </summary>
    public abstract class GraphCanvasNodeView : Node, IGraphCanvasNodeView
    {
        /// <summary>
        /// Configures one reusable graph canvas node shell.
        /// </summary>
        public sealed class Configuration
        {
            /// <summary>
            /// Gets or sets the stable authored node identifier.
            /// </summary>
            public SerializableGuid NodeId { get; set; }

            /// <summary>
            /// Gets or sets the stable view data key used by GraphView state.
            /// </summary>
            public string ViewDataKey { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the authored width applied to the node shell.
            /// </summary>
            public float Width { get; set; } = 240f;

            /// <summary>
            /// Gets or sets whether the node should render one input port.
            /// </summary>
            public bool HasInputPort { get; set; } = true;

            /// <summary>
            /// Gets or sets the input port capacity used when the node has one.
            /// </summary>
            public Port.Capacity InputPortCapacity { get; set; } = Port.Capacity.Multi;

            /// <summary>
            /// Gets or sets the delegate that resolves the authored output ports.
            /// </summary>
            public Func<IReadOnlyList<OutputPortDefinition>> OutputPortDefinitionsProvider
            { get; set; }

            /// <summary>
            /// Gets or sets the delegate that resolves the displayed node title.
            /// </summary>
            public Func<string> DisplayTitleProvider { get; set; }

            /// <summary>
            /// Gets or sets the delegate that resolves the summary text.
            /// </summary>
            public Func<string> SummaryProvider { get; set; }

            /// <summary>
            /// Gets or sets the delegate that resolves the authored node position.
            /// </summary>
            public Func<Vector2> PositionProvider { get; set; }

            /// <summary>
            /// Gets or sets the icon displayed in the title strip.
            /// </summary>
            public Texture TitleIcon { get; set; }

            /// <summary>
            /// Gets or sets the authored title-strip color.
            /// </summary>
            public Color AuthoringTitleColor { get; set; }

            /// <summary>
            /// Gets or sets the authored border color.
            /// </summary>
            public Color AuthoringBorderColor { get; set; }

            /// <summary>
            /// Gets or sets the fallback text displayed when the summary is blank.
            /// </summary>
            public string EmptySummaryText { get; set; } = "No summary.";
        }

        /// <summary>
        /// Describes one output port rendered by the node shell.
        /// </summary>
        public readonly struct OutputPortDefinition
        {
            /// <summary>
            /// Initializes one output port definition.
            /// </summary>
            /// <param name="key">Stable authored output key.</param>
            /// <param name="displayName">Human-readable port label.</param>
            /// <param name="capacity">Port connection capacity.</param>
            public OutputPortDefinition(
                string key,
                string displayName,
                Port.Capacity capacity = Port.Capacity.Single)
            {
                Key = key ?? string.Empty;
                DisplayName = string.IsNullOrWhiteSpace(displayName)
                    ? Key
                    : displayName;
                Capacity = capacity;
            }

            /// <summary>
            /// Gets the stable authored output key.
            /// </summary>
            public string Key { get; }

            /// <summary>
            /// Gets the human-readable port label.
            /// </summary>
            public string DisplayName { get; }

            /// <summary>
            /// Gets the port connection capacity.
            /// </summary>
            public Port.Capacity Capacity { get; }
        }

        private const float DefaultNodeHeight = 120f;

        private readonly Configuration _configuration;
        private readonly IEdgeConnectorListener _edgeConnectorListener;
        private readonly Label _summaryLabel;
        private readonly Dictionary<string, Port> _outputPorts =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes one reusable graph canvas node shell.
        /// </summary>
        /// <param name="configuration">Node shell configuration.</param>
        /// <param name="edgeConnectorListener">Shared edge connector listener.</param>
        protected GraphCanvasNodeView(
            Configuration configuration,
            IEdgeConnectorListener edgeConnectorListener)
        {
            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));
            _edgeConnectorListener = edgeConnectorListener
                ?? throw new ArgumentNullException(nameof(edgeConnectorListener));

            viewDataKey = configuration.ViewDataKey ?? string.Empty;
            style.width = configuration.Width > 0f ? configuration.Width : 240f;

            BuildPorts();
            ApplyBaseStyle();

            Image titleIcon = CreateTitleIcon(configuration.TitleIcon);

            if (titleIcon != null)
            {
                titleContainer.Insert(0, titleIcon);
            }

            _summaryLabel = new Label();
            _summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            _summaryLabel.style.marginTop = 4f;
            _summaryLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            extensionContainer.Add(_summaryLabel);

            ConfigureHorizontalLayout();
            RefreshPresentation();
        }

        /// <summary>
        /// Gets the stable authored node identifier represented by the view.
        /// </summary>
        public SerializableGuid NodeId => _configuration.NodeId;

        /// <summary>
        /// Raised when the GraphView selection state for the node changes.
        /// </summary>
        public event Action SelectionStateChanged;

        /// <summary>
        /// Gets the rendered input port when the node exposes one.
        /// </summary>
        public Port InputPort { get; private set; }

        /// <summary>
        /// Gets the authored graph position represented by the view.
        /// </summary>
        public Vector2 AuthoredPosition => _configuration.PositionProvider?.Invoke() ?? Vector2.zero;

        /// <summary>
        /// Gets the rendered output ports keyed by authored output id.
        /// </summary>
        public IReadOnlyDictionary<string, Port> OutputPorts => _outputPorts;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override void OnSelected()
        {
            base.OnSelected();
            SelectionStateChanged?.Invoke();
        }

        /// <inheritdoc />
        public override void OnUnselected()
        {
            base.OnUnselected();
            SelectionStateChanged?.Invoke();
        }

        /// <summary>
        /// Refreshes the shared node shell from the current authored values.
        /// </summary>
        public void RefreshPresentation()
        {
            title = _configuration.DisplayTitleProvider?.Invoke() ?? string.Empty;

            string summary = _configuration.SummaryProvider?.Invoke();
            _summaryLabel.text = string.IsNullOrWhiteSpace(summary)
                ? _configuration.EmptySummaryText
                : summary;
            _summaryLabel.style.color = new Color(0.86f, 0.86f, 0.86f);

            Vector2 size = GetPosition().size == Vector2.zero
                ? new Vector2(style.width.value.value, DefaultNodeHeight)
                : GetPosition().size;
            Vector2 position = _configuration.PositionProvider?.Invoke() ?? Vector2.zero;
            SetPosition(new Rect(position, size));

            RefreshExpandedState();
            RefreshPorts();
            ApplyAuthoringPalette();
        }

        /// <summary>
        /// Reapplies the authored palette configured for the node shell.
        /// </summary>
        protected void ApplyAuthoringPalette()
        {
            ApplyPalette(
                _configuration.AuthoringTitleColor,
                _configuration.AuthoringBorderColor);
        }

        /// <summary>
        /// Applies one title-strip and border palette to the node shell.
        /// </summary>
        /// <param name="titleColor">Title-strip color.</param>
        /// <param name="borderColor">Border color.</param>
        protected void ApplyPalette(Color titleColor, Color borderColor)
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

        private void BuildPorts()
        {
            if (_configuration.HasInputPort)
            {
                InputPort = GraphCanvasPort.Create(
                    GraphViewDirection.Input,
                    _configuration.InputPortCapacity,
                    _edgeConnectorListener);
                InputPort.portName = "In";
                ConfigureHorizontalPort(InputPort);
                inputContainer.Add(InputPort);
            }

            IReadOnlyList<OutputPortDefinition> outputPorts =
                _configuration.OutputPortDefinitionsProvider?.Invoke()
                ?? Array.Empty<OutputPortDefinition>();

            for (int index = 0; index < outputPorts.Count; index++)
            {
                OutputPortDefinition outputPort = outputPorts[index];
                Port port = GraphCanvasPort.Create(
                    GraphViewDirection.Output,
                    outputPort.Capacity,
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
            Color extensionBackgroundColor = new(0.21f, 0.21f, 0.21f);
            Color portBackgroundColor = new(0.23f, 0.23f, 0.23f);

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

        private static Image CreateTitleIcon(Texture titleIcon)
        {
            if (titleIcon == null)
            {
                return null;
            }

            Image image = new()
            {
                image = titleIcon,
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

        private sealed class GraphCanvasPort : Port
        {
            private GraphCanvasPort(
                Orientation orientation,
                GraphViewDirection direction,
                Capacity capacity,
                Type type)
                : base(orientation, direction, capacity, type)
            {
            }

            public static GraphCanvasPort Create(
                GraphViewDirection direction,
                Capacity capacity,
                IEdgeConnectorListener edgeConnectorListener)
            {
                GraphCanvasPort port = new(
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