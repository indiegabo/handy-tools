using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Flow;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [Serializable]
    public sealed class CutsceneGraph : GraphDefinition, ISerializationCallbackReceiver
    {
        private readonly CutsceneTypedReadOnlyListAdapter<GraphNodeBase, CutsceneNodeBase>
            _nodesView;
        private readonly CutsceneTypedReadOnlyListAdapter<GraphConnection, CutsceneConnection>
            _connectionsView;

        public CutsceneGraph()
        {
            EnsureBlackboardShape();
            _nodesView = new CutsceneTypedReadOnlyListAdapter<GraphNodeBase, CutsceneNodeBase>(
                () => base.Nodes);
            _connectionsView = new CutsceneTypedReadOnlyListAdapter<GraphConnection, CutsceneConnection>(
                () => base.Connections);
        }

        public new IReadOnlyList<CutsceneNodeBase> Nodes
        {
            get
            {
                NormalizeGraphShapes();
                return _nodesView;
            }
        }

        public new IReadOnlyList<CutsceneConnection> Connections
        {
            get
            {
                NormalizeGraphShapes();
                return _connectionsView;
            }
        }

        public new CutsceneGraphBlackboard Blackboard
        {
            get
            {
                NormalizeGraphShapes();
                return EnsureBlackboardShape();
            }
        }

        /// <inheritdoc />
        protected override GraphBlackboard CreateBlackboard()
        {
            return new CutsceneGraphBlackboard();
        }

        /// <inheritdoc />
        protected override GraphConnection CreateConnection(
            SerializableGuid fromNodeId,
            string outputKey,
            SerializableGuid toNodeId)
        {
            return new CutsceneConnection(fromNodeId, outputKey, toNodeId);
        }

        /// <summary>
        /// Restores the blackboard instance during migration or import flows.
        /// </summary>
        /// <param name="blackboard">Blackboard instance that should back the graph.</param>
        public void RestoreBlackboard(CutsceneGraphBlackboard blackboard)
        {
            base.RestoreBlackboard(blackboard ?? new CutsceneGraphBlackboard());
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            EnsureBlackboardShape();
            NormalizeGraphShapes();
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            EnsureBlackboardShape();
            NormalizeGraphShapes();
        }

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

        public new void Clear()
        {
            base.Clear();
        }

        public new void EnsureNodeIds()
        {
            NormalizeGraphShapes();
            base.EnsureNodeIds();
        }

        public void AddNode(CutsceneNodeBase node)
        {
            base.AddNode(node);
        }

        public new bool RemoveNode(SerializableGuid nodeId)
        {
            return base.RemoveNode(nodeId);
        }

        /// <summary>
        /// Adds one serialized connection without replacing existing entries for the
        /// same output key. This is intended for migration, import, and validation
        /// flows that must preserve authored graph state exactly as stored.
        /// </summary>
        /// <param name="connection">Connection to append.</param>
        public void AddConnection(CutsceneConnection connection)
        {
            base.AddConnection(connection);
        }

        public bool TryGetNode(SerializableGuid nodeId, out CutsceneNodeBase node)
        {
            NormalizeGraphShapes();
            node = null;

            if (!base.TryGetNode(nodeId, out GraphNodeBase candidate)
                || candidate is not CutsceneNodeBase cutsceneNode)
            {
                return false;
            }

            node = cutsceneNode;
            return true;
        }

        public new T CreateNode<T>() where T : CutsceneNodeBase, new()
        {
            T node = new();
            AddNode(node);
            return node;
        }

        public CutsceneEntryNode GetEntryNode()
        {
            return Nodes.OfType<CutsceneEntryNode>().FirstOrDefault();
        }

        public new IEnumerable<CutsceneConnection> GetOutgoingConnections(SerializableGuid nodeId)
        {
            NormalizeGraphShapes();
            return base.GetOutgoingConnections(nodeId).Cast<CutsceneConnection>();
        }

        public bool TryGetOutgoingConnection(SerializableGuid nodeId, string outputKey, out CutsceneConnection connection)
        {
            NormalizeGraphShapes();
            connection = null;

            if (!base.TryGetOutgoingConnection(nodeId, outputKey, out GraphConnection candidate)
                || candidate is not CutsceneConnection cutsceneConnection)
            {
                return false;
            }

            connection = cutsceneConnection;
            return true;
        }

        private void NormalizeGraphShapes()
        {
            EnsureBlackboardShape();

            for (int index = 0; index < NodesInternal.Count; index++)
            {
                GraphNodeBase node = NodesInternal[index];

                if (node == null || node is CutsceneNodeBase)
                {
                    continue;
                }

                CutsceneNodeBase normalizedNode =
                    CutsceneGraphCoreRuntimeMigrationUtility.CreateCutsceneAuthoringNode(node);

                if (normalizedNode != null)
                {
                    NodesInternal[index] = normalizedNode;
                }
            }

            for (int index = 0; index < ConnectionsInternal.Count; index++)
            {
                GraphConnection connection = ConnectionsInternal[index];

                if (connection == null || connection is CutsceneConnection)
                {
                    continue;
                }

                CutsceneConnection normalizedConnection = new(
                    connection.FromNodeId,
                    connection.OutputKey,
                    connection.ToNodeId);

                if (connection.HasCustomColor)
                {
                    normalizedConnection.SetCustomColor(connection.CustomColor);
                }

                ConnectionsInternal[index] = normalizedConnection;
            }
        }

        private CutsceneGraphBlackboard EnsureBlackboardShape()
        {
            if (BlackboardInternal is CutsceneGraphBlackboard cutsceneBlackboard)
            {
                return cutsceneBlackboard;
            }

            CutsceneGraphBlackboard promotedBlackboard =
                CutsceneGraphCoreRuntimeMigrationUtility.CreateCutsceneGraphBlackboard(
                    BlackboardInternal ?? base.Blackboard);
            base.RestoreBlackboard(promotedBlackboard);
            return promotedBlackboard;
        }
    }
}