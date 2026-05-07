using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.FSMModule
{
    /// <summary>
    /// Graph view used by the state visualizer window.
    /// </summary>
    public class StatesGraphView : GraphView
    {
        #region Fields

        private readonly ContentDragger _contentDragger;
        private readonly List<Edge> _connectionEdges = new();

        #endregion

        #region Types

        /// <summary>
        /// Immutable data describing a single rendered connection.
        /// </summary>
        public readonly struct ConnectionData
        {
            /// <summary>
            /// Creates a new connection data payload.
            /// </summary>
            /// <param name="fromPort">The source node output port.</param>
            /// <param name="toPort">The target node input port.</param>
            /// <param name="color">The stroke color.</param>
            public ConnectionData(Port fromPort, Port toPort, Color color)
            {
                FromPort = fromPort;
                ToPort = toPort;
                Color = color;
            }

            /// <summary>
            /// Gets the source node output port.
            /// </summary>
            public Port FromPort { get; }

            /// <summary>
            /// Gets the target node input port.
            /// </summary>
            public Port ToPort { get; }

            /// <summary>
            /// Gets the stroke color.
            /// </summary>
            public Color Color { get; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the graph view and its manipulators.
        /// </summary>
        public StatesGraphView()
        {
            style.flexGrow = 1f;
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            Insert(0, new GridBackground());

            _contentDragger = new ContentDragger();
            this.AddManipulator(_contentDragger);
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            StyleSheet styleSheet =
                Resources.Load<StyleSheet>("UI Toolkit/FSM/Styles/handy-fsm-state-visualizer-styles");

            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
        }

        #endregion

        #region Nodes

        /// <summary>
        /// Removes all transient nodes and edges from the graph.
        /// </summary>
        public void ClearGraph()
        {
            ClearConnections();
            DeleteElements(nodes.ToList());
        }

        /// <summary>
        /// Replaces the currently drawn graph connections.
        /// </summary>
        /// <param name="connections">The tree connections to render.</param>
        /// <param name="width">The graph canvas width.</param>
        /// <param name="height">The graph canvas height.</param>
        public void SetConnections(
            IReadOnlyList<ConnectionData> connections,
            float width,
            float height)
        {
            ClearConnections();

            for (int index = 0; index < connections.Count; index++)
            {
                AddConnection(connections[index]);
            }
        }

        /// <summary>
        /// Frames the complete graph after the layout has been updated.
        /// </summary>
        public void FitAllNodes()
        {
            if (!nodes.Any())
                return;

            Rect rectToFit = CalculateRectToFitAll(contentViewContainer);
            CalculateFrameTransform(
                rectToFit,
                layout,
                1,
                out Vector3 frameTranslation,
                out Vector3 frameScaling);

            Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);
            UpdateViewTransform(frameTranslation, frameScaling);
        }

        /// <summary>
        /// Removes the previously created graph connections.
        /// </summary>
        private void ClearConnections()
        {
            if (_connectionEdges.Count == 0)
            {
                return;
            }

            DeleteElements(_connectionEdges);
            _connectionEdges.Clear();
        }

        /// <summary>
        /// Adds a single graph connection using native GraphView edges.
        /// </summary>
        /// <param name="connection">The connection to render.</param>
        private void AddConnection(ConnectionData connection)
        {
            if (connection.FromPort == null || connection.ToPort == null)
            {
                return;
            }

            Edge edge = new()
            {
                output = connection.FromPort,
                input = connection.ToPort,
                capabilities = (Capabilities)0,
                pickingMode = PickingMode.Ignore
            };

            edge.output.Connect(edge);
            edge.input.Connect(edge);
            edge.edgeControl.inputColor = connection.Color;
            edge.edgeControl.outputColor = connection.Color;

            AddElement(edge);
            edge.SendToBack();
            edge.UpdateEdgeControl();
            edge.MarkDirtyRepaint();
            _connectionEdges.Add(edge);
        }

        #endregion
    }
}