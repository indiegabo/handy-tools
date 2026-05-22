using System;
using System.Collections.Generic;
using System.Text;
using IndieGabo.HandyTools.ConversationsModule;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using IndieGabo.HandyTools.ConversationsModule.Nodes.Actions;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Export
{
    /// <summary>
    /// Builds lightweight runtime DTOs from authored conversation tables without carrying editor-only layout state.
    /// </summary>
    public static class ConversationRuntimeCatalogBuilder
    {
        private const int CurrentExportVersion = 1;
        private const string DefaultPayloadDirectory = "Conversations";

        /// <summary>
        /// Builds one runtime catalog and one matching payload set from the provided authored table.
        /// </summary>
        /// <param name="table">Authored table that should be exported.</param>
        /// <param name="catalog">Built lightweight runtime catalog.</param>
        /// <param name="conversationDataSet">Built per-conversation runtime payloads.</param>
        /// <param name="payloadDirectory">Relative payload directory used for generated path metadata.</param>
        public static void Build(
            ConversationTable table,
            out ConversationRuntimeCatalog catalog,
            out List<ConversationData> conversationDataSet,
            string payloadDirectory = DefaultPayloadDirectory)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            table.EnsureAuthoringIds();
            conversationDataSet = new List<ConversationData>(table.Conversations.Count);
            List<ConversationRuntimeCatalog.Entry> catalogEntries =
                new(table.Conversations.Count);
            string normalizedPayloadDirectory = NormalizePayloadDirectory(payloadDirectory);

            for (int index = 0; index < table.Conversations.Count; index++)
            {
                ConversationDefinition conversation = table.Conversations[index];

                if (conversation == null)
                {
                    continue;
                }

                ConversationData conversationData = BuildConversationData(table, conversation);
                conversationDataSet.Add(conversationData);

                string serializedPayload = JsonUtility.ToJson(conversationData);
                string conversationHexId = conversation.ConversationId
                    .ToHexString()
                    .ToLowerInvariant();

                catalogEntries.Add(
                    new ConversationRuntimeCatalog.Entry(
                        conversation.ConversationId,
                        conversation.Title,
                        CurrentExportVersion,
                        $"{normalizedPayloadDirectory}/{conversationHexId}.json",
                        $"conversations/{conversationHexId}",
                        Hash128.Compute(serializedPayload).ToString(),
                        Encoding.UTF8.GetByteCount(serializedPayload)));
            }

            catalog = new ConversationRuntimeCatalog(CurrentExportVersion, catalogEntries);
        }

        /// <summary>
        /// Builds only the runtime catalog metadata for the provided authored table.
        /// </summary>
        /// <param name="table">Authored table that should be exported.</param>
        /// <param name="payloadDirectory">Relative payload directory used for generated path metadata.</param>
        /// <returns>The built runtime catalog.</returns>
        public static ConversationRuntimeCatalog BuildCatalog(
            ConversationTable table,
            string payloadDirectory = DefaultPayloadDirectory)
        {
            Build(table, out ConversationRuntimeCatalog catalog, out _, payloadDirectory);
            return catalog;
        }

        /// <summary>
        /// Builds only the per-conversation runtime payload set for the provided authored table.
        /// </summary>
        /// <param name="table">Authored table that should be exported.</param>
        /// <returns>The built per-conversation runtime payload set.</returns>
        public static List<ConversationData> BuildConversationDataSet(ConversationTable table)
        {
            Build(table, out _, out List<ConversationData> conversationDataSet);
            return conversationDataSet;
        }

        /// <summary>
        /// Builds one per-conversation runtime payload from the authored conversation definition.
        /// </summary>
        /// <param name="table">Authored table that owns the shared conversants.</param>
        /// <param name="conversation">Authored conversation that should be exported.</param>
        /// <returns>The built runtime payload.</returns>
        public static ConversationData BuildConversationData(
            ConversationTable table,
            ConversationDefinition conversation)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (conversation == null)
            {
                throw new ArgumentNullException(nameof(conversation));
            }

            table.EnsureAuthoringIds();
            conversation.EnsureAuthoringIds();
            List<ConversationChoiceData> choices = new();
            List<ConversationNodeData> nodes = BuildNodes(conversation, choices);
            SerializableGuid entryNodeId = ResolveEntryNodeId(nodes, conversation.Title);

            return new ConversationData(
                conversation.ConversationId,
                conversation.Title,
                entryNodeId,
                nodes,
                choices,
                BuildActors(table, conversation));
        }

        /// <summary>
        /// Builds the exported conversant records referenced by the authored conversation.
        /// </summary>
        /// <param name="table">Authored table that owns the shared conversants.</param>
        /// <param name="conversation">Authored conversation that references the shared conversants.</param>
        /// <returns>The exported conversant DTOs.</returns>
        private static List<ConversationActorData> BuildActors(
            ConversationTable table,
            ConversationDefinition conversation)
        {
            HashSet<SerializableGuid> referencedActorIds = CollectReferencedActorIds(conversation);
            List<ConversationActorData> actors =
                new(referencedActorIds.Count);

            if (table?.Actors == null || referencedActorIds.Count == 0)
            {
                return actors;
            }

            for (int index = 0; index < table.Actors.Count; index++)
            {
                ConversationActorDefinition actor = table.Actors[index];

                if (actor == null || !referencedActorIds.Contains(actor.ActorId))
                {
                    continue;
                }

                actor.EnsureId();
                actors.Add(
                    new ConversationActorData(
                        actor.ActorId,
                        actor.Key,
                        actor.DisplayName,
                        actor.ThemeColor));
            }

            return actors;
        }

        /// <summary>
        /// Collects the shared conversant identifiers referenced by one authored conversation.
        /// </summary>
        /// <param name="conversation">Authored conversation that should be inspected.</param>
        /// <returns>The referenced conversant ids.</returns>
        private static HashSet<SerializableGuid> CollectReferencedActorIds(
            ConversationDefinition conversation)
        {
            HashSet<SerializableGuid> actorIds = new();

            if (conversation?.Graph?.Nodes == null)
            {
                return actorIds;
            }

            for (int index = 0; index < conversation.Graph.Nodes.Count; index++)
            {
                if (conversation.Graph.Nodes[index] is not ConversationLineNode lineNode)
                {
                    continue;
                }

                if (lineNode.SpeakerActorId != SerializableGuid.Empty)
                {
                    actorIds.Add(lineNode.SpeakerActorId);
                }

                if (lineNode.ListenerActorId != SerializableGuid.Empty)
                {
                    actorIds.Add(lineNode.ListenerActorId);
                }
            }

            return actorIds;
        }

        /// <summary>
        /// Builds the exported node records for the authored conversation graph.
        /// </summary>
        /// <param name="conversation">Authored conversation that owns the graph.</param>
        /// <param name="choices">Mutable target list for exported choices.</param>
        /// <returns>The exported node DTOs.</returns>
        private static List<ConversationNodeData> BuildNodes(
            ConversationDefinition conversation,
            List<ConversationChoiceData> choices)
        {
            ConversationGraph graph = conversation.Graph;
            List<ConversationNodeData> nodes = new(graph.Nodes.Count);

            for (int index = 0; index < graph.Nodes.Count; index++)
            {
                ConversationNodeBase node = graph.Nodes[index] as ConversationNodeBase;

                if (node == null)
                {
                    continue;
                }

                ConversationNodeKind nodeKind = ResolveNodeKind(node);

                switch (nodeKind)
                {
                    case ConversationNodeKind.Entry:
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                nodeKind,
                                ResolveNextNodeId(graph, node.Id, GraphPortKeys.Next)));
                        break;

                    case ConversationNodeKind.SpokenLine:
                        ConversationLineNode lineNode = node as ConversationLineNode;
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                nodeKind,
                                ResolveNextNodeId(graph, node.Id, GraphPortKeys.Next),
                                lineNode?.SpeakerActorId ?? SerializableGuid.Empty,
                                lineNode?.ListenerActorId ?? SerializableGuid.Empty,
                                lineNode?.SpeakerSlot ?? ConversationParticipantSlot.Auto,
                                lineNode?.ListenerSlot ?? ConversationParticipantSlot.Auto,
                                ConversationStringValueData.CreateDirect(
                                    lineNode?.LineText ?? string.Empty),
                                ConversationTextIdUtility.Build(
                                    conversation.ConversationId,
                                    node.Id)));
                        break;

                    case ConversationNodeKind.NarrationLine:
                        ConversationNarrationLineNode narrationLineNode =
                            node as ConversationNarrationLineNode;
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                nodeKind,
                                ResolveNextNodeId(graph, node.Id, GraphPortKeys.Next),
                                SerializableGuid.Empty,
                                SerializableGuid.Empty,
                                ConversationParticipantSlot.Auto,
                                ConversationParticipantSlot.Auto,
                                ConversationStringValueData.CreateDirect(
                                    narrationLineNode?.LineText ?? string.Empty),
                                ConversationTextIdUtility.Build(
                                    conversation.ConversationId,
                                    node.Id)));
                        break;

                    case ConversationNodeKind.Wait:
                        ConversationWaitNode waitNode = node as ConversationWaitNode;
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                nodeKind,
                                ResolveNextNodeId(graph, node.Id, GraphPortKeys.Next),
                                waitDurationSeconds: waitNode?.DurationSeconds ?? 0f,
                                timeMode: waitNode?.TimeMode ?? ConversationTimeMode.Scaled));
                        break;

                    case ConversationNodeKind.EmitHandyBusEvent:
                        ConversationEmitHandyBusEventNode emitEventNode =
                            node as ConversationEmitHandyBusEventNode;
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                nodeKind,
                                ResolveNextNodeId(graph, node.Id, GraphPortKeys.Next),
                                eventName: emitEventNode?.EventName ?? string.Empty));
                        break;

                    case ConversationNodeKind.WaitForEvent:
                        ConversationWaitForEventNode waitForEventNode =
                            node as ConversationWaitForEventNode;
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                nodeKind,
                                ResolveNextNodeId(graph, node.Id, GraphPortKeys.Next),
                                eventName: waitForEventNode?.EventName ?? string.Empty));
                        break;

                    case ConversationNodeKind.PlayTimeline:
                        throw new NotSupportedException(
                            $"Conversation '{conversation.Title}' cannot be exported when it uses Play Timeline nodes because runtime payload exports cannot serialize scene PlayableDirector references.");

                    default:
                        throw new NotSupportedException(
                            $"Conversation '{conversation.Title}' contains one authored node "
                            + $"type that the runtime export does not support yet: "
                            + $"{node.GetType().FullName}.");
                }
            }

            choices ??= new List<ConversationChoiceData>();
            return nodes;
        }

        /// <summary>
        /// Resolves the exported node kind for the authored node.
        /// </summary>
        /// <param name="node">Authored node to classify.</param>
        /// <returns>The exported runtime node kind.</returns>
        private static ConversationNodeKind ResolveNodeKind(ConversationNodeBase node)
        {
            if (node is ConversationEntryNode)
            {
                return ConversationNodeKind.Entry;
            }

            if (node is ConversationLineNode)
            {
                return ConversationNodeKind.SpokenLine;
            }

            if (node is ConversationNarrationLineNode)
            {
                return ConversationNodeKind.NarrationLine;
            }

            if (node is ConversationWaitNode)
            {
                return ConversationNodeKind.Wait;
            }

            if (node is ConversationEmitHandyBusEventNode)
            {
                return ConversationNodeKind.EmitHandyBusEvent;
            }

            if (node is ConversationWaitForEventNode)
            {
                return ConversationNodeKind.WaitForEvent;
            }

            if (node is ConversationPlayTimelineNode)
            {
                return ConversationNodeKind.PlayTimeline;
            }

            throw new NotSupportedException(
                $"Conversation export cannot classify authored node type "
                + $"{node.GetType().FullName}.");
        }

        /// <summary>
        /// Resolves the single downstream node connected from one authored output.
        /// </summary>
        /// <param name="graph">Authored graph that owns the connection data.</param>
        /// <param name="nodeId">Origin node identifier.</param>
        /// <param name="outputKey">Origin output key.</param>
        /// <returns>The resolved downstream node identifier when connected.</returns>
        private static SerializableGuid ResolveNextNodeId(
            ConversationGraph graph,
            SerializableGuid nodeId,
            string outputKey)
        {
            return graph.TryGetOutgoingConnection(nodeId, outputKey, out GraphConnection connection)
                ? connection.ToNodeId
                : SerializableGuid.Empty;
        }

        /// <summary>
        /// Resolves the unique entry node id from the exported node set.
        /// </summary>
        /// <param name="nodes">Exported node DTOs.</param>
        /// <param name="conversationTitle">Conversation title used for diagnostics.</param>
        /// <returns>The resolved entry node identifier.</returns>
        private static SerializableGuid ResolveEntryNodeId(
            List<ConversationNodeData> nodes,
            string conversationTitle)
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                if (nodes[index].Kind == ConversationNodeKind.Entry)
                {
                    return nodes[index].NodeId;
                }
            }

            throw new InvalidOperationException(
                $"Conversation '{conversationTitle}' cannot be exported without one entry node.");
        }
        /// <summary>
        /// Normalizes one authored payload directory to one forward-slash relative path.
        /// </summary>
        /// <param name="payloadDirectory">Requested payload directory.</param>
        /// <returns>The normalized payload directory.</returns>
        private static string NormalizePayloadDirectory(string payloadDirectory)
        {
            if (string.IsNullOrWhiteSpace(payloadDirectory))
            {
                return DefaultPayloadDirectory;
            }

            return payloadDirectory
                .Trim()
                .Replace('\\', '/')
                .Trim('/');
        }
    }
}