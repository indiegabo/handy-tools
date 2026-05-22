using System;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.ConversationsModule.Core
{
    /// <summary>
    /// Stores one Conversations-authored graph on top of the shared GraphCore model.
    /// </summary>
    [Serializable]
    public sealed class ConversationGraph : GraphDefinition
    {
        /// <summary>
        /// Determines whether the graph already owns one entry-compatible node.
        /// </summary>
        /// <returns>True when one entry node already exists.</returns>
        public bool HasEntryNode()
        {
            for (int index = 0; index < Nodes.Count; index++)
            {
                if (IsEntryNode(Nodes[index]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Ensures the graph owns one entry-compatible node.
        /// </summary>
        /// <returns>True when a new entry node was created.</returns>
        public bool EnsureEntryNode()
        {
            if (HasEntryNode())
            {
                return false;
            }

            AddNode(new ConversationEntryNode());
            return true;
        }

        /// <summary>
        /// Determines whether one authored node can act as the conversation entry.
        /// </summary>
        /// <param name="node">Authored node that should be classified.</param>
        /// <returns>True when the node is one supported entry-node shape.</returns>
        public static bool IsEntryNode(GraphNodeBase node)
        {
            return node is ConversationEntryNode;
        }

        /// <summary>
        /// Creates one minimal authored graph that proves the asset-owned host path.
        /// </summary>
        /// <returns>The default authored conversation graph.</returns>
        public static ConversationGraph CreateDefault()
        {
            ConversationGraph graph = new();
            ConversationEntryNode entryNode = new();
            graph.AddNode(entryNode);

            return graph;
        }
    }
}