using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using IndieGabo.HandyTools.Editor.GraphCore;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Graph
{
    /// <summary>
    /// Renders one minimal Conversations-authored graph on the shared canvas shell.
    /// </summary>
    public sealed class ConversationGraphView : GraphCanvasView<ConversationGraphNodeView>
    {
        private ConversationTable _table;
        private ConversationDefinition _conversation;
        private bool _isRebuildingGraph;

        /// <summary>
        /// Describes one pending connected-node creation request emitted from one edge drop.
        /// </summary>
        public readonly struct ConnectedNodeCreationRequest
        {
            /// <summary>
            /// Creates one connected-node creation request.
            /// </summary>
            /// <param name="fromNodeId">Origin node that should be connected.</param>
            /// <param name="outputKey">Origin output key that should be used.</param>
            /// <param name="screenPosition">Drop position provided by GraphView.</param>
            public ConnectedNodeCreationRequest(
                SerializableGuid fromNodeId,
                string outputKey,
                Vector2 screenPosition)
            {
                FromNodeId = fromNodeId;
                OutputKey = outputKey;
                ScreenPosition = screenPosition;
            }

            /// <summary>
            /// Gets the origin node identifier.
            /// </summary>
            public SerializableGuid FromNodeId { get; }

            /// <summary>
            /// Gets the origin output key.
            /// </summary>
            public string OutputKey { get; }

            /// <summary>
            /// Gets the drop position provided by GraphView.
            /// </summary>
            public Vector2 ScreenPosition { get; }
        }

        /// <summary>
        /// Creates one Conversations graph canvas.
        /// </summary>
        internal ConversationGraphView()
        {
            GridBackground gridBackground = new();
            gridBackground.style.position = Position.Absolute;
            gridBackground.style.left = 0f;
            gridBackground.style.top = 0f;
            gridBackground.style.right = 0f;
            gridBackground.style.bottom = 0f;

            InitializeCanvas(
                gridBackground,
                HandleConnectionDropOutsidePort,
                CleanupTransientEdge);

            graphViewChanged = HandleGraphViewChanged;
        }

        /// <summary>
        /// Raised after one authored graph mutation is committed.
        /// </summary>
        public event Action GraphModified;

        /// <summary>
        /// Raised when the user drops one edge onto the canvas and wants to create one node.
        /// </summary>
        public event Action<ConnectedNodeCreationRequest> ConnectedNodeCreationRequested;

        /// <summary>
        /// Gets the currently bound conversation table.
        /// </summary>
        public ConversationTable Table => _table;

        /// <summary>
        /// Gets the currently selected authored conversation.
        /// </summary>
        public ConversationDefinition Conversation => _conversation;

        /// <summary>
        /// Binds the canvas to one authored conversation inside one conversation table.
        /// </summary>
        /// <param name="table">Authored table bound to the canvas.</param>
        /// <param name="conversation">Selected authored conversation.</param>
        public void BindConversation(
            ConversationTable table,
            ConversationDefinition conversation)
        {
            _table = table;
            _conversation = conversation;
            ResetCanvasSelectionState();
            RebuildGraph();
        }

        /// <summary>
        /// Aligns one node input port to the most recent edge-drop position.
        /// </summary>
        /// <param name="nodeId">Node that should be aligned.</param>
        internal void AlignNodeInputPortToDropPosition(SerializableGuid nodeId)
        {
            AlignRegisteredNodeInputPortToDropPosition(nodeId);
        }

        /// <summary>
        /// Creates one node of the requested type in the selected conversation.
        /// </summary>
        /// <param name="nodeType">Concrete Conversations node type to instantiate.</param>
        /// <param name="graphPosition">Target graph position.</param>
        /// <returns>True when the node was created.</returns>
        public bool CreateNode(Type nodeType, Vector2 graphPosition)
        {
            return CreateNode(nodeType, graphPosition, default, null) != null;
        }

        /// <summary>
        /// Creates one node and optionally connects it from one existing output.
        /// </summary>
        /// <param name="nodeType">Concrete Conversations node type to instantiate.</param>
        /// <param name="graphPosition">Target graph position.</param>
        /// <param name="connectFromNodeId">Optional origin node to connect from.</param>
        /// <param name="connectOutputKey">Optional origin output key.</param>
        /// <returns>The created node when successful.</returns>
        public ConversationNodeBase CreateNode(
            Type nodeType,
            Vector2 graphPosition,
            SerializableGuid connectFromNodeId,
            string connectOutputKey)
        {
            if (_table == null || _conversation == null)
            {
                return null;
            }

            ConversationNodeBase node = ConversationNodeCreationRegistry.CreateNode(nodeType);

            if (node == null)
            {
                return null;
            }

            if (node is ConversationEntryNode
                && _conversation.Graph.HasEntryNode())
            {
                return null;
            }

            Undo.RecordObject(
                _table,
                connectFromNodeId == SerializableGuid.Empty
                    ? "Create Conversation Node"
                    : "Add Connected Conversation Node");
            node.Position = graphPosition;
            _conversation.Graph.AddNode(node);

            if (connectFromNodeId != SerializableGuid.Empty)
            {
                _conversation.Graph.Connect(
                    connectFromNodeId,
                    string.IsNullOrWhiteSpace(connectOutputKey)
                        ? GraphPortKeys.Next
                        : connectOutputKey,
                    node.Id);
            }

            _table.EnsureAuthoringIds();
            EditorUtility.SetDirty(_table);
            RebuildGraph(node.Id);
            GraphModified?.Invoke();
            return node;
        }

        /// <summary>
        /// Rebuilds the visual canvas from the currently bound graph.
        /// </summary>
        /// <param name="selectedNodeId">Optional node that should stay selected.</param>
        public void RebuildGraph(SerializableGuid selectedNodeId = default)
        {
            _isRebuildingGraph = true;

            try
            {
                DeleteElements(edges.ToList());
                DeleteElements(nodes.ToList());
                ClearRegisteredNodeViews();

                if (_table == null || _conversation == null)
                {
                    return;
                }

                IReadOnlyList<GraphNodeBase> nodesToRender = _conversation.Graph.Nodes;

                for (int index = 0; index < nodesToRender.Count; index++)
                {
                    if (nodesToRender[index] is not ConversationNodeBase node)
                    {
                        continue;
                    }

                    ConversationGraphNodeView nodeView = new(node, EdgeConnectorListener);
                    AddElement(nodeView);
                    RegisterNodeView(nodeView);
                }

                IReadOnlyList<GraphConnection> connections = _conversation.Graph.Connections;

                for (int index = 0; index < connections.Count; index++)
                {
                    TryAddConnectionEdge(connections[index]);
                }

                if (selectedNodeId != default
                    && TryGetRegisteredNodeView(
                        selectedNodeId,
                        out ConversationGraphNodeView selectedNodeView))
                {
                    ClearSelection();
                    AddToSelection(selectedNodeView);
                }
            }
            finally
            {
                _isRebuildingGraph = false;
            }
        }

        /// <summary>
        /// Refreshes one rendered node after authored inspector changes.
        /// </summary>
        /// <param name="nodeId">Stable node identifier.</param>
        public void RefreshNodePresentation(SerializableGuid nodeId)
        {
            if (nodeId == SerializableGuid.Empty
                || !TryGetRegisteredNodeView(nodeId, out ConversationGraphNodeView nodeView))
            {
                return;
            }

            nodeView.RefreshPresentation();
        }

        /// <inheritdoc />
        protected override void HandleNodeInputPortAligned(
            ConversationGraphNodeView nodeView,
            Vector2 alignedPosition)
        {
            if (_table == null || _conversation == null || nodeView?.Node == null)
            {
                return;
            }

            Undo.RecordObject(_table, "Align Conversation Node");
            nodeView.Node.Position = alignedPosition;
            EditorUtility.SetDirty(_table);
            GraphModified?.Invoke();
        }

        private GraphViewChange HandleGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (_table == null || _conversation == null || _isRebuildingGraph)
            {
                return graphViewChange;
            }

            bool hasGraphChanges = false;

            if (graphViewChange.elementsToRemove != null)
            {
                HashSet<SerializableGuid> protectedEntryNodeIds =
                    ResolveProtectedEntryNodeIds(graphViewChange.elementsToRemove);

                if (protectedEntryNodeIds.Count > 0)
                {
                    graphViewChange.elementsToRemove = FilterProtectedRemovalElements(
                        graphViewChange.elementsToRemove,
                        protectedEntryNodeIds);
                }

                List<Edge> edgesToRemove = graphViewChange.elementsToRemove
                    .OfType<Edge>()
                    .ToList();

                if (edgesToRemove.Count > 0)
                {
                    Undo.RecordObject(_table, "Remove Conversation Connection");

                    for (int index = 0; index < edgesToRemove.Count; index++)
                    {
                        RemoveConnection(edgesToRemove[index]);
                    }

                    hasGraphChanges = true;
                }

                List<ConversationGraphNodeView> nodesToRemove = graphViewChange.elementsToRemove
                    .OfType<ConversationGraphNodeView>()
                    .ToList();

                if (nodesToRemove.Count > 0)
                {
                    Undo.RecordObject(_table, "Remove Conversation Node");

                    for (int index = 0; index < nodesToRemove.Count; index++)
                    {
                        _conversation.Graph.RemoveNode(nodesToRemove[index].Node.Id);
                    }

                    hasGraphChanges = true;
                }
            }

            if (graphViewChange.edgesToCreate != null && graphViewChange.edgesToCreate.Count > 0)
            {
                List<Edge> edgesToCreate = graphViewChange.edgesToCreate.ToList();
                graphViewChange.edgesToCreate.Clear();

                Undo.RecordObject(_table, "Create Conversation Connection");

                for (int index = 0; index < edgesToCreate.Count; index++)
                {
                    Edge transientEdge = edgesToCreate[index];
                    CreateConnection(transientEdge);
                    CleanupTransientEdge(transientEdge);
                }

                hasGraphChanges = true;
            }

            List<ConversationGraphNodeView> movedNodeViews = graphViewChange.movedElements
                ?.OfType<ConversationGraphNodeView>()
                .ToList();

            if (movedNodeViews != null && movedNodeViews.Count > 0)
            {
                Undo.RecordObject(_table, "Move Conversation Nodes");

                for (int index = 0; index < movedNodeViews.Count; index++)
                {
                    ConversationGraphNodeView nodeView = movedNodeViews[index];

                    if (nodeView?.Node == null)
                    {
                        continue;
                    }

                    nodeView.Node.Position = nodeView.GetPosition().position;
                }

                hasGraphChanges = true;
            }

            if (!hasGraphChanges)
            {
                return graphViewChange;
            }

            _table.EnsureAuthoringIds();
            EditorUtility.SetDirty(_table);
            GraphModified?.Invoke();
            return graphViewChange;
        }

        private HashSet<SerializableGuid> ResolveProtectedEntryNodeIds(
            IReadOnlyList<GraphElement> elementsToRemove)
        {
            HashSet<SerializableGuid> protectedNodeIds = new();

            if (elementsToRemove == null || elementsToRemove.Count == 0)
            {
                return protectedNodeIds;
            }

            int remainingEntryNodeCount = 0;
            IReadOnlyList<GraphNodeBase> graphNodes = _conversation.Graph.Nodes;

            for (int index = 0; index < graphNodes.Count; index++)
            {
                if (ConversationGraph.IsEntryNode(graphNodes[index]))
                {
                    remainingEntryNodeCount++;
                }
            }

            if (remainingEntryNodeCount <= 0)
            {
                return protectedNodeIds;
            }

            List<ConversationGraphNodeView> nodeViewsToRemove = elementsToRemove
                .OfType<ConversationGraphNodeView>()
                .ToList();

            for (int index = 0; index < nodeViewsToRemove.Count; index++)
            {
                ConversationGraphNodeView nodeView = nodeViewsToRemove[index];

                if (nodeView?.Node == null
                    || !ConversationGraph.IsEntryNode(nodeView.Node))
                {
                    continue;
                }

                if (remainingEntryNodeCount <= 1)
                {
                    protectedNodeIds.Add(nodeView.Node.Id);
                    continue;
                }

                remainingEntryNodeCount--;
            }

            return protectedNodeIds;
        }

        private static List<GraphElement> FilterProtectedRemovalElements(
            IReadOnlyList<GraphElement> elementsToRemove,
            HashSet<SerializableGuid> protectedNodeIds)
        {
            List<GraphElement> filteredElements = new(elementsToRemove.Count);

            for (int index = 0; index < elementsToRemove.Count; index++)
            {
                GraphElement element = elementsToRemove[index];

                if (!IsProtectedRemovalElement(element, protectedNodeIds))
                {
                    filteredElements.Add(element);
                }
            }

            return filteredElements;
        }

        private static bool IsProtectedRemovalElement(
            GraphElement element,
            HashSet<SerializableGuid> protectedNodeIds)
        {
            if (element is ConversationGraphNodeView nodeView)
            {
                return nodeView.Node != null
                    && protectedNodeIds.Contains(nodeView.Node.Id);
            }

            if (element is Edge edge)
            {
                SerializableGuid outputNodeId = GetNodeId(
                    edge.output?.node as ConversationGraphNodeView);

                if (outputNodeId != SerializableGuid.Empty
                    && protectedNodeIds.Contains(outputNodeId))
                {
                    return true;
                }

                SerializableGuid inputNodeId = GetNodeId(
                    edge.input?.node as ConversationGraphNodeView);

                return inputNodeId != SerializableGuid.Empty
                    && protectedNodeIds.Contains(inputNodeId);
            }

            return false;
        }

        private static SerializableGuid GetNodeId(ConversationGraphNodeView nodeView)
        {
            return nodeView?.Node?.Id ?? SerializableGuid.Empty;
        }

        private void HandleConnectionDropOutsidePort(Edge edge, Vector2 screenPosition)
        {
            Port draggedPort = edge.output?.edgeConnector?.edgeDragHelper?.draggedPort
                ?? edge.input?.edgeConnector?.edgeDragHelper?.draggedPort;

            CleanupTransientEdge(edge);

            if (draggedPort?.direction != UnityEditor.Experimental.GraphView.Direction.Output
                || draggedPort.node is not ConversationGraphNodeView fromNodeView)
            {
                return;
            }

            string outputKey = draggedPort.userData as string ?? GraphPortKeys.Next;
            ConnectedNodeCreationRequested?.Invoke(new ConnectedNodeCreationRequest(
                fromNodeView.Node.Id,
                outputKey,
                screenPosition));
        }

        private void CleanupTransientEdge(Edge edge)
        {
            if (edge == null)
            {
                return;
            }

            edge.output?.Disconnect(edge);
            edge.input?.Disconnect(edge);
            edge.RemoveFromHierarchy();
        }

        private void TryAddConnectionEdge(GraphConnection connection)
        {
            if (connection == null
                || !TryGetRegisteredNodeView(
                    connection.FromNodeId,
                    out ConversationGraphNodeView fromNodeView)
                || !TryGetRegisteredNodeView(
                    connection.ToNodeId,
                    out ConversationGraphNodeView toNodeView)
                || !fromNodeView.OutputPorts.TryGetValue(
                    connection.OutputKey,
                    out Port outputPort)
                || toNodeView.InputPort == null)
            {
                return;
            }

            Edge edge = outputPort.ConnectTo(toNodeView.InputPort);
            AddElement(edge);
        }

        private void CreateConnection(Edge edge)
        {
            if (edge?.output?.node is not ConversationGraphNodeView fromNodeView
                || edge.input?.node is not ConversationGraphNodeView toNodeView)
            {
                return;
            }

            string outputKey = edge.output.userData as string ?? GraphPortKeys.Next;
            _conversation.Graph.Connect(fromNodeView.Node.Id, outputKey, toNodeView.Node.Id);
        }

        private void RemoveConnection(Edge edge)
        {
            if (edge?.output?.node is not ConversationGraphNodeView fromNodeView)
            {
                return;
            }

            string outputKey = edge.output.userData as string ?? GraphPortKeys.Next;
            _conversation.Graph.Disconnect(fromNodeView.Node.Id, outputKey);
        }
    }
}