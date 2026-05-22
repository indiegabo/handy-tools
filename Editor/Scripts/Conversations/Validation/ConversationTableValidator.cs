using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Nodes;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.GraphCore.Validation;
using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.Editor.ConversationsModule.Validation
{
    /// <summary>
    /// Validates one authored conversation table and emits editor-facing issues.
    /// </summary>
    public static class ConversationTableValidator
    {
        /// <summary>
        /// Validates the provided authored conversation table.
        /// </summary>
        /// <param name="table">Table that should be validated.</param>
        /// <returns>The current validation issues.</returns>
        public static IReadOnlyList<ConversationValidationIssue> Validate(ConversationTable table)
        {
            if (table == null)
            {
                return Array.Empty<ConversationValidationIssue>();
            }

            List<ConversationValidationIssue> issues = new();
            AppendConversationIssues(table, issues);
            return issues;
        }

        /// <summary>
        /// Appends conversation-level issues such as duplicate ids, actor problems,
        /// and graph topology issues.
        /// </summary>
        /// <param name="table">Table that owns the conversations.</param>
        /// <param name="issues">Issue sink.</param>
        private static void AppendConversationIssues(
            ConversationTable table,
            ICollection<ConversationValidationIssue> issues)
        {
            if (table?.Conversations == null)
            {
                return;
            }

            HashSet<SerializableGuid> seenConversationIds = new();
            AppendActorIssues(table, issues);

            for (int index = 0; index < table.Conversations.Count; index++)
            {
                ConversationDefinition conversation = table.Conversations[index];

                if (conversation == null)
                {
                    continue;
                }

                string conversationLabel = BuildConversationLabel(conversation);

                if (conversation.ConversationId == SerializableGuid.Empty)
                {
                    issues.Add(new ConversationValidationIssue(
                        ConversationValidationSeverity.Error,
                        "conversation.id.empty",
                        $"Conversation '{conversationLabel}' has an empty stable id.",
                        ConversationValidationTargetKind.Conversation,
                        conversation.ConversationId));
                }
                else if (!seenConversationIds.Add(conversation.ConversationId))
                {
                    issues.Add(new ConversationValidationIssue(
                        ConversationValidationSeverity.Error,
                        "conversation.id.duplicate",
                        $"Conversation '{conversationLabel}' duplicates another conversation id.",
                        ConversationValidationTargetKind.Conversation,
                        conversation.ConversationId));
                }

                AppendConversationGraphIssues(table, conversation, issues);
            }
        }

        /// <summary>
        /// Appends shared-actor issues for one authored table.
        /// </summary>
        /// <param name="table">Table that owns the shared conversants.</param>
        /// <param name="issues">Issue sink.</param>
        private static void AppendActorIssues(
            ConversationTable table,
            ICollection<ConversationValidationIssue> issues)
        {
            if (table?.Actors == null)
            {
                return;
            }

            HashSet<SerializableGuid> seenActorIds = new();
            Dictionary<string, SerializableGuid> seenActorKeys =
                new(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < table.Actors.Count; index++)
            {
                ConversationActorDefinition actor = table.Actors[index];

                if (actor == null)
                {
                    issues.Add(new ConversationValidationIssue(
                        ConversationValidationSeverity.Error,
                        "actor.null",
                        "The table contains one empty conversant entry.",
                        ConversationValidationTargetKind.Table));
                    continue;
                }

                string actorLabel = BuildActorLabel(actor);

                if (actor.ActorId == SerializableGuid.Empty)
                {
                    issues.Add(new ConversationValidationIssue(
                        ConversationValidationSeverity.Error,
                        "actor.id.empty",
                        $"Conversant '{actorLabel}' has an empty stable id.",
                        ConversationValidationTargetKind.Actor,
                        actorId: actor.ActorId));
                }
                else if (!seenActorIds.Add(actor.ActorId))
                {
                    issues.Add(new ConversationValidationIssue(
                        ConversationValidationSeverity.Error,
                        "actor.id.duplicate",
                        $"Conversant '{actorLabel}' duplicates another conversant id.",
                        ConversationValidationTargetKind.Actor,
                        actorId: actor.ActorId));
                }

                string actorKey = actor.Key;

                if (seenActorKeys.TryGetValue(
                        actorKey,
                        out SerializableGuid existingActorId)
                    && existingActorId != actor.ActorId)
                {
                    issues.Add(new ConversationValidationIssue(
                        ConversationValidationSeverity.Error,
                        "actor.key.duplicate",
                        $"The table uses conversant key '{actorKey}' more than once.",
                        ConversationValidationTargetKind.Actor,
                        actorId: actor.ActorId));
                }
                else if (!seenActorKeys.ContainsKey(actorKey))
                {
                    seenActorKeys.Add(actorKey, actor.ActorId);
                }
            }
        }

        /// <summary>
        /// Appends graph-topology and node-semantic issues for one conversation graph.
        /// </summary>
        /// <param name="conversation">Conversation that owns the graph.</param>
        /// <param name="issues">Issue sink.</param>
        private static void AppendConversationGraphIssues(
            ConversationTable table,
            ConversationDefinition conversation,
            ICollection<ConversationValidationIssue> issues)
        {
            ConversationGraph graph = conversation?.Graph;
            string conversationLabel = BuildConversationLabel(conversation);

            if (graph == null)
            {
                issues.Add(new ConversationValidationIssue(
                    ConversationValidationSeverity.Error,
                    "conversation.graph.missing",
                    $"Conversation '{conversationLabel}' is missing its graph.",
                    ConversationValidationTargetKind.Conversation,
                    conversation?.ConversationId ?? SerializableGuid.Empty));
                return;
            }

            GraphTopologyValidationProfile profile = new()
            {
                GraphDisplayName = "conversation graph",
                RootNodeDisplayName = "Entry",
                RequireRootNode = true,
                AllowMultipleRootNodes = false,
                DetectUnreachableNodes = true,
                DetectOrphanNodes = false,
                DetectFamilyMismatch = false,
                AppendSemanticIssues = (validationGraph, nodes, validationIssues) =>
                    AppendDuplicateNodeIdIssues(nodes, validationIssues),
            };

            IReadOnlyList<GraphValidationIssue> graphIssues =
                GraphTopologyValidator.Validate(graph, profile);

            for (int index = 0; index < graphIssues.Count; index++)
            {
                GraphValidationIssue graphIssue = graphIssues[index];
                issues.Add(new ConversationValidationIssue(
                    ConvertSeverity(graphIssue.Severity),
                    "conversation.graph.topology",
                    $"Conversation '{conversationLabel}': {graphIssue.Message}",
                    graphIssue.NodeId == SerializableGuid.Empty
                        ? ConversationValidationTargetKind.Conversation
                        : ConversationValidationTargetKind.Node,
                    conversation.ConversationId,
                    nodeId: graphIssue.NodeId));
            }

            AppendTextNodeIssues(table, conversation, issues);
        }

        /// <summary>
        /// Appends semantic issues for authored text-presenting nodes.
        /// </summary>
        /// <param name="conversation">Conversation that owns the nodes.</param>
        /// <param name="issues">Issue sink.</param>
        private static void AppendTextNodeIssues(
            ConversationTable table,
            ConversationDefinition conversation,
            ICollection<ConversationValidationIssue> issues)
        {
            if (conversation?.Graph?.Nodes == null)
            {
                return;
            }

            string conversationLabel = BuildConversationLabel(conversation);
            for (int index = 0; index < conversation.Graph.Nodes.Count; index++)
            {
                if (conversation.Graph.Nodes[index] is ConversationLineNode spokenLineNode)
                {
                    AppendSpokenLineIssues(table, conversation, conversationLabel, spokenLineNode, issues);
                    continue;
                }

                if (conversation.Graph.Nodes[index] is ConversationNarrationLineNode narrationLineNode)
                {
                    AppendNarrationLineIssues(
                        conversation,
                        conversationLabel,
                        narrationLineNode,
                        issues);
                }
            }
        }

        /// <summary>
        /// Appends semantic issues for one authored spoken line node.
        /// </summary>
        /// <param name="table">Table that owns the shared conversants.</param>
        /// <param name="conversation">Conversation that owns the node.</param>
        /// <param name="conversationLabel">Display label used by diagnostics.</param>
        /// <param name="lineNode">Spoken line node being validated.</param>
        /// <param name="issues">Issue sink.</param>
        private static void AppendSpokenLineIssues(
            ConversationTable table,
            ConversationDefinition conversation,
            string conversationLabel,
            ConversationLineNode lineNode,
            ICollection<ConversationValidationIssue> issues)
        {
            if (lineNode.SpeakerActorId == SerializableGuid.Empty)
            {
                issues.Add(new ConversationValidationIssue(
                    ConversationValidationSeverity.Error,
                    "spoken_line.speaker.empty",
                    $"Conversation '{conversationLabel}' has a spoken line node without a speaker conversant.",
                    ConversationValidationTargetKind.Node,
                    conversation.ConversationId,
                    nodeId: lineNode.Id));
            }
            else if (table == null || !table.TryGetActor(lineNode.SpeakerActorId, out _))
            {
                issues.Add(new ConversationValidationIssue(
                    ConversationValidationSeverity.Error,
                    "spoken_line.speaker.missing",
                    $"Conversation '{conversationLabel}' has a spoken line node pointing to a missing speaker conversant.",
                    ConversationValidationTargetKind.Node,
                    conversation.ConversationId,
                    actorId: lineNode.SpeakerActorId,
                    nodeId: lineNode.Id));
            }

            if (lineNode.ListenerActorId != SerializableGuid.Empty
                && (table == null || !table.TryGetActor(lineNode.ListenerActorId, out _)))
            {
                issues.Add(new ConversationValidationIssue(
                    ConversationValidationSeverity.Error,
                    "spoken_line.listener.missing",
                    $"Conversation '{conversationLabel}' has a spoken line node pointing to a missing listener conversant.",
                    ConversationValidationTargetKind.Node,
                    conversation.ConversationId,
                    actorId: lineNode.ListenerActorId,
                    nodeId: lineNode.Id));
            }

            if (string.IsNullOrWhiteSpace(lineNode.LineText))
            {
                issues.Add(new ConversationValidationIssue(
                    ConversationValidationSeverity.Warning,
                    "spoken_line.text.empty",
                    $"Conversation '{conversationLabel}' has a spoken line node without literal text.",
                    ConversationValidationTargetKind.Node,
                    conversation.ConversationId,
                    nodeId: lineNode.Id));
            }
        }

        /// <summary>
        /// Appends semantic issues for one authored narration line node.
        /// </summary>
        /// <param name="conversation">Conversation that owns the node.</param>
        /// <param name="conversationLabel">Display label used by diagnostics.</param>
        /// <param name="lineNode">Narration line node being validated.</param>
        /// <param name="issues">Issue sink.</param>
        private static void AppendNarrationLineIssues(
            ConversationDefinition conversation,
            string conversationLabel,
            ConversationNarrationLineNode lineNode,
            ICollection<ConversationValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(lineNode.LineText))
            {
                issues.Add(new ConversationValidationIssue(
                    ConversationValidationSeverity.Warning,
                    "narration_line.text.empty",
                    $"Conversation '{conversationLabel}' has a narration line node without literal text.",
                    ConversationValidationTargetKind.Node,
                    conversation.ConversationId,
                    nodeId: lineNode.Id));
            }
        }

        /// <summary>
        /// Appends duplicate or empty node-id issues for one graph.
        /// </summary>
        /// <param name="nodes">Graph nodes participating in validation.</param>
        /// <param name="issues">Issue sink.</param>
        private static void AppendDuplicateNodeIdIssues(
            IReadOnlyList<GraphNodeBase> nodes,
            ICollection<GraphValidationIssue> issues)
        {
            HashSet<SerializableGuid> seenNodeIds = new();

            for (int index = 0; index < nodes.Count; index++)
            {
                if (nodes[index] is not ConversationNodeBase node)
                {
                    continue;
                }

                if (node.Id == SerializableGuid.Empty)
                {
                    issues.Add(new GraphValidationIssue(
                        GraphValidationSeverity.Error,
                        $"Node '{node.DisplayTitle}' has an empty id.",
                        node.Id));
                    continue;
                }

                if (seenNodeIds.Add(node.Id))
                {
                    continue;
                }

                issues.Add(new GraphValidationIssue(
                    GraphValidationSeverity.Error,
                    $"Node '{node.DisplayTitle}' duplicates another node id.",
                    node.Id));
            }
        }

        /// <summary>
        /// Converts one shared graph-validation severity into the Conversations severity.
        /// </summary>
        /// <param name="severity">Shared graph severity.</param>
        /// <returns>The mapped Conversations severity.</returns>
        private static ConversationValidationSeverity ConvertSeverity(GraphValidationSeverity severity)
        {
            return severity switch
            {
                GraphValidationSeverity.Info => ConversationValidationSeverity.Info,
                GraphValidationSeverity.Warning => ConversationValidationSeverity.Warning,
                _ => ConversationValidationSeverity.Error,
            };
        }

        /// <summary>
        /// Builds one short display label for the provided conversant.
        /// </summary>
        /// <param name="actor">Conversant that should be represented.</param>
        /// <returns>The display label used in validation messages.</returns>
        private static string BuildActorLabel(ConversationActorDefinition actor)
        {
            if (actor == null)
            {
                return "Conversant";
            }

            string displayName = actor.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                return actor.Key;
            }

            return string.Equals(displayName, actor.Key, StringComparison.OrdinalIgnoreCase)
                ? displayName
                : $"{displayName} ({actor.Key})";
        }

        /// <summary>
        /// Builds one short display label for the provided conversation.
        /// </summary>
        /// <param name="conversation">Conversation that should be represented.</param>
        /// <returns>The display label used in validation messages.</returns>
        private static string BuildConversationLabel(ConversationDefinition conversation)
        {
            if (conversation == null)
            {
                return "Conversation";
            }

            return string.IsNullOrWhiteSpace(conversation.Title)
                ? "Conversation"
                : conversation.Title;
        }
    }
}
