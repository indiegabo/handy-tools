using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Applies the default GraphView edge-creation behavior while exposing hooks for host-specific drop handling.
    /// </summary>
    public sealed class GraphEdgeConnectorListener : IEdgeConnectorListener
    {
        private readonly Action<Edge> _cleanupInvalidEdge;
        private readonly Action<Edge, Vector2> _dropOutsidePortHandler;
        private readonly GraphView _graphView;

        /// <summary>
        /// Creates one reusable edge connector listener for one graph view.
        /// </summary>
        /// <param name="graphView">Graph view that owns the edge-authoring interaction.</param>
        /// <param name="dropOutsidePortHandler">
        /// Host-specific callback invoked when one edge is dropped outside a compatible port.
        /// </param>
        /// <param name="cleanupInvalidEdge">
        /// Host-specific callback used to dispose transient edges that should not remain in the view.
        /// </param>
        public GraphEdgeConnectorListener(
            GraphView graphView,
            Action<Edge, Vector2> dropOutsidePortHandler,
            Action<Edge> cleanupInvalidEdge)
        {
            _graphView = graphView;
            _dropOutsidePortHandler = dropOutsidePortHandler;
            _cleanupInvalidEdge = cleanupInvalidEdge;
        }

        /// <inheritdoc />
        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            if (_dropOutsidePortHandler != null)
            {
                _dropOutsidePortHandler(edge, position);
                return;
            }

            _cleanupInvalidEdge?.Invoke(edge);
        }

        /// <inheritdoc />
        public void OnDrop(GraphView graphView, Edge edge)
        {
            if (edge?.input == null || edge.output == null)
            {
                _cleanupInvalidEdge?.Invoke(edge);
                return;
            }

            List<GraphElement> elementsToRemove = new();

            if (edge.input.capacity == Port.Capacity.Single)
            {
                elementsToRemove.AddRange(
                    edge.input.connections
                        .Where(existingEdge => existingEdge != edge)
                        .Cast<GraphElement>());
            }

            if (edge.output.capacity == Port.Capacity.Single)
            {
                elementsToRemove.AddRange(
                    edge.output.connections
                        .Where(existingEdge => existingEdge != edge)
                        .Cast<GraphElement>());
            }

            GraphViewChange graphViewChange = new()
            {
                edgesToCreate = new List<Edge> { edge },
                elementsToRemove = elementsToRemove.Count > 0
                    ? elementsToRemove.Distinct().ToList()
                    : null,
            };

            _graphView?.graphViewChanged?.Invoke(graphViewChange);
        }
    }
}