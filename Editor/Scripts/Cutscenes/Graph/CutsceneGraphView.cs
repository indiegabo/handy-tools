using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Editor.GraphCore;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Graph
{
    public sealed class CutsceneGraphView : GraphCanvasView<CutsceneGraphNodeView>
    {
        private const float AutoArrangeLayerGap = 96f;
        private const float AutoArrangeNodeGap = 48f;
        private static readonly Color DefaultEdgeColor = new(0.45f, 0.45f, 0.45f);
        private static readonly Color TraversedEdgeColor = new(0.34f, 0.76f, 0.48f);
        private static readonly IReadOnlyList<EdgeColorPreset> EdgeColorPresets =
            new EdgeColorPreset[]
            {
                new("Amber", new Color(0.92f, 0.61f, 0.23f)),
                new("Red", new Color(0.83f, 0.36f, 0.36f)),
                new("Green", new Color(0.42f, 0.78f, 0.48f)),
                new("Blue", new Color(0.37f, 0.60f, 0.92f)),
                new("Teal", new Color(0.22f, 0.74f, 0.72f)),
                new("Purple", new Color(0.59f, 0.47f, 0.87f)),
                new("Slate", new Color(0.62f, 0.67f, 0.73f)),
            };

        private readonly Dictionary<string, Edge> _edgesByConnectionKey =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CutsceneConnection> _connectionsByConnectionKey =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly CutsceneGridBackground _gridBackground;

        private CutsceneDirector _director;
        private bool _isRebuildingGraph;
        private bool _isApplyingNodePositions;

        internal CutsceneGraphView()
        {
            _gridBackground = new CutsceneGridBackground(this);
            InitializeCanvas(
                _gridBackground,
                HandleConnectionDropOutsidePort,
                CleanupTransientEdge);

            graphViewChanged = HandleGraphViewChanged;
        }

        public event Action<ConnectedNodeCreationRequest> ConnectedNodeCreationRequested;

        public event Action GraphModified;

        public CutsceneDirector Director => _director;

        public readonly struct ConnectedNodeCreationRequest
        {
            public ConnectedNodeCreationRequest(
                SerializableGuid fromNodeId,
                string outputKey,
                Vector2 screenPosition)
            {
                FromNodeId = fromNodeId;
                OutputKey = outputKey;
                ScreenPosition = screenPosition;
            }

            public SerializableGuid FromNodeId { get; }

            public string OutputKey { get; }

            public Vector2 ScreenPosition { get; }
        }

        public void BindDirector(CutsceneDirector director)
        {
            _director = director;
            ResetCanvasSelectionState();
            RebuildGraph();
            RefreshRuntimeState();
        }

        internal void AlignNodeInputPortToDropPosition(SerializableGuid nodeId)
        {
            AlignRegisteredNodeInputPortToDropPosition(nodeId);
        }

        public void RebuildGraph(SerializableGuid selectedNodeId = default)
        {
            _isRebuildingGraph = true;

            try
            {
                DeleteElements(edges.ToList());
                DeleteElements(nodes.ToList());
                ClearRegisteredNodeViews();
                _edgesByConnectionKey.Clear();
                _connectionsByConnectionKey.Clear();

                if (_director == null)
                {
                    return;
                }

                IReadOnlyList<CutsceneNodeBase> graphNodes = _director.Graph.Nodes;

                for (int index = 0; index < graphNodes.Count; index++)
                {
                    CutsceneNodeBase graphNode = graphNodes[index];

                    if (graphNode == null)
                    {
                        continue;
                    }

                    CutsceneGraphNodeView nodeView = new(graphNode, EdgeConnectorListener);
                    AddElement(nodeView);
                    RegisterNodeView(nodeView);
                }

                IReadOnlyList<CutsceneConnection> connections = _director.Graph.Connections;

                for (int index = 0; index < connections.Count; index++)
                {
                    TryAddConnectionEdge(connections[index]);
                }

                if (selectedNodeId != default
                    && TryGetRegisteredNodeView(
                        selectedNodeId,
                        out CutsceneGraphNodeView nodeViewToSelect))
                {
                    ClearSelection();
                    AddToSelection(nodeViewToSelect);
                }
            }
            finally
            {
                _isRebuildingGraph = false;
            }
        }

        public void RefreshNodePresentation(SerializableGuid nodeId)
        {
            if (_director == null
                || nodeId == SerializableGuid.Empty
                || !TryGetRegisteredNodeView(nodeId, out CutsceneGraphNodeView nodeView))
            {
                return;
            }

            nodeView.RefreshPresentation();
        }

        public void RefreshRuntimeState()
        {
            foreach (CutsceneGraphNodeView nodeView in NodeViews.Values)
            {
                nodeView.SetRuntimeState(false, false, false, false, false);
            }

            foreach (Edge edge in _edgesByConnectionKey.Values)
            {
                SetEdgeColor(edge, ResolveEdgeColor(GetConnection(edge), false));
            }

            if (_director == null
                || !EditorApplication.isPlaying
                || !_director.TryGetRuntimeRun(out CutsceneRun run)
                || run == null)
            {
                return;
            }

            Dictionary<SerializableGuid, CutsceneNodeStatus> latestNodeStatuses = new();
            HashSet<string> traversedConnections = new(StringComparer.OrdinalIgnoreCase);
            HashSet<SerializableGuid> activeNodeIds = new(run.ActiveNodeIds);

            for (int index = 0; index < run.Trace.Steps.Count; index++)
            {
                CutsceneRunTraceStep step = run.Trace.Steps[index];
                latestNodeStatuses[step.NodeId] = step.NodeStatus;

                if (!string.IsNullOrWhiteSpace(step.OutputKey))
                {
                    traversedConnections.Add(CreateConnectionKey(step.NodeId, step.OutputKey));
                }
            }

            foreach (KeyValuePair<SerializableGuid, CutsceneGraphNodeView> pair in NodeViews)
            {
                bool isCurrent = run.Status == CutsceneRunStatus.Running
                    && activeNodeIds.Contains(pair.Key);
                bool wasSuccessful = latestNodeStatuses.TryGetValue(pair.Key, out CutsceneNodeStatus status)
                    && status == CutsceneNodeStatus.Success;
                bool wasFailed = latestNodeStatuses.TryGetValue(pair.Key, out status)
                    && status == CutsceneNodeStatus.Failure;
                bool wasCancelled = run.Status == CutsceneRunStatus.Cancelled
                    && pair.Key == run.CurrentNodeId;
                bool wasVisited = run.Trace.HasVisited(pair.Key);

                pair.Value.SetRuntimeState(
                    isCurrent,
                    wasSuccessful,
                    wasFailed,
                    wasCancelled,
                    wasVisited);
            }

            foreach (KeyValuePair<string, Edge> pair in _edgesByConnectionKey)
            {
                SetEdgeColor(pair.Value, ResolveEdgeColor(
                    GetConnection(pair.Key),
                    traversedConnections.Contains(pair.Key)));
            }
        }
        private GraphViewChange HandleGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (_director == null || _isRebuildingGraph || _isApplyingNodePositions)
            {
                return graphViewChange;
            }

            bool shouldRebuild = false;

            if (graphViewChange.elementsToRemove != null)
            {
                List<Edge> edgesToRemove = graphViewChange.elementsToRemove.OfType<Edge>().ToList();

                if (edgesToRemove.Count > 0)
                {
                    CutsceneEditorUtility.RecordDirectorChange(_director, "Remove Cutscene Connection");

                    for (int index = 0; index < edgesToRemove.Count; index++)
                    {
                        RemoveConnection(edgesToRemove[index]);
                    }

                    shouldRebuild = true;
                }

                List<CutsceneGraphNodeView> nodesToRemove =
                    graphViewChange.elementsToRemove.OfType<CutsceneGraphNodeView>().ToList();

                if (nodesToRemove.Count > 0)
                {
                    CutsceneEditorUtility.RecordDirectorChange(_director, "Remove Cutscene Node");

                    for (int index = 0; index < nodesToRemove.Count; index++)
                    {
                        _director.Graph.RemoveNode(nodesToRemove[index].Node.Id);
                    }

                    shouldRebuild = true;
                }
            }

            if (graphViewChange.edgesToCreate != null && graphViewChange.edgesToCreate.Count > 0)
            {
                List<Edge> edgesToCreate = graphViewChange.edgesToCreate.ToList();
                graphViewChange.edgesToCreate.Clear();

                CutsceneEditorUtility.RecordDirectorChange(_director, "Create Cutscene Connection");

                for (int index = 0; index < edgesToCreate.Count; index++)
                {
                    Edge transientEdge = edgesToCreate[index];
                    CreateConnection(transientEdge);
                    CleanupTransientEdge(transientEdge);
                }

                shouldRebuild = true;
            }

            List<CutsceneGraphNodeView> movedNodeViews =
                graphViewChange.movedElements?.OfType<CutsceneGraphNodeView>().ToList();

            if (movedNodeViews != null && movedNodeViews.Count > 0)
            {
                List<Vector2> snappedPositions = new(movedNodeViews.Count);
                bool hasPositionChanges = false;

                _isApplyingNodePositions = true;

                try
                {
                    for (int index = 0; index < movedNodeViews.Count; index++)
                    {
                        CutsceneGraphNodeView nodeView = movedNodeViews[index];
                        Rect nodeRect = nodeView.GetPosition();
                        Vector2 snappedPosition = SnapToGrid(nodeRect.position, nodeRect.size);
                        snappedPositions.Add(snappedPosition);

                        if (snappedPosition != nodeRect.position)
                        {
                            nodeView.SetPosition(new Rect(snappedPosition, nodeRect.size));
                        }

                        if (nodeView.Node.Position != snappedPosition)
                        {
                            hasPositionChanges = true;
                        }
                    }
                }
                finally
                {
                    _isApplyingNodePositions = false;
                }

                if (hasPositionChanges)
                {
                    CutsceneEditorUtility.RecordDirectorChange(_director, "Move Cutscene Node");

                    for (int index = 0; index < movedNodeViews.Count; index++)
                    {
                        CutsceneGraphNodeView nodeView = movedNodeViews[index];
                        nodeView.Node.Position = snappedPositions[index];
                    }

                    GraphModified?.Invoke();
                }
            }

            if (shouldRebuild)
            {
                GraphModified?.Invoke();
                RebuildGraph();
                schedule.Execute(PurgeUnexpectedEdges).ExecuteLater(0);
            }

            return graphViewChange;
        }

        internal void AutoArrangeNodes()
        {
            if (_director == null || NodeViews.Count == 0)
            {
                return;
            }

            IReadOnlyList<List<CutsceneGraphNodeView>> layers = BuildLayoutLayers();
            if (layers.Count == 0)
            {
                return;
            }

            Dictionary<int, float> layerPrimarySizes = new();
            Dictionary<int, float> layerCrossSizes = new();
            float maxCrossSize = 0f;

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                List<CutsceneGraphNodeView> layer = layers[layerIndex];
                float maxPrimarySize = 0f;
                float crossSize = 0f;

                for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
                {
                    Vector2 nodeSize = GetNodeSize(layer[nodeIndex]);
                    float primarySize = nodeSize.x;
                    float crossAxisSize = nodeSize.y;

                    maxPrimarySize = Mathf.Max(maxPrimarySize, primarySize);
                    crossSize += crossAxisSize;

                    if (nodeIndex < layer.Count - 1)
                    {
                        crossSize += AutoArrangeNodeGap;
                    }
                }

                layerPrimarySizes[layerIndex] = maxPrimarySize;
                layerCrossSizes[layerIndex] = crossSize;
                maxCrossSize = Mathf.Max(maxCrossSize, crossSize);
            }

            CutsceneEditorUtility.RecordDirectorChange(_director, "Auto Arrange Cutscene Nodes");
            _isApplyingNodePositions = true;

            try
            {
                float primaryPosition = 0f;

                for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
                {
                    List<CutsceneGraphNodeView> layer = layers[layerIndex];
                    float crossPosition = (maxCrossSize - layerCrossSizes[layerIndex]) * 0.5f;

                    for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
                    {
                        CutsceneGraphNodeView nodeView = layer[nodeIndex];
                        Vector2 nodeSize = GetNodeSize(nodeView);
                        Vector2 arrangedPosition = new(primaryPosition, crossPosition);

                        Vector2 snappedPosition = SnapToGrid(arrangedPosition, nodeSize);
                        nodeView.SetPosition(new Rect(snappedPosition, nodeSize));
                        nodeView.Node.Position = snappedPosition;

                        crossPosition += nodeSize.y + AutoArrangeNodeGap;
                    }

                    primaryPosition += layerPrimarySizes[layerIndex] + AutoArrangeLayerGap;
                }
            }
            finally
            {
                _isApplyingNodePositions = false;
            }

            RefreshEdges();
            GraphModified?.Invoke();
            schedule.Execute(() => FrameAll()).ExecuteLater(1);
        }

        private static Vector2 SnapToGrid(Vector2 position, Vector2 size)
        {
            Vector2 snappedCenter = new(
                SnapAxis(position.x + (size.x * 0.5f)),
                SnapAxis(position.y + (size.y * 0.5f)));

            return new Vector2(
                snappedCenter.x - (size.x * 0.5f),
                snappedCenter.y - (size.y * 0.5f));
        }

        private static float SnapAxis(float value)
        {
            return Mathf.Round(value / CutsceneGridBackground.CutsceneMinorStep)
                * CutsceneGridBackground.CutsceneMinorStep;
        }

        private IReadOnlyList<List<CutsceneGraphNodeView>> BuildLayoutLayers()
        {
            Dictionary<SerializableGuid, CutsceneGraphNodeView> arrangeableNodes = NodeViews
                .Where(pair => pair.Value.Node.ParticipatesInAutoArrange)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            if (arrangeableNodes.Count == 0)
            {
                return Array.Empty<List<CutsceneGraphNodeView>>();
            }

            Dictionary<SerializableGuid, List<SerializableGuid>> incoming = new();
            Dictionary<SerializableGuid, List<SerializableGuid>> outgoing = new();

            foreach (KeyValuePair<SerializableGuid, CutsceneGraphNodeView> pair in arrangeableNodes)
            {
                incoming[pair.Key] = new List<SerializableGuid>();
                outgoing[pair.Key] = new List<SerializableGuid>();
            }

            for (int index = 0; index < _director.Graph.Connections.Count; index++)
            {
                CutsceneConnection connection = _director.Graph.Connections[index];

                if (!outgoing.ContainsKey(connection.FromNodeId)
                    || !incoming.ContainsKey(connection.ToNodeId))
                {
                    continue;
                }

                outgoing[connection.FromNodeId].Add(connection.ToNodeId);
                incoming[connection.ToNodeId].Add(connection.FromNodeId);
            }

            Dictionary<SerializableGuid, int> depthByNodeId = new();
            Dictionary<SerializableGuid, int> remainingIncomingCounts =
                incoming.ToDictionary(pair => pair.Key, pair => pair.Value.Count);

            Queue<SerializableGuid> processingQueue = new();
            List<SerializableGuid> roots = remainingIncomingCounts
                .Where(pair => pair.Value == 0)
                .Select(pair => pair.Key)
                .OrderBy(nodeId => GetSecondarySortKey(arrangeableNodes[nodeId]))
                .ToList();

            for (int index = 0; index < roots.Count; index++)
            {
                SerializableGuid rootNodeId = roots[index];
                depthByNodeId[rootNodeId] = 0;
                processingQueue.Enqueue(rootNodeId);
            }

            List<SerializableGuid> topologicalOrder = new();

            while (processingQueue.Count > 0)
            {
                SerializableGuid nodeId = processingQueue.Dequeue();
                topologicalOrder.Add(nodeId);

                List<SerializableGuid> children = outgoing[nodeId];

                for (int childIndex = 0; childIndex < children.Count; childIndex++)
                {
                    SerializableGuid childNodeId = children[childIndex];
                    int nextDepth = depthByNodeId[nodeId] + 1;

                    if (!depthByNodeId.TryGetValue(childNodeId, out int currentDepth)
                        || nextDepth > currentDepth)
                    {
                        depthByNodeId[childNodeId] = nextDepth;
                    }

                    remainingIncomingCounts[childNodeId]--;

                    if (remainingIncomingCounts[childNodeId] == 0)
                    {
                        processingQueue.Enqueue(childNodeId);
                    }
                }
            }

            List<SerializableGuid> unresolvedNodeIds = arrangeableNodes.Keys
                .Where(nodeId => !topologicalOrder.Contains(nodeId))
                .OrderBy(nodeId => GetPrimarySortKey(arrangeableNodes[nodeId]))
                .ToList();

            int fallbackDepth = depthByNodeId.Count == 0
                ? 0
                : depthByNodeId.Values.Max() + 1;

            for (int index = 0; index < unresolvedNodeIds.Count; index++)
            {
                SerializableGuid unresolvedNodeId = unresolvedNodeIds[index];
                depthByNodeId[unresolvedNodeId] = fallbackDepth++;
                topologicalOrder.Add(unresolvedNodeId);
            }

            Dictionary<int, List<SerializableGuid>> layerNodeIds = new();

            for (int index = 0; index < topologicalOrder.Count; index++)
            {
                SerializableGuid nodeId = topologicalOrder[index];
                int depth = depthByNodeId[nodeId];

                if (!layerNodeIds.TryGetValue(depth, out List<SerializableGuid> layer))
                {
                    layer = new List<SerializableGuid>();
                    layerNodeIds.Add(depth, layer);
                }

                layer.Add(nodeId);
            }

            Dictionary<SerializableGuid, float> orderHints = new();
            List<List<CutsceneGraphNodeView>> orderedLayers = new();
            List<int> sortedDepths = layerNodeIds.Keys.OrderBy(depth => depth).ToList();

            for (int depthIndex = 0; depthIndex < sortedDepths.Count; depthIndex++)
            {
                int depth = sortedDepths[depthIndex];
                List<SerializableGuid> layer = layerNodeIds[depth];

                layer.Sort((leftNodeId, rightNodeId) =>
                {
                    float leftOrder = ResolveLayerOrderHint(
                        leftNodeId,
                        incoming,
                        orderHints,
                        GetSecondarySortKey(arrangeableNodes[leftNodeId]));
                    float rightOrder = ResolveLayerOrderHint(
                        rightNodeId,
                        incoming,
                        orderHints,
                        GetSecondarySortKey(arrangeableNodes[rightNodeId]));

                    int orderComparison = leftOrder.CompareTo(rightOrder);
                    if (orderComparison != 0)
                    {
                        return orderComparison;
                    }

                    return GetSecondarySortKey(arrangeableNodes[leftNodeId])
                        .CompareTo(GetSecondarySortKey(arrangeableNodes[rightNodeId]));
                });

                List<CutsceneGraphNodeView> orderedLayer = new(layer.Count);

                for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
                {
                    SerializableGuid nodeId = layer[nodeIndex];
                    orderHints[nodeId] = nodeIndex;
                    orderedLayer.Add(arrangeableNodes[nodeId]);
                }

                orderedLayers.Add(orderedLayer);
            }

            return orderedLayers;
        }

        private static float ResolveLayerOrderHint(
            SerializableGuid nodeId,
            IReadOnlyDictionary<SerializableGuid, List<SerializableGuid>> incoming,
            IReadOnlyDictionary<SerializableGuid, float> orderHints,
            float fallbackOrderHint)
        {
            List<SerializableGuid> parentNodeIds = incoming[nodeId];
            float total = 0f;
            int count = 0;

            for (int index = 0; index < parentNodeIds.Count; index++)
            {
                SerializableGuid parentNodeId = parentNodeIds[index];

                if (!orderHints.TryGetValue(parentNodeId, out float orderHint))
                {
                    continue;
                }

                total += orderHint;
                count++;
            }

            if (count > 0)
            {
                return total / count;
            }

            return fallbackOrderHint;
        }

        private static Vector2 GetNodeSize(CutsceneGraphNodeView nodeView)
        {
            Rect nodeRect = nodeView.GetPosition();
            return nodeRect.size == Vector2.zero
                ? new Vector2(240f, 120f)
                : nodeRect.size;
        }

        private static float GetPrimarySortKey(CutsceneGraphNodeView nodeView)
        {
            return nodeView.Node.Position.x;
        }

        private static float GetSecondarySortKey(CutsceneGraphNodeView nodeView)
        {
            return nodeView.Node.Position.y;
        }

        private void RefreshEdges()
        {
            foreach (Edge edge in _edgesByConnectionKey.Values)
            {
                edge.UpdateEdgeControl();
                edge.MarkDirtyRepaint();
            }
        }

        private void CreateConnection(Edge edge)
        {
            if (edge?.output?.node is not CutsceneGraphNodeView fromNodeView
                || edge.input?.node is not CutsceneGraphNodeView toNodeView)
            {
                return;
            }

            string outputKey = edge.output.userData as string ?? CutsceneNodePorts.Next;
            _director.Graph.Connect(fromNodeView.Node.Id, outputKey, toNodeView.Node.Id);
        }

        private void RemoveConnection(Edge edge)
        {
            if (edge?.output?.node is not CutsceneGraphNodeView fromNodeView)
            {
                return;
            }

            string outputKey = edge.output.userData as string ?? CutsceneNodePorts.Next;
            _director.Graph.Disconnect(fromNodeView.Node.Id, outputKey);
        }

        private static void CleanupTransientEdge(Edge edge)
        {
            if (edge == null)
            {
                return;
            }

            edge.output?.Disconnect(edge);
            edge.input?.Disconnect(edge);
            edge.RemoveFromHierarchy();
        }

        private void HandleConnectionDropOutsidePort(Edge edge, Vector2 screenPosition)
        {
            Port draggedPort = edge.output?.edgeConnector?.edgeDragHelper?.draggedPort
                ?? edge.input?.edgeConnector?.edgeDragHelper?.draggedPort;

            CleanupTransientEdge(edge);

            if (draggedPort?.direction != UnityEditor.Experimental.GraphView.Direction.Output
                || draggedPort.node is not CutsceneGraphNodeView fromNodeView)
            {
                return;
            }

            string outputKey = draggedPort.userData as string ?? CutsceneNodePorts.Next;
            ConnectedNodeCreationRequested?.Invoke(new ConnectedNodeCreationRequest(
                fromNodeView.Node.Id,
                outputKey,
                screenPosition));
        }

        private void PurgeUnexpectedEdges()
        {
            List<Edge> unexpectedEdges = edges
                .ToList()
                .Where(IsUnexpectedVisualEdge)
                .ToList();

            for (int index = 0; index < unexpectedEdges.Count; index++)
            {
                CleanupTransientEdge(unexpectedEdges[index]);
            }
        }

        private bool IsUnexpectedVisualEdge(Edge edge)
        {
            if (edge?.output?.node is not CutsceneGraphNodeView fromNodeView
                || edge.input?.node is not CutsceneGraphNodeView)
            {
                return true;
            }

            string outputKey = edge.output.userData as string ?? CutsceneNodePorts.Next;
            string connectionKey = CreateConnectionKey(fromNodeView.Node.Id, outputKey);

            return !_edgesByConnectionKey.TryGetValue(connectionKey, out Edge trackedEdge)
                || !ReferenceEquals(trackedEdge, edge);
        }

        private void TryAddConnectionEdge(CutsceneConnection connection)
        {
            if (!TryGetRegisteredNodeView(connection.FromNodeId, out CutsceneGraphNodeView fromNodeView)
                || !TryGetRegisteredNodeView(connection.ToNodeId, out CutsceneGraphNodeView toNodeView)
                || !fromNodeView.OutputPorts.TryGetValue(connection.OutputKey, out Port outputPort)
                || toNodeView.InputPort == null)
            {
                return;
            }

            string connectionKey = CreateConnectionKey(connection.FromNodeId, connection.OutputKey);

            Edge edge = new CutsceneGraphEdge(
                color => UpdateConnectionColor(connectionKey, color))
            {
                output = outputPort,
                input = toNodeView.InputPort,
                capabilities = Capabilities.Selectable | Capabilities.Deletable,
            };

            edge.output.Connect(edge);
            edge.input.Connect(edge);
            AddElement(edge);
            edge.SendToBack();

            _edgesByConnectionKey[connectionKey] = edge;
            _connectionsByConnectionKey[connectionKey] = connection;
            SetEdgeColor(edge, ResolveEdgeColor(connection, false));
        }

        private CutsceneConnection GetConnection(Edge edge)
        {
            if (edge?.output?.node is not CutsceneGraphNodeView fromNodeView)
            {
                return null;
            }

            string outputKey = edge.output.userData as string ?? CutsceneNodePorts.Next;
            return GetConnection(CreateConnectionKey(fromNodeView.Node.Id, outputKey));
        }

        private CutsceneConnection GetConnection(string connectionKey)
        {
            return _connectionsByConnectionKey.TryGetValue(connectionKey, out CutsceneConnection connection)
                ? connection
                : null;
        }

        private static Color ResolveEdgeColor(CutsceneConnection connection, bool isTraversed)
        {
            Color authoredColor = connection != null && connection.HasCustomColor
                ? connection.CustomColor
                : DefaultEdgeColor;

            if (!isTraversed)
            {
                return authoredColor;
            }

            return connection != null && connection.HasCustomColor
                ? Color.Lerp(authoredColor, TraversedEdgeColor, 0.55f)
                : TraversedEdgeColor;
        }

        private void UpdateConnectionColor(string connectionKey, Color? color)
        {
            if (_director == null
                || string.IsNullOrWhiteSpace(connectionKey)
                || !_connectionsByConnectionKey.TryGetValue(connectionKey, out CutsceneConnection connection))
            {
                return;
            }

            CutsceneEditorUtility.RecordDirectorChange(
                _director,
                color.HasValue
                    ? "Set Cutscene Connection Color"
                    : "Clear Cutscene Connection Color");

            if (color.HasValue)
            {
                connection.SetCustomColor(color.Value);
            }
            else
            {
                connection.ClearCustomColor();
            }

            CutsceneEditorUtility.MarkDirectorDirty(_director);
            RefreshRuntimeState();
            GraphModified?.Invoke();
        }

        private static void SetEdgeColor(Edge edge, Color color)
        {
            if (edge == null)
            {
                return;
            }

            if (edge is CutsceneGraphEdge cutsceneEdge)
            {
                cutsceneEdge.SetColor(color);
                return;
            }

            ApplyEdgeColor(edge, color);
        }

        private static void ApplyEdgeColor(Edge edge, Color color)
        {
            edge.edgeControl.inputColor = color;
            edge.edgeControl.outputColor = color;
            edge.MarkDirtyRepaint();
        }

        private static string CreateConnectionKey(SerializableGuid nodeId, string outputKey)
        {
            return $"{nodeId.ToHexString()}::{outputKey}";
        }

        protected override void HandleNodeInputPortAligned(
            CutsceneGraphNodeView nodeView,
            Vector2 alignedPosition)
        {
            nodeView.Node.Position = alignedPosition;

            RefreshEdges();

            if (_director != null)
            {
                CutsceneEditorUtility.MarkDirectorDirty(_director);
            }
        }

        protected override void ApplyAlignedNodePosition(
            CutsceneGraphNodeView nodeView,
            Rect position)
        {
            _isApplyingNodePositions = true;

            try
            {
                nodeView.SetPosition(position);
            }
            finally
            {
                _isApplyingNodePositions = false;
            }

            HandleNodeInputPortAligned(nodeView, position.position);
        }

        private readonly struct EdgeColorPreset
        {
            public EdgeColorPreset(string label, Color color)
            {
                Label = label;
                Color = color;
            }

            public string Label { get; }

            public Color Color { get; }
        }

        private sealed class CutsceneGraphEdge : Edge
        {
            private readonly Action<Color?> _applyColor;
            private Color _currentColor = DefaultEdgeColor;

            public CutsceneGraphEdge(Action<Color?> applyColor)
            {
                _applyColor = applyColor;
                RegisterCallback<ContextualMenuPopulateEvent>(PopulateContextualMenu);
                RegisterCallback<GeometryChangedEvent>(_ => ApplyCurrentColor());
                RegisterCallback<AttachToPanelEvent>(_ => schedule.Execute(ApplyCurrentColor).ExecuteLater(0));
            }

            public void SetColor(Color color)
            {
                _currentColor = color;
                ApplyCurrentColor();
                schedule.Execute(ApplyCurrentColor).ExecuteLater(0);
            }

            private void PopulateContextualMenu(ContextualMenuPopulateEvent evt)
            {
                evt.menu.AppendSeparator();

                for (int index = 0; index < EdgeColorPresets.Count; index++)
                {
                    EdgeColorPreset preset = EdgeColorPresets[index];
                    evt.menu.AppendAction(
                        $"Connection Color/{preset.Label}",
                        _ => _applyColor?.Invoke(preset.Color),
                        _ => DropdownMenuAction.Status.Normal);
                }

                evt.menu.AppendAction(
                    "Connection Color/Clear",
                    _ => _applyColor?.Invoke(null),
                    _ => DropdownMenuAction.Status.Normal);
            }

            private void ApplyCurrentColor()
            {
                ApplyEdgeColor(this, _currentColor);
            }
        }

    }
}