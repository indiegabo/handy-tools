using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.Editor.GraphCore;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Graph
{
    /// <summary>
    /// Builds the family-scoped Conversations node catalog exposed through GraphCore.
    /// </summary>
    public static class ConversationNodeCreationRegistry
    {
        private static IReadOnlyList<NodeDescriptor> _descriptors;

        /// <summary>
        /// Describes one createable Conversations node entry.
        /// </summary>
        public readonly struct NodeDescriptor
        {
            /// <summary>
            /// Initializes one Conversations node descriptor.
            /// </summary>
            /// <param name="nodeType">Concrete node type exposed to creation UIs.</param>
            /// <param name="menuPath">Menu path shown to the user.</param>
            public NodeDescriptor(Type nodeType, string menuPath)
            {
                NodeType = nodeType;
                MenuPath = menuPath;
            }

            /// <summary>
            /// Gets the concrete node type exposed to creation UIs.
            /// </summary>
            public Type NodeType { get; }

            /// <summary>
            /// Gets the menu path shown to the user.
            /// </summary>
            public string MenuPath { get; }

            /// <summary>
            /// Gets the short display name resolved from the menu path.
            /// </summary>
            public string DisplayName => MenuPath.Split('/').LastOrDefault() ?? NodeType.Name;
        }

        /// <summary>
        /// Gets the Conversations-authored node descriptors.
        /// </summary>
        public static IReadOnlyList<NodeDescriptor> Descriptors =>
            _descriptors ??= BuildDescriptors();

        /// <summary>
        /// Gets the GraphCore family-scoped catalog descriptors exposed by Conversations.
        /// </summary>
        public static IReadOnlyList<GraphFamilyNodeCatalogRegistry.Descriptor> FamilyDescriptors
        {
            get
            {
                _ = Descriptors;
                return GraphFamilyNodeCatalogRegistry.GetDescriptors(ConversationGraphFamily.Id);
            }
        }

        /// <summary>
        /// Creates one authored node instance for the requested Conversations type.
        /// </summary>
        /// <param name="nodeType">Concrete node type to instantiate.</param>
        /// <returns>The created node instance when the type is valid.</returns>
        public static ConversationNodeBase CreateNode(Type nodeType)
        {
            return GraphAttributedNodeCatalogUtility.CreateNode<ConversationNodeBase>(nodeType);
        }

        /// <summary>
        /// Scans the runtime assembly and registers the family-scoped Conversations catalog.
        /// </summary>
        /// <returns>The discovered node descriptors.</returns>
        private static IReadOnlyList<NodeDescriptor> BuildDescriptors()
        {
            ConversationGraphFamily.Register();

            List<NodeDescriptor> descriptors = GraphAttributedNodeCatalogUtility
                .Discover<ConversationNodeBase, ConversationNodeMenuAttribute>()
                .Select(descriptor => new NodeDescriptor(
                    descriptor.NodeType,
                    descriptor.Attribute.MenuPath))
                .ToList();

            GraphFamilyNodeCatalogRegistry.RegisterRange(
                descriptors.Select(descriptor =>
                    new GraphFamilyNodeCatalogRegistry.Descriptor(
                        ConversationGraphFamily.Id,
                        descriptor.NodeType,
                        descriptor.MenuPath)));

            return descriptors;
        }
    }
}