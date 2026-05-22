using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndieGabo.HandyTools.ConversationsModule;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.GraphCore.Validation;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Actions;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Flow;
using IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem;
using IndieGabo.HandyTools.Utils;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Validation
{
    public static class CutsceneGraphValidator
    {
        public static IReadOnlyList<CutsceneGraphValidationIssue> Validate(CutsceneDirector director)
        {
            if (director == null)
            {
                return Array.Empty<CutsceneGraphValidationIssue>();
            }

            return Validate(
                director.Graph,
                DialogueSystemIntegrationAvailability.IsAvailable(),
                ConversationsModuleDefinition.IsActive);
        }

        public static IReadOnlyList<CutsceneGraphValidationIssue> Validate(
            CutsceneGraph graph,
            bool isDialogueSystemAvailable,
            bool isConversationsModuleActive)
        {
            List<CutsceneGraphValidationIssue> issues = new();

            if (graph == null)
            {
                issues.Add(new CutsceneGraphValidationIssue(
                    CutsceneGraphValidationSeverity.Error,
                    "The cutscene graph is missing.",
                    SerializableGuid.Empty));
                return issues;
            }

            List<CutsceneNodeBase> nodes = graph.Nodes.Where(node => node != null).ToList();
            List<CutsceneNodeBase> topologyNodes = nodes
                .Where(node => node.ParticipatesInTopologyValidation)
                .ToList();

            if (nodes.Count == 0)
            {
                issues.Add(new CutsceneGraphValidationIssue(
                    CutsceneGraphValidationSeverity.Error,
                    "The cutscene graph does not contain any nodes.",
                    SerializableGuid.Empty));
                return issues;
            }

            AppendSharedTopologyIssues(graph, issues);
            AppendValueBranchIssues(topologyNodes, issues);
            AppendReachabilityIssues(graph, topologyNodes, issues);
            AppendNullReferenceIssues(nodes, issues);
            AppendBlackboardBindingIssues(graph.Blackboard, nodes, issues);
            AppendDialogueAvailabilityIssues(nodes, isDialogueSystemAvailable, issues);
            AppendConversationsModuleAvailabilityIssues(
                nodes,
                isConversationsModuleActive,
                issues);

            return issues;
        }

        private static void AppendBlackboardBindingIssues(
            CutsceneGraphBlackboard blackboard,
            IReadOnlyList<CutsceneNodeBase> nodes,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                AppendSerializedBindingIssues(
                    nodes[index],
                    blackboard,
                    nodes[index],
                    string.Empty,
                    issues);
            }
        }

        private static void AppendSharedTopologyIssues(
            CutsceneGraph graph,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            GraphDefinition validationGraph = BuildTopologyValidationGraph(graph);
            GraphTopologyValidationProfile profile = new()
            {
                GraphDisplayName = "cutscene graph",
                RootNodeDisplayName = "Entry",
                RequireRootNode = true,
                AllowMultipleRootNodes = false,
                DetectUnreachableNodes = false,
                DetectOrphanNodes = false,
                DetectFamilyMismatch = false,
                IsRootNode = node => node is CutsceneTopologyValidationNode adaptedNode
                    && adaptedNode.Source is CutsceneEntryNode,
                ShouldValidateNode = node => node is CutsceneTopologyValidationNode adaptedNode
                    && adaptedNode.Source.ParticipatesInTopologyValidation,
            };

            IReadOnlyList<GraphValidationIssue> topologyIssues =
                GraphTopologyValidator.Validate(validationGraph, profile);

            for (int index = 0; index < topologyIssues.Count; index++)
            {
                GraphValidationIssue issue = topologyIssues[index];
                issues.Add(new CutsceneGraphValidationIssue(
                    ConvertSeverity(issue.Severity),
                    issue.Message,
                    issue.NodeId));
            }
        }

        private static GraphDefinition BuildTopologyValidationGraph(CutsceneGraph graph)
        {
            GraphDefinition validationGraph = new();

            for (int index = 0; index < graph.Nodes.Count; index++)
            {
                CutsceneNodeBase node = graph.Nodes[index];

                if (node == null)
                {
                    continue;
                }

                validationGraph.AddNode(
                    new CutsceneTopologyValidationNode(node),
                    preserveId: true);
            }

            for (int index = 0; index < graph.Connections.Count; index++)
            {
                CutsceneConnection connection = graph.Connections[index];

                if (connection == null)
                {
                    continue;
                }

                validationGraph.AddConnection(new GraphConnection(
                    connection.FromNodeId,
                    connection.OutputKey,
                    connection.ToNodeId));
            }

            return validationGraph;
        }

        private static CutsceneGraphValidationSeverity ConvertSeverity(
            GraphValidationSeverity severity)
        {
            return severity switch
            {
                GraphValidationSeverity.Info => CutsceneGraphValidationSeverity.Info,
                GraphValidationSeverity.Warning => CutsceneGraphValidationSeverity.Warning,
                _ => CutsceneGraphValidationSeverity.Error,
            };
        }

        private static void AppendDuplicateIdIssues(
            IReadOnlyList<CutsceneNodeBase> nodes,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            HashSet<SerializableGuid> seenIds = new();

            for (int index = 0; index < nodes.Count; index++)
            {
                CutsceneNodeBase node = nodes[index];

                if (node.Id == SerializableGuid.Empty)
                {
                    issues.Add(new CutsceneGraphValidationIssue(
                        CutsceneGraphValidationSeverity.Error,
                        $"Node '{node.DisplayTitle}' has an empty id.",
                        node.Id));
                    continue;
                }

                if (!seenIds.Add(node.Id))
                {
                    issues.Add(new CutsceneGraphValidationIssue(
                        CutsceneGraphValidationSeverity.Error,
                        $"Node '{node.DisplayTitle}' duplicates an existing node id.",
                        node.Id));
                }
            }
        }

        private static void AppendEntryIssues(
            IReadOnlyList<CutsceneNodeBase> nodes,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            List<CutsceneEntryNode> entryNodes = nodes.OfType<CutsceneEntryNode>().ToList();

            if (entryNodes.Count == 0)
            {
                issues.Add(new CutsceneGraphValidationIssue(
                    CutsceneGraphValidationSeverity.Error,
                    "The cutscene graph does not contain an Entry node.",
                    SerializableGuid.Empty));
                return;
            }

            if (entryNodes.Count > 1)
            {
                issues.Add(new CutsceneGraphValidationIssue(
                    CutsceneGraphValidationSeverity.Warning,
                    "The cutscene graph contains more than one Entry node. Only the first entry node will drive execution.",
                    entryNodes[1].Id));
            }
        }

        private static void AppendMandatoryConnectionIssues(
            CutsceneGraph graph,
            IReadOnlyList<CutsceneNodeBase> nodes,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                CutsceneNodeBase node = nodes[index];
                IReadOnlyList<CutsceneNodePort> outputPorts = node.GetOutputPorts();

                for (int portIndex = 0; portIndex < outputPorts.Count; portIndex++)
                {
                    CutsceneNodePort port = outputPorts[portIndex];

                    if (!port.IsMandatory)
                    {
                        continue;
                    }

                    if (!graph.TryGetOutgoingConnection(node.Id, port.Key, out _))
                    {
                        issues.Add(new CutsceneGraphValidationIssue(
                            CutsceneGraphValidationSeverity.Error,
                            $"Node '{node.DisplayTitle}' is missing a connection for mandatory output '{port.DisplayName}'.",
                            node.Id));
                    }
                }
            }
        }

        private static void AppendValueBranchIssues(
            IReadOnlyList<CutsceneNodeBase> nodes,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            foreach (CutsceneValueBranchNode node in nodes.OfType<CutsceneValueBranchNode>())
            {
                if (node.Branches == null || node.Branches.Count == 0)
                {
                    issues.Add(new CutsceneGraphValidationIssue(
                        CutsceneGraphValidationSeverity.Error,
                        $"Node '{node.DisplayTitle}' must define at least one branch output.",
                        node.Id));
                    continue;
                }

                HashSet<string> seenValues = new(
                    node.IgnoreCase
                        ? StringComparer.OrdinalIgnoreCase
                        : StringComparer.Ordinal);

                for (int index = 0; index < node.Branches.Count; index++)
                {
                    CutsceneValueBranchNode.BranchOption branch = node.Branches[index];

                    if (branch == null)
                    {
                        issues.Add(new CutsceneGraphValidationIssue(
                            CutsceneGraphValidationSeverity.Error,
                            $"Node '{node.DisplayTitle}' contains one null branch entry.",
                            node.Id));
                        continue;
                    }

                    if (!seenValues.Add(branch.MatchValue))
                    {
                        issues.Add(new CutsceneGraphValidationIssue(
                            CutsceneGraphValidationSeverity.Error,
                            $"Node '{node.DisplayTitle}' defines more than one output for value '{branch.MatchValue}'.",
                            node.Id));
                    }
                }
            }
        }

        private static void AppendReachabilityIssues(
            CutsceneGraph graph,
            IReadOnlyList<CutsceneNodeBase> nodes,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            CutsceneEntryNode entryNode = nodes.OfType<CutsceneEntryNode>().FirstOrDefault();

            if (entryNode == null)
            {
                return;
            }

            HashSet<SerializableGuid> reachableNodeIds = GetReachableNodeIds(graph, entryNode.Id);
            List<CutsceneFinishNode> finishNodes = nodes.OfType<CutsceneFinishNode>().ToList();

            if (finishNodes.Count == 0)
            {
                issues.Add(new CutsceneGraphValidationIssue(
                    CutsceneGraphValidationSeverity.Warning,
                    "The cutscene graph does not contain a Finish node.",
                    SerializableGuid.Empty));
                return;
            }

            for (int index = 0; index < finishNodes.Count; index++)
            {
                CutsceneFinishNode finishNode = finishNodes[index];

                if (!reachableNodeIds.Contains(finishNode.Id))
                {
                    issues.Add(new CutsceneGraphValidationIssue(
                        CutsceneGraphValidationSeverity.Error,
                        $"Finish node '{finishNode.DisplayTitle}' is unreachable from Entry.",
                        finishNode.Id));
                }
            }
        }

        private static HashSet<SerializableGuid> GetReachableNodeIds(
            CutsceneGraph graph,
            SerializableGuid entryNodeId)
        {
            HashSet<SerializableGuid> reachableNodeIds = new();
            Queue<SerializableGuid> queue = new();
            queue.Enqueue(entryNodeId);

            while (queue.Count > 0)
            {
                SerializableGuid currentNodeId = queue.Dequeue();

                if (!reachableNodeIds.Add(currentNodeId))
                {
                    continue;
                }

                foreach (CutsceneConnection connection in graph.GetOutgoingConnections(currentNodeId))
                {
                    queue.Enqueue(connection.ToNodeId);
                }
            }

            return reachableNodeIds;
        }

        private static void AppendNullReferenceIssues(
            IReadOnlyList<CutsceneNodeBase> nodes,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                CutsceneNodeBase node = nodes[index];
                FieldInfo[] fields = node.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                {
                    FieldInfo field = fields[fieldIndex];

                    if (!IsSerializedField(field)
                        || !typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)
                        || IsOptionalSceneReferenceField(node, field))
                    {
                        continue;
                    }

                    if (!LooksLikeRequiredSceneReference(field))
                    {
                        continue;
                    }

                    if (field.GetValue(node) != null)
                    {
                        continue;
                    }

                    issues.Add(new CutsceneGraphValidationIssue(
                        CutsceneGraphValidationSeverity.Error,
                        $"Node '{node.DisplayTitle}' has a null scene reference for '{ObjectNames.NicifyVariableName(field.Name)}'.",
                        node.Id));
                }
            }
        }

        private static void AppendSerializedBindingIssues(
            object owner,
            CutsceneGraphBlackboard blackboard,
            CutsceneNodeBase node,
            string pathPrefix,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            if (owner == null)
            {
                return;
            }

            FieldInfo[] fields = owner.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
            {
                FieldInfo field = fields[fieldIndex];

                if (!IsSerializedField(field))
                {
                    continue;
                }

                string fieldLabel = BuildFieldLabel(pathPrefix, field.Name);

                if (field.FieldType == typeof(CutsceneValueSource))
                {
                    AppendValueSourceIssues(
                        node,
                        fieldLabel,
                        field.GetValue(owner) as CutsceneValueSource,
                        blackboard,
                        issues);
                    continue;
                }

                if (field.FieldType == typeof(CutsceneBlackboardVariableReference))
                {
                    AppendVariableReferenceIssues(
                        node,
                        fieldLabel,
                        field.GetValue(owner) as CutsceneBlackboardVariableReference,
                        blackboard,
                        issues,
                        null);
                    continue;
                }

                object fieldValue = field.GetValue(owner);

                if (fieldValue is IList list)
                {
                    AppendListBindingIssues(
                        list,
                        blackboard,
                        node,
                        fieldLabel,
                        issues);
                    continue;
                }

                if (fieldValue == null
                    || !ShouldInspectNestedSerializedObject(field.FieldType))
                {
                    continue;
                }

                AppendSerializedBindingIssues(
                    fieldValue,
                    blackboard,
                    node,
                    fieldLabel,
                    issues);
            }
        }

        private static void AppendListBindingIssues(
            IList list,
            CutsceneGraphBlackboard blackboard,
            CutsceneNodeBase node,
            string fieldLabel,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            for (int itemIndex = 0; itemIndex < list.Count; itemIndex++)
            {
                object item = list[itemIndex];

                if (item == null || !ShouldInspectNestedSerializedObject(item.GetType()))
                {
                    continue;
                }

                AppendSerializedBindingIssues(
                    item,
                    blackboard,
                    node,
                    $"{fieldLabel} {itemIndex + 1}",
                    issues);
            }
        }

        private static void AppendValueSourceIssues(
            CutsceneNodeBase node,
            string fieldLabel,
            CutsceneValueSource source,
            CutsceneGraphBlackboard blackboard,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            if (source == null)
            {
                issues.Add(new CutsceneGraphValidationIssue(
                    CutsceneGraphValidationSeverity.Error,
                    $"Node '{node.DisplayTitle}' is missing the value source for '{fieldLabel}'.",
                    node.Id));
                return;
            }

            if (source.Mode != CutsceneValueSourceMode.Blackboard)
            {
                return;
            }

            AppendVariableReferenceIssues(
                node,
                fieldLabel,
                source.BlackboardVariable,
                blackboard,
                issues,
                source.ExpectedValueType);
        }

        private static void AppendVariableReferenceIssues(
            CutsceneNodeBase node,
            string fieldLabel,
            CutsceneBlackboardVariableReference variableReference,
            CutsceneGraphBlackboard blackboard,
            ICollection<CutsceneGraphValidationIssue> issues,
            Type expectedType)
        {
            if (variableReference == null)
            {
                issues.Add(new CutsceneGraphValidationIssue(
                    CutsceneGraphValidationSeverity.Error,
                    $"Node '{node.DisplayTitle}' is missing the blackboard variable reference for '{fieldLabel}'.",
                    node.Id));
                return;
            }

            if (!variableReference.IsAssigned)
            {
                issues.Add(new CutsceneGraphValidationIssue(
                    CutsceneGraphValidationSeverity.Error,
                    $"Node '{node.DisplayTitle}' has no blackboard variable assigned for '{fieldLabel}'.",
                    node.Id));
                return;
            }

            string variableLabel = GetVariableLabel(variableReference);

            if (!TryResolveReferencedEntry(blackboard, variableReference, out CutsceneGraphBlackboardEntry entry)
                || entry?.Value == null)
            {
                issues.Add(new CutsceneGraphValidationIssue(
                    CutsceneGraphValidationSeverity.Error,
                    $"Node '{node.DisplayTitle}' could not resolve blackboard variable '{variableLabel}' for '{fieldLabel}'.",
                    node.Id));
                return;
            }

            expectedType ??= variableReference.ValueType;

            if (expectedType == null || entry.Value.TryGetValue(expectedType, out _))
            {
                return;
            }

            Type actualType = entry.Value.GetExpectedValueType();
            issues.Add(new CutsceneGraphValidationIssue(
                CutsceneGraphValidationSeverity.Error,
                $"Node '{node.DisplayTitle}' binds '{fieldLabel}' to blackboard variable '{variableLabel}', but expects '{GetTypeLabel(expectedType)}' and found '{GetTypeLabel(actualType)}'.",
                node.Id));
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            return field.IsPublic || field.IsDefined(typeof(SerializeField), true);
        }

        private static string BuildFieldLabel(string pathPrefix, string fieldName)
        {
            string nicifiedName = ObjectNames.NicifyVariableName(fieldName);
            return string.IsNullOrWhiteSpace(pathPrefix)
                ? nicifiedName
                : $"{pathPrefix} / {nicifiedName}";
        }

        private static string GetVariableLabel(
            CutsceneBlackboardVariableReference variableReference)
        {
            if (!string.IsNullOrWhiteSpace(variableReference.EntryKey))
            {
                return variableReference.EntryKey;
            }

            return variableReference.EntryId == SerializableGuid.Empty
                ? "Unassigned"
                : variableReference.EntryId.ToHexString();
        }

        private static string GetTypeLabel(Type type)
        {
            return type?.Name ?? "Unknown";
        }

        private static bool TryResolveReferencedEntry(
            CutsceneGraphBlackboard blackboard,
            CutsceneBlackboardVariableReference variableReference,
            out CutsceneGraphBlackboardEntry entry)
        {
            entry = null;

            if (blackboard == null || variableReference == null)
            {
                return false;
            }

            if (variableReference.EntryId != SerializableGuid.Empty
                && blackboard.TryGetEntry(variableReference.EntryId, out entry))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(variableReference.EntryKey)
                && blackboard.TryGetEntry(variableReference.EntryKey, out entry);
        }

        private static bool ShouldInspectNestedSerializedObject(Type fieldType)
        {
            return fieldType != null
                && fieldType.IsClass
                && fieldType != typeof(string)
                && fieldType != typeof(CutsceneValueSource)
                && fieldType != typeof(CutsceneBlackboardVariableReference)
                && !typeof(UnityEngine.Object).IsAssignableFrom(fieldType);
        }

        private static bool LooksLikeRequiredSceneReference(FieldInfo field)
        {
            Type fieldType = field.FieldType;

            if (fieldType == typeof(GameObject)
                || fieldType == typeof(Transform)
                || typeof(Component).IsAssignableFrom(fieldType)
                || typeof(Behaviour).IsAssignableFrom(fieldType)
                || fieldType == typeof(Animator))
            {
                return true;
            }

            string fieldName = field.Name;
            return fieldName.IndexOf("target", StringComparison.OrdinalIgnoreCase) >= 0
                || fieldName.IndexOf("animator", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsOptionalSceneReferenceField(CutsceneNodeBase node, FieldInfo field)
        {
            return node is CutsceneDialogueConversationNode
                && (field.Name == "_speaker" || field.Name == "_listener");
        }

        private static void AppendDialogueAvailabilityIssues(
            IReadOnlyList<CutsceneNodeBase> nodes,
            bool isDialogueSystemAvailable,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            if (isDialogueSystemAvailable
                || nodes.All(node => node is not CutsceneDialogueConversationNode))
            {
                return;
            }

            issues.Add(new CutsceneGraphValidationIssue(
                CutsceneGraphValidationSeverity.Warning,
                "The graph contains Dialogue System conversation nodes, but Dialogue System is not currently available in this project.",
                SerializableGuid.Empty));
        }

        private static void AppendConversationsModuleAvailabilityIssues(
            IReadOnlyList<CutsceneNodeBase> nodes,
            bool isConversationsModuleActive,
            ICollection<CutsceneGraphValidationIssue> issues)
        {
            if (isConversationsModuleActive
                || nodes.All(node => node is not CutsceneConversationReferenceNode))
            {
                return;
            }

            issues.Add(new CutsceneGraphValidationIssue(
                CutsceneGraphValidationSeverity.Warning,
                "The graph contains Conversations Start Conversation nodes, but the Conversations module is not currently active.",
                SerializableGuid.Empty));
        }

        private sealed class CutsceneTopologyValidationNode : GraphNodeBase
        {
            private readonly IReadOnlyList<GraphPortDefinition> _outputPorts;

            public CutsceneTopologyValidationNode(CutsceneNodeBase source)
            {
                Source = source ?? throw new ArgumentNullException(nameof(source));
                RestoreId(source.Id);
                Title = source.DisplayTitle;
                Position = source.Position;
                _outputPorts = source.GetOutputPorts()
                    .Where(port => port != null)
                    .Select(port => new GraphPortDefinition(
                        port.Key,
                        port.DisplayName,
                        port.IsMandatory))
                    .ToList();
            }

            public CutsceneNodeBase Source { get; }

            public override bool HasInputPort => Source.HasInputPort;

            public override bool ParticipatesInAutoArrange => Source.ParticipatesInAutoArrange;

            public override bool ParticipatesInTopologyValidation =>
                Source.ParticipatesInTopologyValidation;

            public override bool UsesRuntimeStateStyling => Source.UsesRuntimeStateStyling;

            public override bool RequiresTick => Source.RequiresTick;

            public override IReadOnlyList<GraphPortDefinition> GetOutputPorts()
            {
                return _outputPorts;
            }

            public override string GetSummary()
            {
                return Source.GetSummary();
            }
        }
    }
}