using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.GraphCore
{
    /// <summary>
    /// Stores the shared serialized graph model made of nodes and connections.
    /// </summary>
    [Serializable]
    public class GraphDefinition
    {
        [SerializeReference] private List<GraphNodeBase> _nodes = new();
        [SerializeField] private List<GraphConnection> _connections = new();
        [SerializeField] private GraphBlackboard _blackboard = new();

        /// <summary>
        /// Gets the mutable serialized node collection for derived authored graph shells.
        /// </summary>
        protected List<GraphNodeBase> NodesInternal => _nodes;

        /// <summary>
        /// Gets the mutable serialized connection collection for derived authored graph shells.
        /// </summary>
        protected List<GraphConnection> ConnectionsInternal => _connections;

        /// <summary>
        /// Gets or sets the serialized graph-local blackboard for derived authored graph shells.
        /// </summary>
        protected GraphBlackboard BlackboardInternal
        {
            get => _blackboard;
            set => _blackboard = value;
        }

        /// <summary>
        /// Gets the serialized node collection.
        /// </summary>
        public IReadOnlyList<GraphNodeBase> Nodes => _nodes;

        /// <summary>
        /// Gets the serialized connection collection.
        /// </summary>
        public IReadOnlyList<GraphConnection> Connections => _connections;

        /// <summary>
        /// Gets the graph-local blackboard owned by the definition.
        /// </summary>
        public GraphBlackboard Blackboard => _blackboard ??= CreateBlackboard();

        /// <summary>
        /// Creates the authored blackboard container used by this graph definition.
        /// Derived graph families can override this to return family-specific shells
        /// while preserving the shared serialized shape.
        /// </summary>
        /// <returns>The authored graph-local blackboard instance.</returns>
        protected virtual GraphBlackboard CreateBlackboard()
        {
            return new GraphBlackboard();
        }

        /// <summary>
        /// Creates one authored connection instance for this graph definition.
        /// Derived graph families can override this to preserve family-specific
        /// connection shells while storing them through the shared serialized shape.
        /// </summary>
        /// <param name="fromNodeId">Origin node identifier.</param>
        /// <param name="outputKey">Origin output key.</param>
        /// <param name="toNodeId">Target node identifier.</param>
        /// <returns>The authored connection instance.</returns>
        protected virtual GraphConnection CreateConnection(
            SerializableGuid fromNodeId,
            string outputKey,
            SerializableGuid toNodeId)
        {
            return new GraphConnection(fromNodeId, outputKey, toNodeId);
        }

        /// <summary>
        /// Restores the graph-local blackboard instance during migration or import flows.
        /// </summary>
        /// <param name="blackboard">Blackboard instance that should back the definition.</param>
        public void RestoreBlackboard(GraphBlackboard blackboard)
        {
            _blackboard = blackboard ?? new GraphBlackboard();
        }

        /// <summary>
        /// Removes all nodes and connections from the graph.
        /// </summary>
        public void Clear()
        {
            _nodes.Clear();
            _connections.Clear();
            _blackboard?.Clear();
        }

        /// <summary>
        /// Ensures that all nodes own one stable identifier.
        /// </summary>
        public void EnsureNodeIds()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                _nodes[i]?.EnsureId();
            }

            _blackboard?.EnsureEntryIds();
        }

        /// <summary>
        /// Adds one node to the graph.
        /// </summary>
        /// <param name="node">Node to add.</param>
        public void AddNode(GraphNodeBase node)
        {
            AddNode(node, preserveId: false);
        }

        /// <summary>
        /// Adds one node to the graph and optionally preserves the current authored id.
        /// </summary>
        /// <param name="node">Node to add.</param>
        /// <param name="preserveId">
        /// True to preserve the current node id exactly as authored; false to
        /// ensure a non-empty id before insertion.
        /// </param>
        public void AddNode(GraphNodeBase node, bool preserveId)
        {
            if (node == null)
            {
                return;
            }

            if (!preserveId)
            {
                node.EnsureId();
            }

            _nodes.Add(node);
        }

        /// <summary>
        /// Removes one node and all incident connections.
        /// </summary>
        /// <param name="nodeId">Identifier of the node to remove.</param>
        /// <returns>True when one node or connection was removed.</returns>
        public bool RemoveNode(SerializableGuid nodeId)
        {
            int removedNodes = _nodes.RemoveAll(node => node != null && node.Id == nodeId);
            int removedConnections = _connections.RemoveAll(
                connection => connection.FromNodeId == nodeId || connection.ToNodeId == nodeId);

            return removedNodes > 0 || removedConnections > 0;
        }

        /// <summary>
        /// Creates or replaces one directed connection for the provided output.
        /// </summary>
        /// <param name="fromNodeId">Origin node identifier.</param>
        /// <param name="outputKey">Origin output key.</param>
        /// <param name="toNodeId">Target node identifier.</param>
        public void Connect(
            SerializableGuid fromNodeId,
            string outputKey,
            SerializableGuid toNodeId)
        {
            GraphConnection existingConnection = _connections.FirstOrDefault(
                connection => connection.FromNodeId == fromNodeId
                    && string.Equals(
                        connection.OutputKey,
                        outputKey,
                        StringComparison.OrdinalIgnoreCase));

            if (existingConnection != null)
            {
                existingConnection.SetTarget(toNodeId);
                return;
            }

            _connections.Add(CreateConnection(fromNodeId, outputKey, toNodeId));
        }

        /// <summary>
        /// Adds one serialized connection without replacing existing entries for the
        /// same output key. This is intended for migration, import, and validation
        /// flows that must preserve authored graph state exactly as stored.
        /// </summary>
        /// <param name="connection">Connection to append.</param>
        public void AddConnection(GraphConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            _connections.Add(connection);
        }

        /// <summary>
        /// Removes one outgoing connection for the provided output.
        /// </summary>
        /// <param name="fromNodeId">Origin node identifier.</param>
        /// <param name="outputKey">Origin output key.</param>
        public void Disconnect(SerializableGuid fromNodeId, string outputKey)
        {
            _connections.RemoveAll(
                connection => connection.FromNodeId == fromNodeId
                    && string.Equals(
                        connection.OutputKey,
                        outputKey,
                        StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Attempts to resolve one node by identifier.
        /// </summary>
        /// <param name="nodeId">Identifier to resolve.</param>
        /// <param name="node">Resolved node when found.</param>
        /// <returns>True when the node exists.</returns>
        public bool TryGetNode(SerializableGuid nodeId, out GraphNodeBase node)
        {
            node = _nodes.FirstOrDefault(candidate => candidate != null && candidate.Id == nodeId);
            return node != null;
        }

        /// <summary>
        /// Creates one node of the requested type and adds it to the graph.
        /// </summary>
        /// <typeparam name="T">Concrete node type to create.</typeparam>
        /// <returns>The newly created node instance.</returns>
        public T CreateNode<T>() where T : GraphNodeBase, new()
        {
            T node = new();
            AddNode(node);
            return node;
        }

        /// <summary>
        /// Enumerates the outgoing connections declared by one node.
        /// </summary>
        /// <param name="nodeId">Origin node identifier.</param>
        /// <returns>The matching outgoing connections.</returns>
        public IEnumerable<GraphConnection> GetOutgoingConnections(SerializableGuid nodeId)
        {
            return _connections.Where(connection => connection.FromNodeId == nodeId);
        }

        /// <summary>
        /// Attempts to resolve one outgoing connection by origin and output key.
        /// </summary>
        /// <param name="nodeId">Origin node identifier.</param>
        /// <param name="outputKey">Origin output key.</param>
        /// <param name="connection">Resolved connection when found.</param>
        /// <returns>True when the connection exists.</returns>
        public bool TryGetOutgoingConnection(
            SerializableGuid nodeId,
            string outputKey,
            out GraphConnection connection)
        {
            connection = _connections.FirstOrDefault(
                candidate => candidate.FromNodeId == nodeId
                    && string.Equals(
                        candidate.OutputKey,
                        outputKey,
                        StringComparison.OrdinalIgnoreCase));

            return connection != null;
        }

        /// <summary>
        /// Applies one custom authoring color to one outgoing connection.
        /// </summary>
        /// <param name="fromNodeId">Origin node identifier.</param>
        /// <param name="outputKey">Origin output key.</param>
        /// <param name="color">Color to apply.</param>
        /// <returns>True when the connection exists and was updated.</returns>
        public bool TrySetConnectionColor(
            SerializableGuid fromNodeId,
            string outputKey,
            Color color)
        {
            if (!TryGetOutgoingConnection(fromNodeId, outputKey, out GraphConnection connection))
            {
                return false;
            }

            connection.SetCustomColor(color);
            return true;
        }

        /// <summary>
        /// Clears one custom authoring color from one outgoing connection.
        /// </summary>
        /// <param name="fromNodeId">Origin node identifier.</param>
        /// <param name="outputKey">Origin output key.</param>
        /// <returns>True when the connection exists and was updated.</returns>
        public bool TryClearConnectionColor(SerializableGuid fromNodeId, string outputKey)
        {
            if (!TryGetOutgoingConnection(fromNodeId, outputKey, out GraphConnection connection))
            {
                return false;
            }

            connection.ClearCustomColor();
            return true;
        }

        /// <summary>
        /// Gets whether one node has any incoming connections.
        /// </summary>
        /// <param name="nodeId">Identifier of the node to inspect.</param>
        /// <returns>True when at least one incoming connection exists.</returns>
        public bool HasIncomingConnections(SerializableGuid nodeId)
        {
            return _connections.Any(connection => connection.ToNodeId == nodeId);
        }
    }
}