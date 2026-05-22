using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using IndieGabo.HandyTools.ConversationsModule.Nodes.Actions;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Builds runtime conversation payloads directly from authored ConversationTable data.
    /// </summary>
    public static class ConversationAuthoredRuntimeBuilder
    {
        #region Public API

        /// <summary>
        /// Resolves one authored conversation from the provided serialized conversation
        /// reference.
        /// </summary>
        /// <param name="reference">Serialized conversation reference.</param>
        /// <param name="conversation">Resolved authored conversation when found.</param>
        /// <param name="failureReason">Failure reason when the selection cannot be resolved.</param>
        /// <returns>True when one authored conversation was resolved.</returns>
        public static bool TryResolveConversation(
            ConversationReference reference,
            out ConversationDefinition conversation,
            out string failureReason)
        {
            if (reference == null)
            {
                conversation = null;
                failureReason = "Conversation trigger requires one ConversationTable asset.";
                return false;
            }

            return TryResolveConversation(
                reference.Table,
                reference.ConversationId,
                reference.ConversationTitle,
                out conversation,
                out failureReason);
        }

        /// <summary>
        /// Resolves one authored conversation from the provided table selection.
        /// </summary>
        /// <param name="table">Authored table that owns the conversation.</param>
        /// <param name="conversationTitle">Optional authored title used for selection.</param>
        /// <param name="conversation">Resolved authored conversation when found.</param>
        /// <param name="failureReason">Failure reason when the selection cannot be resolved.</param>
        /// <returns>True when one authored conversation was resolved.</returns>
        public static bool TryResolveConversation(
            ConversationTable table,
            string conversationTitle,
            out ConversationDefinition conversation,
            out string failureReason)
        {
            return TryResolveConversation(
                table,
                SerializableGuid.Empty,
                conversationTitle,
                out conversation,
                out failureReason);
        }

        /// <summary>
        /// Resolves one authored conversation from the provided table selection and stable
        /// conversation identifier.
        /// </summary>
        /// <param name="table">Authored table that owns the conversation.</param>
        /// <param name="conversationId">Stable authored conversation identifier.</param>
        /// <param name="conversationTitle">Optional authored title used as fallback selection.</param>
        /// <param name="conversation">Resolved authored conversation when found.</param>
        /// <param name="failureReason">Failure reason when the selection cannot be resolved.</param>
        /// <returns>True when one authored conversation was resolved.</returns>
        private static bool TryResolveConversation(
            ConversationTable table,
            SerializableGuid conversationId,
            string conversationTitle,
            out ConversationDefinition conversation,
            out string failureReason)
        {
            conversation = null;
            failureReason = string.Empty;

            if (table == null)
            {
                failureReason = "Conversation trigger requires one ConversationTable asset.";
                return false;
            }

            table.EnsureAuthoringIds();

            if (table.Conversations == null || table.Conversations.Count == 0)
            {
                failureReason =
                    "The configured ConversationTable does not contain authored conversations.";
                return false;
            }

            if (conversationId != SerializableGuid.Empty
                && table.TryGetConversation(conversationId, out conversation)
                && conversation != null)
            {
                return true;
            }

            string requestedTitle = conversationTitle?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(requestedTitle))
            {
                for (int index = 0; index < table.Conversations.Count; index++)
                {
                    if (table.Conversations[index] == null)
                    {
                        continue;
                    }

                    conversation = table.Conversations[index];
                    return true;
                }

                failureReason =
                    "The configured ConversationTable only contains missing conversation entries.";
                return false;
            }

            for (int index = 0; index < table.Conversations.Count; index++)
            {
                ConversationDefinition candidate = table.Conversations[index];

                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.Title, requestedTitle, StringComparison.OrdinalIgnoreCase))
                {
                    conversation = candidate;
                    return true;
                }
            }

            failureReason =
                conversationId != SerializableGuid.Empty
                    ? $"The configured ConversationTable does not contain the referenced conversation '{requestedTitle}'."
                    : $"The configured ConversationTable does not contain a conversation named '{requestedTitle}'.";
            return false;
        }

        /// <summary>
        /// Builds one runtime conversation payload directly from authored data.
        /// </summary>
        /// <param name="table">Authored table that owns the selected conversation.</param>
        /// <param name="conversation">Authored conversation that should be presented.</param>
        /// <returns>The built runtime conversation payload.</returns>
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

            return new ConversationData(
                conversation.ConversationId,
                conversation.Title,
                ResolveEntryNodeId(conversation),
                BuildNodes(conversation),
                Array.Empty<ConversationChoiceData>(),
                BuildActors(table));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Builds the runtime actor payloads exposed to playback presenters.
        /// </summary>
        /// <param name="table">Authored table that owns the shared actors.</param>
        /// <returns>The built actor payload set.</returns>
        private static List<ConversationActorData> BuildActors(ConversationTable table)
        {
            List<ConversationActorData> actors = new();

            if (table?.Actors == null)
            {
                return actors;
            }

            for (int index = 0; index < table.Actors.Count; index++)
            {
                ConversationActorDefinition actor = table.Actors[index];

                if (actor == null)
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
        /// Builds the runtime node payloads from the authored conversation graph.
        /// </summary>
        /// <param name="conversation">Authored conversation that owns the graph.</param>
        /// <returns>The built node payload set.</returns>
        private static List<ConversationNodeData> BuildNodes(ConversationDefinition conversation)
        {
            ConversationGraph graph = conversation.Graph;
            List<ConversationNodeData> nodes = new(graph.Nodes.Count);

            for (int index = 0; index < graph.Nodes.Count; index++)
            {
                if (graph.Nodes[index] is not ConversationNodeBase node)
                {
                    continue;
                }

                SerializableGuid nextNodeId = ResolveNextNodeId(graph, node.Id);

                switch (node)
                {
                    case ConversationEntryNode:
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                ConversationNodeKind.Entry,
                                nextNodeId));
                        break;

                    case ConversationLineNode lineNode:
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                ConversationNodeKind.SpokenLine,
                                nextNodeId,
                                lineNode.SpeakerActorId,
                                lineNode.ListenerActorId,
                                lineNode.SpeakerSlot,
                                lineNode.ListenerSlot,
                                ConversationStringValueData.CreateDirect(lineNode.LineText),
                                ConversationTextIdUtility.Build(
                                    conversation.ConversationId,
                                    node.Id)));
                        break;

                    case ConversationNarrationLineNode narrationNode:
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                ConversationNodeKind.NarrationLine,
                                nextNodeId,
                                lineText: ConversationStringValueData.CreateDirect(
                                    narrationNode.LineText),
                                textId: ConversationTextIdUtility.Build(
                                    conversation.ConversationId,
                                    node.Id)));
                        break;

                    case ConversationWaitNode waitNode:
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                ConversationNodeKind.Wait,
                                nextNodeId,
                                waitDurationSeconds: waitNode.DurationSeconds,
                                timeMode: waitNode.TimeMode));
                        break;

                    case ConversationEmitHandyBusEventNode emitEventNode:
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                ConversationNodeKind.EmitHandyBusEvent,
                                nextNodeId,
                                eventName: emitEventNode.EventName));
                        break;

                    case ConversationWaitForEventNode waitForEventNode:
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                ConversationNodeKind.WaitForEvent,
                                nextNodeId,
                                eventName: waitForEventNode.EventName));
                        break;

                    case ConversationPlayTimelineNode playTimelineNode:
                        nodes.Add(
                            new ConversationNodeData(
                                node.Id,
                                ConversationNodeKind.PlayTimeline,
                                nextNodeId,
                                playableDirector: playTimelineNode.PlayableDirector,
                                restartOnEnter: playTimelineNode.RestartOnEnter,
                                stopOnExit: playTimelineNode.StopOnExit));
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Conversation authored playback does not support node type '{node.GetType().Name}'.");
                }
            }

            return nodes;
        }

        /// <summary>
        /// Resolves the stable authored entry node id used to start playback.
        /// </summary>
        /// <param name="conversation">Authored conversation that should be inspected.</param>
        /// <returns>The resolved entry node id.</returns>
        private static SerializableGuid ResolveEntryNodeId(ConversationDefinition conversation)
        {
            if (conversation?.Graph?.Nodes == null)
            {
                throw new InvalidOperationException(
                    "Conversation authored playback could not access the authored graph.");
            }

            for (int index = 0; index < conversation.Graph.Nodes.Count; index++)
            {
                if (conversation.Graph.Nodes[index] is ConversationEntryNode entryNode)
                {
                    return entryNode.Id;
                }
            }

            throw new InvalidOperationException(
                $"Conversation '{conversation.Title}' does not contain an entry node.");
        }

        /// <summary>
        /// Resolves the linear next-node route for the provided authored node.
        /// </summary>
        /// <param name="graph">Authored graph that owns the node.</param>
        /// <param name="nodeId">Node whose next route should be resolved.</param>
        /// <returns>The downstream node id or <see cref="SerializableGuid.Empty"/> when none exists.</returns>
        private static SerializableGuid ResolveNextNodeId(
            ConversationGraph graph,
            SerializableGuid nodeId)
        {
            return graph != null
                && graph.TryGetOutgoingConnection(nodeId, GraphPortKeys.Next, out GraphConnection connection)
                ? connection.ToNodeId
                : SerializableGuid.Empty;
        }

        #endregion
    }
}