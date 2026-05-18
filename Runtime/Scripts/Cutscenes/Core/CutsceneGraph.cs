using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Flow;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [Serializable]
    public sealed class CutsceneGraph
    {
        [SerializeReference] private List<CutsceneNodeBase> _nodes = new();
        [SerializeField] private List<CutsceneConnection> _connections = new();
        [SerializeField] private CutsceneGraphBlackboard _blackboard = new();

        public IReadOnlyList<CutsceneNodeBase> Nodes => _nodes;

        public IReadOnlyList<CutsceneConnection> Connections => _connections;

        public CutsceneGraphBlackboard Blackboard => _blackboard ??= new CutsceneGraphBlackboard();

        public static CutsceneGraph CreateDefault()
        {
            CutsceneGraph graph = new();
            CutsceneEntryNode entryNode = new();
            CutsceneFinishNode finishNode = new();
            finishNode.Position = new Vector2(280f, 0f);

            graph.AddNode(entryNode);
            graph.AddNode(finishNode);
            graph.Connect(entryNode.Id, CutsceneNodePorts.Next, finishNode.Id);

            return graph;
        }

        public void Clear()
        {
            _nodes.Clear();
            _connections.Clear();
            _blackboard?.Clear();
        }

        public void EnsureNodeIds()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                _nodes[i]?.EnsureId();
            }

            _blackboard?.EnsureEntryIds();
        }

        public void AddNode(CutsceneNodeBase node)
        {
            if (node == null)
            {
                return;
            }

            node.EnsureId();
            _nodes.Add(node);
        }

        public bool RemoveNode(SerializableGuid nodeId)
        {
            int removedNodes = _nodes.RemoveAll(node => node != null && node.Id == nodeId);
            int removedConnections = _connections.RemoveAll(connection => connection.FromNodeId == nodeId || connection.ToNodeId == nodeId);
            return removedNodes > 0 || removedConnections > 0;
        }

        public void Connect(SerializableGuid fromNodeId, string outputKey, SerializableGuid toNodeId)
        {
            CutsceneConnection existingConnection = _connections.FirstOrDefault(
                connection => connection.FromNodeId == fromNodeId
                    && string.Equals(connection.OutputKey, outputKey, StringComparison.OrdinalIgnoreCase));

            if (existingConnection != null)
            {
                existingConnection.SetTarget(toNodeId);
                return;
            }

            _connections.Add(new CutsceneConnection(fromNodeId, outputKey, toNodeId));
        }

        public void Disconnect(SerializableGuid fromNodeId, string outputKey)
        {
            _connections.RemoveAll(connection => connection.FromNodeId == fromNodeId
                && string.Equals(connection.OutputKey, outputKey, StringComparison.OrdinalIgnoreCase));
        }

        public bool TryGetNode(SerializableGuid nodeId, out CutsceneNodeBase node)
        {
            node = _nodes.FirstOrDefault(candidate => candidate != null && candidate.Id == nodeId);
            return node != null;
        }

        public T CreateNode<T>() where T : CutsceneNodeBase, new()
        {
            T node = new();
            AddNode(node);
            return node;
        }

        public CutsceneEntryNode GetEntryNode()
        {
            return _nodes.OfType<CutsceneEntryNode>().FirstOrDefault();
        }

        public IEnumerable<CutsceneConnection> GetOutgoingConnections(SerializableGuid nodeId)
        {
            return _connections.Where(connection => connection.FromNodeId == nodeId);
        }

        public bool TryGetOutgoingConnection(SerializableGuid nodeId, string outputKey, out CutsceneConnection connection)
        {
            connection = _connections.FirstOrDefault(candidate => candidate.FromNodeId == nodeId
                && string.Equals(candidate.OutputKey, outputKey, StringComparison.OrdinalIgnoreCase));

            return connection != null;
        }

        public bool TrySetConnectionColor(
            SerializableGuid fromNodeId,
            string outputKey,
            Color color)
        {
            if (!TryGetOutgoingConnection(fromNodeId, outputKey, out CutsceneConnection connection))
            {
                return false;
            }

            connection.SetCustomColor(color);
            return true;
        }

        public bool TryClearConnectionColor(SerializableGuid fromNodeId, string outputKey)
        {
            if (!TryGetOutgoingConnection(fromNodeId, outputKey, out CutsceneConnection connection))
            {
                return false;
            }

            connection.ClearCustomColor();
            return true;
        }

        public bool HasIncomingConnections(SerializableGuid nodeId)
        {
            return _connections.Any(connection => connection.ToNodeId == nodeId);
        }
    }
}