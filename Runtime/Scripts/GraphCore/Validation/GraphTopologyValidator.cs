using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.GraphCore.Validation
{
    /// <summary>
    /// Validates shared graph topology rules that are independent from one concrete graph family.
    /// </summary>
    public static class GraphTopologyValidator
    {
        /// <summary>
        /// Validates one graph using the default topology profile.
        /// </summary>
        /// <param name="graph">Graph to validate.</param>
        /// <returns>The issues emitted by the validation pass.</returns>
        public static IReadOnlyList<GraphValidationIssue> Validate(GraphDefinition graph)
        {
            return Validate(graph, null);
        }

        /// <summary>
        /// Validates one graph using the provided topology profile.
        /// </summary>
        /// <param name="graph">Graph to validate.</param>
        /// <param name="profile">Profile that configures the validation pass.</param>
        /// <returns>The issues emitted by the validation pass.</returns>
        public static IReadOnlyList<GraphValidationIssue> Validate(
            GraphDefinition graph,
            GraphTopologyValidationProfile profile)
        {
            profile ??= new GraphTopologyValidationProfile();

            List<GraphValidationIssue> issues = new();

            if (graph == null)
            {
                issues.Add(new GraphValidationIssue(
                    GraphValidationSeverity.Error,
                    $"The {NormalizeGraphLabel(profile.GraphDisplayName)} is missing.",
                    SerializableGuid.Empty));
                return issues;
            }

            List<GraphNodeBase> nodes = graph.Nodes.Where(node => node != null).ToList();

            if (nodes.Count == 0)
            {
                issues.Add(new GraphValidationIssue(
                    GraphValidationSeverity.Error,
                    $"The {NormalizeGraphLabel(profile.GraphDisplayName)} does not contain any nodes.",
                    SerializableGuid.Empty));
                return issues;
            }

            List<GraphNodeBase> topologyNodes = nodes
                .Where(node => ShouldValidateNode(node, profile))
                .ToList();

            AppendDuplicateIdIssues(nodes, issues);

            Dictionary<SerializableGuid, GraphNodeBase> nodesById = BuildNodeMap(nodes);

            AppendConnectionIssues(graph, nodesById, issues, profile);
            AppendMandatoryOutputIssues(graph, topologyNodes, issues, profile);
            AppendRootIssues(topologyNodes, issues, profile);
            AppendReachabilityIssues(graph, topologyNodes, issues, profile);
            AppendFamilyMismatchIssues(topologyNodes, issues, profile);

            profile.AppendSemanticIssues?.Invoke(graph, nodes, issues);

            return issues;
        }

        private static void AppendDuplicateIdIssues(
            IReadOnlyList<GraphNodeBase> nodes,
            ICollection<GraphValidationIssue> issues)
        {
            HashSet<SerializableGuid> seenIds = new();

            for (int index = 0; index < nodes.Count; index++)
            {
                GraphNodeBase node = nodes[index];

                if (node.Id == SerializableGuid.Empty)
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        $"Node '{node.DisplayTitle}' has an empty id.",
                        node.Id));
                    continue;
                }

                if (!seenIds.Add(node.Id))
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        $"Node '{node.DisplayTitle}' duplicates an existing node id.",
                        node.Id));
                }
            }
        }

        private static void AppendConnectionIssues(
            GraphDefinition graph,
            IReadOnlyDictionary<SerializableGuid, GraphNodeBase> nodesById,
            ICollection<GraphValidationIssue> issues,
            GraphTopologyValidationProfile profile)
        {
            HashSet<string> seenConnectionKeys = new(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < graph.Connections.Count; index++)
            {
                GraphConnection connection = graph.Connections[index];

                if (connection == null)
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        "The graph contains one null connection entry.",
                        SerializableGuid.Empty));
                    continue;
                }

                GraphNodeBase fromNode = null;
                bool hasSourceNode = nodesById.TryGetValue(connection.FromNodeId, out fromNode);
                bool hasTargetNode = nodesById.ContainsKey(connection.ToNodeId);

                if (profile.DetectMissingNodes && !hasSourceNode)
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        $"Connection from node id '{connection.FromNodeId}' references a missing source node.",
                        connection.FromNodeId));
                }

                if (profile.DetectMissingNodes && hasSourceNode && !hasTargetNode)
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        $"Node '{fromNode.DisplayTitle}' output '{NormalizeOutputKey(connection.OutputKey)}' references a missing target node '{connection.ToNodeId}'.",
                        fromNode.Id));
                }

                if (!hasSourceNode)
                {
                    continue;
                }

                string connectionKey = CreateConnectionKey(
                    connection.FromNodeId,
                    connection.OutputKey);

                if (profile.DetectConnectionMultiplicity
                    && !string.IsNullOrWhiteSpace(connection.OutputKey)
                    && !seenConnectionKeys.Add(connectionKey))
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        $"Node '{fromNode.DisplayTitle}' contains more than one connection for output '{connection.OutputKey}'.",
                        fromNode.Id));
                }

                IReadOnlyList<GraphPortDefinition> outputPorts = fromNode.GetOutputPorts();

                if (!profile.DetectUndeclaredOutputKeys)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(connection.OutputKey))
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        $"Node '{fromNode.DisplayTitle}' contains one connection with a blank output key.",
                        fromNode.Id));
                    continue;
                }

                bool isDeclaredOutput = outputPorts.Any(port =>
                    port != null
                    && string.Equals(
                        port.Key,
                        connection.OutputKey,
                        StringComparison.OrdinalIgnoreCase));

                if (!isDeclaredOutput)
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        $"Node '{fromNode.DisplayTitle}' declares no output named '{connection.OutputKey}', but the graph contains a connection for it.",
                        fromNode.Id));
                }
            }
        }

        private static void AppendMandatoryOutputIssues(
            GraphDefinition graph,
            IReadOnlyList<GraphNodeBase> nodes,
            ICollection<GraphValidationIssue> issues,
            GraphTopologyValidationProfile profile)
        {
            if (!profile.DetectMissingMandatoryOutputs)
            {
                return;
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                GraphNodeBase node = nodes[index];
                IReadOnlyList<GraphPortDefinition> outputPorts = node.GetOutputPorts();

                for (int portIndex = 0; portIndex < outputPorts.Count; portIndex++)
                {
                    GraphPortDefinition port = outputPorts[portIndex];

                    if (port == null || !port.IsMandatory)
                    {
                        continue;
                    }

                    if (!graph.TryGetOutgoingConnection(node.Id, port.Key, out _))
                    {
                        issues.Add(new GraphValidationIssue(
                            GraphValidationSeverity.Error,
                            $"Node '{node.DisplayTitle}' is missing a connection for mandatory output '{port.DisplayName}'.",
                            node.Id));
                    }
                }
            }
        }

        private static void AppendRootIssues(
            IReadOnlyList<GraphNodeBase> nodes,
            ICollection<GraphValidationIssue> issues,
            GraphTopologyValidationProfile profile)
        {
            List<GraphNodeBase> rootNodes = GetRootNodes(nodes, profile);

            if (profile.RequireRootNode && rootNodes.Count == 0)
            {
                issues.Add(new GraphValidationIssue(
                    GraphValidationSeverity.Error,
                    $"The {NormalizeGraphLabel(profile.GraphDisplayName)} does not contain a {NormalizeGraphLabel(profile.RootNodeDisplayName)} node.",
                    SerializableGuid.Empty));
                return;
            }

            if (!profile.AllowMultipleRootNodes && rootNodes.Count > 1)
            {
                issues.Add(new GraphValidationIssue(
                    GraphValidationSeverity.Warning,
                    $"The {NormalizeGraphLabel(profile.GraphDisplayName)} contains more than one {NormalizeGraphLabel(profile.RootNodeDisplayName)} node.",
                    rootNodes[1].Id));
            }
        }

        private static void AppendReachabilityIssues(
            GraphDefinition graph,
            IReadOnlyList<GraphNodeBase> nodes,
            ICollection<GraphValidationIssue> issues,
            GraphTopologyValidationProfile profile)
        {
            if (!profile.DetectUnreachableNodes && !profile.DetectOrphanNodes)
            {
                return;
            }

            List<GraphNodeBase> rootNodes = GetRootNodes(nodes, profile);

            if (rootNodes.Count == 0)
            {
                return;
            }

            HashSet<SerializableGuid> reachableNodeIds = GetReachableNodeIds(
                graph,
                rootNodes.Select(node => node.Id));

            for (int index = 0; index < nodes.Count; index++)
            {
                GraphNodeBase node = nodes[index];

                if (reachableNodeIds.Contains(node.Id))
                {
                    continue;
                }

                bool isOrphan = !graph.HasIncomingConnections(node.Id);

                if (isOrphan && profile.DetectOrphanNodes)
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Warning,
                        $"Node '{node.DisplayTitle}' is orphaned and cannot be reached from the configured {NormalizeGraphLabel(profile.RootNodeDisplayName)} nodes.",
                        node.Id));
                    continue;
                }

                if (profile.DetectUnreachableNodes)
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Warning,
                        $"Node '{node.DisplayTitle}' is unreachable from the configured {NormalizeGraphLabel(profile.RootNodeDisplayName)} nodes.",
                        node.Id));
                }
            }
        }

        private static void AppendFamilyMismatchIssues(
            IReadOnlyList<GraphNodeBase> nodes,
            ICollection<GraphValidationIssue> issues,
            GraphTopologyValidationProfile profile)
        {
            if (!profile.DetectFamilyMismatch
                || profile.ResolveNodeFamilyId == null
                || string.IsNullOrWhiteSpace(profile.ExpectedFamilyId))
            {
                return;
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                GraphNodeBase node = nodes[index];
                string nodeFamilyId = profile.ResolveNodeFamilyId(node);

                if (string.IsNullOrWhiteSpace(nodeFamilyId)
                    || string.Equals(
                        nodeFamilyId,
                        profile.ExpectedFamilyId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                issues.Add(new GraphValidationIssue(
                    GraphValidationSeverity.Error,
                    $"Node '{node.DisplayTitle}' belongs to family '{nodeFamilyId}', but the graph expects family '{profile.ExpectedFamilyId}'.",
                    node.Id));
            }
        }

        private static Dictionary<SerializableGuid, GraphNodeBase> BuildNodeMap(
            IReadOnlyList<GraphNodeBase> nodes)
        {
            Dictionary<SerializableGuid, GraphNodeBase> nodesById = new();

            for (int index = 0; index < nodes.Count; index++)
            {
                GraphNodeBase node = nodes[index];

                if (node.Id == SerializableGuid.Empty || nodesById.ContainsKey(node.Id))
                {
                    continue;
                }

                nodesById.Add(node.Id, node);
            }

            return nodesById;
        }

        private static List<GraphNodeBase> GetRootNodes(
            IReadOnlyList<GraphNodeBase> nodes,
            GraphTopologyValidationProfile profile)
        {
            Func<GraphNodeBase, bool> isRootNode = profile.IsRootNode
                ?? (node => node != null && !node.HasInputPort);

            return nodes.Where(isRootNode).ToList();
        }

        private static HashSet<SerializableGuid> GetReachableNodeIds(
            GraphDefinition graph,
            IEnumerable<SerializableGuid> rootNodeIds)
        {
            HashSet<SerializableGuid> reachableNodeIds = new();
            Queue<SerializableGuid> queue = new();

            foreach (SerializableGuid rootNodeId in rootNodeIds)
            {
                if (rootNodeId != SerializableGuid.Empty)
                {
                    queue.Enqueue(rootNodeId);
                }
            }

            while (queue.Count > 0)
            {
                SerializableGuid currentNodeId = queue.Dequeue();

                if (!reachableNodeIds.Add(currentNodeId))
                {
                    continue;
                }

                foreach (GraphConnection connection in graph.GetOutgoingConnections(currentNodeId))
                {
                    if (connection != null)
                    {
                        queue.Enqueue(connection.ToNodeId);
                    }
                }
            }

            return reachableNodeIds;
        }

        private static bool ShouldValidateNode(
            GraphNodeBase node,
            GraphTopologyValidationProfile profile)
        {
            if (node == null)
            {
                return false;
            }

            return profile.ShouldValidateNode?.Invoke(node)
                ?? node.ParticipatesInTopologyValidation;
        }

        private static string NormalizeGraphLabel(string label)
        {
            return string.IsNullOrWhiteSpace(label) ? "graph" : label.Trim();
        }

        private static string NormalizeOutputKey(string outputKey)
        {
            return string.IsNullOrWhiteSpace(outputKey) ? "Unknown" : outputKey;
        }

        private static string CreateConnectionKey(
            SerializableGuid fromNodeId,
            string outputKey)
        {
            return $"{fromNodeId}|{outputKey ?? string.Empty}";
        }
    }
}