using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Graph
{
    public static class CutsceneNodeCreationRegistry
    {
        private static IReadOnlyList<NodeDescriptor> _descriptors;

        public readonly struct NodeDescriptor
        {
            public NodeDescriptor(Type nodeType, string menuPath, bool requiresDialogueSystem)
            {
                NodeType = nodeType;
                MenuPath = menuPath;
                RequiresDialogueSystem = requiresDialogueSystem;
            }

            public Type NodeType { get; }

            public string MenuPath { get; }

            public bool RequiresDialogueSystem { get; }

            public string DisplayName => MenuPath.Split('/').LastOrDefault() ?? NodeType.Name;

            public Texture2D Icon => CutsceneNodePresentationRegistry.GetMetadata(NodeType).Icon;
        }

        public static IReadOnlyList<NodeDescriptor> Descriptors =>
            _descriptors ??= BuildDescriptors();

        public static IReadOnlyList<NodeDescriptor> GetCreateableDescriptors()
        {
            bool isDialogueSystemAvailable = DialogueSystemIntegrationAvailability.IsAvailable();

            return Descriptors
                .Where(descriptor => !descriptor.RequiresDialogueSystem || isDialogueSystemAvailable)
                .ToList();
        }

        public static CutsceneNodeBase CreateNode(Type nodeType)
        {
            if (nodeType == null || !typeof(CutsceneNodeBase).IsAssignableFrom(nodeType))
            {
                return null;
            }

            try
            {
                return Activator.CreateInstance(nodeType, true) as CutsceneNodeBase;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        private static IReadOnlyList<NodeDescriptor> BuildDescriptors()
        {
            return TypeCache.GetTypesWithAttribute<CutsceneNodeMenuAttribute>()
                .Where(type => type != null
                    && !type.IsAbstract
                    && typeof(CutsceneNodeBase).IsAssignableFrom(type))
                .Select(type =>
                {
                    CutsceneNodeMenuAttribute attribute =
                        type.GetCustomAttributes(typeof(CutsceneNodeMenuAttribute), false)
                            .OfType<CutsceneNodeMenuAttribute>()
                            .First();

                    return new NodeDescriptor(
                        type,
                        attribute.MenuPath,
                        attribute.RequiresDialogueSystem);
                })
                .OrderBy(descriptor => descriptor.MenuPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}