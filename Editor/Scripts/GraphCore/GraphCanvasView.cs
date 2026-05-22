using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Provides one reusable GraphView canvas shell with shared graph-surface behavior.
    /// </summary>
    /// <typeparam name="TNodeView">Concrete node-view type rendered by the canvas.</typeparam>
    public abstract class GraphCanvasView<TNodeView> : GraphView
        where TNodeView : Node, IGraphCanvasNodeView
    {
        private const int PortAlignmentStabilizationPasses = 3;

        private readonly DeferredGraphUiActionDispatcher _selectionChangedDispatcher;
        private readonly Dictionary<SerializableGuid, TNodeView> _nodeViews = new();
        private readonly Dictionary<Port, Vector2> _portAlignmentTargets = new();
        private readonly HashSet<Port> _portsPendingAlignment = new();

        private Vector2 _cachedMousePosition;
        private SerializableGuid _lastSelectedNodeId;

        /// <summary>
        /// Initializes the reusable graph canvas shell.
        /// </summary>
        protected GraphCanvasView()
        {
            style.flexGrow = 1f;
            _selectionChangedDispatcher = new DeferredGraphUiActionDispatcher(this);
        }

        /// <summary>
        /// Raised when the selected node view changes.
        /// </summary>
        public event Action<TNodeView> NodeSelected;

        /// <summary>
        /// Raised when the user requests node creation on the canvas.
        /// </summary>
        public event Action<Vector2> NodeCreationRequested;

        /// <summary>
        /// Gets the reusable edge connector listener owned by the canvas.
        /// </summary>
        protected IEdgeConnectorListener EdgeConnectorListener { get; private set; }

        /// <summary>
        /// Gets the currently registered node views keyed by stable node id.
        /// </summary>
        protected IReadOnlyDictionary<SerializableGuid, TNodeView> NodeViews => _nodeViews;

        /// <summary>
        /// Applies the shared graph-canvas shell configuration.
        /// </summary>
        /// <param name="gridBackground">Grid background element mounted behind the graph content.</param>
        /// <param name="dropOutsidePortHandler">
        /// Callback invoked when one edge is dropped outside a compatible port.
        /// </param>
        /// <param name="cleanupInvalidEdge">
        /// Callback used to remove transient edges that should not stay in the canvas.
        /// </param>
        protected void InitializeCanvas(
            VisualElement gridBackground,
            Action<Edge, Vector2> dropOutsidePortHandler,
            Action<Edge> cleanupInvalidEdge)
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            if (gridBackground != null)
            {
                Insert(0, gridBackground);
                viewTransformChanged = _ => gridBackground.MarkDirtyRepaint();
                RegisterCallback<GeometryChangedEvent>(_ => gridBackground.MarkDirtyRepaint());
            }

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            EdgeConnectorListener = new GraphEdgeConnectorListener(
                this,
                dropOutsidePortHandler,
                cleanupInvalidEdge);

            nodeCreationRequest = _ => NodeCreationRequested?.Invoke(_cachedMousePosition);
            RegisterCallback<MouseMoveEvent>(HandleMouseMove);
            RegisterCallback<MouseDownEvent>(HandleMouseDown);
            RegisterCallback<KeyUpEvent>(_ => ScheduleSelectionChangedNotification());
        }

        /// <summary>
        /// Resets the internal selection cache used by the deferred selection notifications.
        /// </summary>
        protected void ResetCanvasSelectionState()
        {
            _lastSelectedNodeId = SerializableGuid.Empty;
        }

        /// <summary>
        /// Removes all registered node views and unsubscribes their shared callbacks.
        /// </summary>
        protected void ClearRegisteredNodeViews()
        {
            foreach (TNodeView nodeView in _nodeViews.Values)
            {
                nodeView.SelectionStateChanged -= HandleNodeViewSelectionStateChanged;
            }

            _nodeViews.Clear();
            _portAlignmentTargets.Clear();
            _portsPendingAlignment.Clear();
        }

        /// <summary>
        /// Registers one node view with the shared canvas shell.
        /// </summary>
        /// <param name="nodeView">Node view to register.</param>
        protected void RegisterNodeView(TNodeView nodeView)
        {
            if (nodeView == null)
            {
                return;
            }

            nodeView.SelectionStateChanged -= HandleNodeViewSelectionStateChanged;
            nodeView.SelectionStateChanged += HandleNodeViewSelectionStateChanged;
            _nodeViews[nodeView.NodeId] = nodeView;
        }

        /// <summary>
        /// Attempts to resolve one registered node view by stable node id.
        /// </summary>
        /// <param name="nodeId">Stable node identifier.</param>
        /// <param name="nodeView">Resolved node view when found.</param>
        /// <returns>True when the node view exists.</returns>
        protected bool TryGetRegisteredNodeView(
            SerializableGuid nodeId,
            out TNodeView nodeView)
        {
            return _nodeViews.TryGetValue(nodeId, out nodeView);
        }

        /// <summary>
        /// Schedules one selected node input port to align with its current drop position.
        /// </summary>
        /// <param name="nodeId">Stable node identifier.</param>
        protected void AlignRegisteredNodeInputPortToDropPosition(SerializableGuid nodeId)
        {
            if (!TryGetRegisteredNodeView(nodeId, out TNodeView nodeView)
                || nodeView.InputPort == null)
            {
                return;
            }

            _portAlignmentTargets[nodeView.InputPort] = nodeView.AuthoredPosition;

            if (_portsPendingAlignment.Add(nodeView.InputPort))
            {
                nodeView.InputPort.RegisterCallback<GeometryChangedEvent>(HandlePendingPortAlignment);
            }
        }

        /// <inheritdoc />
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(port =>
                    port != startPort
                    && port.node != startPort.node
                    && port.direction != startPort.direction)
                .ToList();
        }

        /// <summary>
        /// Called after one input port alignment has updated the node position in the view.
        /// </summary>
        /// <param name="nodeView">Node view that was aligned.</param>
        /// <param name="alignedPosition">New aligned node position.</param>
        protected abstract void HandleNodeInputPortAligned(
            TNodeView nodeView,
            Vector2 alignedPosition);

        /// <summary>
        /// Applies one aligned node position to the view and authored model.
        /// </summary>
        /// <param name="nodeView">Node view receiving the aligned position.</param>
        /// <param name="position">Aligned graph-space rect to apply.</param>
        protected virtual void ApplyAlignedNodePosition(
            TNodeView nodeView,
            Rect position)
        {
            nodeView.SetPosition(position);
            HandleNodeInputPortAligned(nodeView, position.position);
        }

        private void ScheduleSelectionChangedNotification()
        {
            _selectionChangedDispatcher.Dispatch(NotifySelectionChanged);
        }

        private void NotifySelectionChanged()
        {
            TNodeView selectedNodeView = selection?.OfType<TNodeView>().FirstOrDefault();
            SerializableGuid selectedNodeId = selectedNodeView?.NodeId
                ?? SerializableGuid.Empty;

            if (selectedNodeId == _lastSelectedNodeId)
            {
                return;
            }

            _lastSelectedNodeId = selectedNodeId;
            NodeSelected?.Invoke(selectedNodeView);
        }

        private void HandlePendingPortAlignment(GeometryChangedEvent evt)
        {
            if (evt.target is not Port port)
            {
                return;
            }

            TryAlignPendingPort(port);
        }

        private Vector2 ResolveAlignedPortPosition(Port port)
        {
            return contentViewContainer.WorldToLocal(
                port.GetGlobalCenter() + new Vector3(3f, 3f, 0f));
        }

        private Vector2 ResolvePortAlignmentOffset(TNodeView nodeView, Port port)
        {
            return nodeView.mainContainer.WorldToLocal(
                port.GetGlobalCenter() + new Vector3(3f, 3f, 0f));
        }

        private void TryAlignPendingPort(Port port)
        {
            if (port == null
                || !_portsPendingAlignment.Contains(port)
                || port.panel == null
                || port.worldBound.width <= 0f
                || port.worldBound.height <= 0f
                || port.node is not TNodeView nodeView
                || nodeView.worldBound.width <= 0f
                || nodeView.worldBound.height <= 0f
                || !_portAlignmentTargets.TryGetValue(port, out Vector2 desiredPortPosition))
            {
                return;
            }

            _portsPendingAlignment.Remove(port);
            _portAlignmentTargets.Remove(port);
            port.UnregisterCallback<GeometryChangedEvent>(HandlePendingPortAlignment);

            Rect position = nodeView.GetPosition();
            position.position = desiredPortPosition - ResolvePortAlignmentOffset(nodeView, port);

            ApplyAlignedNodePosition(nodeView, position);

            schedule.Execute(() => StabilizePendingPortAlignment(
                    port,
                    desiredPortPosition,
                    PortAlignmentStabilizationPasses))
                .ExecuteLater(0);
        }

        private void StabilizePendingPortAlignment(
            Port port,
            Vector2 desiredPortPosition,
            int remainingPasses)
        {
            if (port == null
                || port.panel == null
                || port.node is not TNodeView nodeView
                || nodeView.worldBound.width <= 0f
                || nodeView.worldBound.height <= 0f)
            {
                return;
            }

            Vector2 alignedPortPosition = ResolveAlignedPortPosition(port);
            Vector2 residualOffset = alignedPortPosition - desiredPortPosition;

            if (residualOffset.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Rect position = nodeView.GetPosition();
            position.position = desiredPortPosition - ResolvePortAlignmentOffset(nodeView, port);

            ApplyAlignedNodePosition(nodeView, position);

            if (remainingPasses > 1)
            {
                schedule.Execute(() => StabilizePendingPortAlignment(
                        port,
                        desiredPortPosition,
                        remainingPasses - 1))
                    .ExecuteLater(0);
            }
        }

        private void HandleNodeViewSelectionStateChanged()
        {
            ScheduleSelectionChangedNotification();
        }

        private void HandleMouseDown(MouseDownEvent evt)
        {
            _cachedMousePosition = evt.localMousePosition;
        }

        private void HandleMouseMove(MouseMoveEvent evt)
        {
            _cachedMousePosition = evt.localMousePosition;
        }
    }
}