using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.FSMModule
{
    /// <summary>
    /// Represents a single node in the FSM state inheritance graph.
    /// </summary>
    public class StateNode : Node
    {
        /// <summary>
        /// Defines the visual role of a node inside the graph.
        /// </summary>
        public enum NodeKind
        {
            Owner,
            Neutral,
            Path,
            Current
        }

        private const float CornerRadius = 18f;

        private readonly Port _inputPort;
        private readonly Port _outputPort;

        /// <summary>
        /// Gets the input port used by incoming edges.
        /// </summary>
        public Port InputPort => _inputPort;

        /// <summary>
        /// Gets the output port used by outgoing edges.
        /// </summary>
        public Port OutputPort => _outputPort;

        /// <summary>
        /// Creates a visual node with the requested styling and connectivity.
        /// </summary>
        /// <param name="title">The text shown inside the node.</param>
        /// <param name="nodeKind">The visual role assigned to the node.</param>
        /// <param name="canReceive">Whether the node should expose an input port.</param>
        /// <param name="canSend">Whether the node should expose an output port.</param>
        /// <param name="width">The node width in pixels.</param>
        public StateNode(
            string title,
            NodeKind nodeKind,
            bool canReceive,
            bool canSend,
            float width)
        {
            this.title = title;

            style.width = width;
            style.height = 66f;

            if (canReceive)
            {
                _inputPort = InstantiatePort(
                    Orientation.Vertical,
                    Direction.Input,
                    Port.Capacity.Multi,
                    typeof(bool));

                ConfigurePort(_inputPort);
                AttachPort(_inputPort, true);
            }

            if (canSend)
            {
                _outputPort = InstantiatePort(
                    Orientation.Vertical,
                    Direction.Output,
                    Port.Capacity.Multi,
                    typeof(bool));

                ConfigurePort(_outputPort);
                AttachPort(_outputPort, false);
            }

            ApplyNodeStyle(nodeKind);

            RefreshExpandedState();
            RefreshPorts();
        }

        /// <summary>
        /// Keeps the graph ports invisible while preserving valid edge anchors.
        /// </summary>
        /// <param name="port">The port being configured.</param>
        private static void ConfigurePort(Port port)
        {
            if (port == null)
                return;

            port.portName = string.Empty;
            port.pickingMode = PickingMode.Ignore;
            port.style.opacity = 0f;
            port.style.position = Position.Absolute;
            port.style.width = 8f;
            port.style.height = 8f;
            port.style.marginTop = 0f;
            port.style.marginBottom = 0f;
            port.style.marginLeft = 0f;
            port.style.marginRight = 0f;
            port.style.left = new Length(50f, LengthUnit.Percent);
            port.style.marginLeft = -4f;
        }

        /// <summary>
        /// Attaches a hidden port to the visual top or bottom center of the node.
        /// </summary>
        /// <param name="port">The port being attached.</param>
        /// <param name="attachToTop">True to anchor at the top center; otherwise bottom center.</param>
        private void AttachPort(Port port, bool attachToTop)
        {
            if (port == null)
                return;

            if (attachToTop)
            {
                port.style.top = 0f;
            }
            else
            {
                port.style.bottom = 0f;
            }

            mainContainer.Add(port);
        }

        /// <summary>
        /// Applies the node visuals required by the visualizer mockup.
        /// </summary>
        /// <param name="nodeKind">The role currently displayed by the node.</param>
        private void ApplyNodeStyle(NodeKind nodeKind)
        {
            Color backgroundColor = new Color32(46, 46, 46, 255);
            Color borderColor = new Color32(68, 68, 68, 255);
            float borderWidth = 1f;

            switch (nodeKind)
            {
                case NodeKind.Owner:
                    borderWidth = 0f;
                    break;
                case NodeKind.Path:
                    borderColor = new Color(0.74f, 0.42f, 0.92f);
                    borderWidth = 2f;
                    break;
                case NodeKind.Current:
                    backgroundColor = new Color(0.69f, 0.39f, 0.93f);
                    borderColor = backgroundColor;
                    borderWidth = 0f;
                    break;
            }

            style.backgroundColor = Color.clear;
            style.borderLeftWidth = 0f;
            style.borderRightWidth = 0f;
            style.borderTopWidth = 0f;
            style.borderBottomWidth = 0f;

            mainContainer.style.backgroundColor = backgroundColor;
            mainContainer.style.borderLeftColor = borderColor;
            mainContainer.style.borderRightColor = borderColor;
            mainContainer.style.borderTopColor = borderColor;
            mainContainer.style.borderBottomColor = borderColor;
            mainContainer.style.borderLeftWidth = borderWidth;
            mainContainer.style.borderRightWidth = borderWidth;
            mainContainer.style.borderTopWidth = borderWidth;
            mainContainer.style.borderBottomWidth = borderWidth;
            mainContainer.style.borderTopLeftRadius = CornerRadius;
            mainContainer.style.borderTopRightRadius = CornerRadius;
            mainContainer.style.borderBottomLeftRadius = CornerRadius;
            mainContainer.style.borderBottomRightRadius = CornerRadius;
            mainContainer.style.unityOverflowClipBox = OverflowClipBox.ContentBox;
            mainContainer.style.position = Position.Relative;

            titleContainer.style.flexGrow = 1f;
            titleContainer.style.justifyContent = Justify.Center;
            titleContainer.style.alignItems = Align.Center;
            titleContainer.style.paddingLeft = 12f;
            titleContainer.style.paddingRight = 12f;
            titleContainer.style.paddingTop = 8f;
            titleContainer.style.paddingBottom = 8f;
            titleContainer.style.backgroundColor = Color.clear;

            if (titleContainer.Q<Label>() is Label label)
            {
                label.style.color = Color.white;
                label.style.fontSize = nodeKind == NodeKind.Owner ? 19f : 16f;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.flexGrow = 1f;
            }

            extensionContainer.style.display = DisplayStyle.None;
        }
    }
}