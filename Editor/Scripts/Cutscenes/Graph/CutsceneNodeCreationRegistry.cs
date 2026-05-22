using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.ConversationsModule;
using IndieGabo.HandyTools.Editor.GraphCore;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Graph
{
    public static class CutsceneNodeCreationRegistry
    {
        private static IReadOnlyList<NodeDescriptor> _descriptors;

        public readonly struct NodeDescriptor
        {
            public NodeDescriptor(
                Type nodeType,
                string menuPath,
                bool requiresDialogueSystem,
                bool requiresConversationsModule)
            {
                NodeType = nodeType;
                MenuPath = menuPath;
                RequiresDialogueSystem = requiresDialogueSystem;
                RequiresConversationsModule = requiresConversationsModule;
            }

            public Type NodeType { get; }

            public string MenuPath { get; }

            public bool RequiresDialogueSystem { get; }

            public bool RequiresConversationsModule { get; }

            public string DisplayName => MenuPath.Split('/').LastOrDefault() ?? NodeType.Name;

            public Texture2D Icon => CutsceneNodePresentationRegistry.GetMetadata(NodeType).Icon;
        }

        public static IReadOnlyList<NodeDescriptor> Descriptors =>
            _descriptors ??= BuildDescriptors();

        /// <summary>
        /// Gets the shared family-scoped descriptors exposed through GraphCore.Editor.
        /// </summary>
        public static IReadOnlyList<GraphFamilyNodeCatalogRegistry.Descriptor> FamilyDescriptors
        {
            get
            {
                _ = Descriptors;
                return GraphFamilyNodeCatalogRegistry.GetDescriptors(CutsceneGraphFamily.Id);
            }
        }

        public static IReadOnlyList<NodeDescriptor> GetCreateableDescriptors()
        {
            bool isDialogueSystemAvailable = DialogueSystemIntegrationAvailability.IsAvailable();
            bool isConversationsModuleActive = ConversationsModuleDefinition.IsActive;

            return Descriptors
                .Where(descriptor =>
                    (!descriptor.RequiresDialogueSystem || isDialogueSystemAvailable)
                    && (!descriptor.RequiresConversationsModule || isConversationsModuleActive))
                .ToList();
        }

        public static CutsceneNodeBase CreateNode(Type nodeType)
        {
            return GraphAttributedNodeCatalogUtility.CreateNode<CutsceneNodeBase>(nodeType);
        }

        private static IReadOnlyList<NodeDescriptor> BuildDescriptors()
        {
            CutsceneGraphFamily.Register();

            List<NodeDescriptor> descriptors = GraphAttributedNodeCatalogUtility
                .Discover<CutsceneNodeBase, CutsceneNodeMenuAttribute>()
                .Select(descriptor => new NodeDescriptor(
                    descriptor.NodeType,
                    descriptor.Attribute.MenuPath,
                    descriptor.Attribute.RequiresDialogueSystem,
                    descriptor.Attribute.RequiresConversationsModule))
                .ToList();

            GraphFamilyNodeCatalogRegistry.RegisterRange(
                descriptors.Select(descriptor =>
                    new GraphFamilyNodeCatalogRegistry.Descriptor(
                        CutsceneGraphFamily.Id,
                        descriptor.NodeType,
                        descriptor.MenuPath)));

            return descriptors;
        }
    }
}