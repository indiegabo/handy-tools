using System;
using System.Collections.Generic;
using System.Reflection;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Actions;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Flow;
using IndieGabo.HandyTools.CutscenesModule.Events;
using IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem;
using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Playables;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    /// <summary>
    /// Converts serialized Cutscenes runtime blackboard data into GraphCore runtime shapes.
    /// </summary>
    public static class CutsceneGraphCoreRuntimeMigrationUtility
    {
        /// <summary>
        /// Attempts to resolve one migrated Cutscenes node payload from one GraphCore graph.
        /// </summary>
        /// <param name="graph">Migrated GraphCore graph definition.</param>
        /// <param name="nodeId">Stable node identifier.</param>
        /// <param name="node">Resolved Cutscenes node payload when available.</param>
        /// <returns>True when the migrated graph contains the node payload.</returns>
        public static bool TryGetCutsceneNode(
            GraphDefinition graph,
            SerializableGuid nodeId,
            out CutsceneNodeBase node)
        {
            node = null;

            return graph != null
                && graph.TryGetNode(nodeId, out GraphNodeBase graphNode)
                && TryGetCutsceneNode(graphNode, out node);
        }

        /// <summary>
        /// Attempts to resolve one migrated Cutscenes node payload from one GraphCore node.
        /// </summary>
        /// <param name="graphNode">Migrated GraphCore node snapshot.</param>
        /// <param name="node">Resolved Cutscenes node payload when available.</param>
        /// <returns>True when the migrated node wraps one Cutscenes payload.</returns>
        public static bool TryGetCutsceneNode(
            GraphNodeBase graphNode,
            out CutsceneNodeBase node)
        {
            node = (graphNode as CutsceneGraphNodeAdapter)?.SourceNode;
            return node != null;
        }

        /// <summary>
        /// Attempts to resolve one migrated entry node snapshot from one GraphCore graph.
        /// </summary>
        /// <param name="graph">Migrated GraphCore graph definition.</param>
        /// <param name="entryNode">Resolved GraphCore entry node snapshot when available.</param>
        /// <returns>True when the migrated graph contains one entry node snapshot.</returns>
        public static bool TryGetEntryNode(
            GraphDefinition graph,
            out GraphNodeBase entryNode)
        {
            entryNode = null;

            if (graph == null)
            {
                return false;
            }

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                GraphNodeBase graphNode = graph.Nodes[i];

                if (graphNode is CutsceneEntryGraphNode)
                {
                    entryNode = graphNode;
                    return true;
                }

                if (TryGetCutsceneNode(graphNode, out CutsceneNodeBase node)
                    && node is CutsceneEntryNode)
                {
                    entryNode = graphNode;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to execute one migrated GraphCore-native cutscene node that does not
        /// require the legacy payload adapter.
        /// </summary>
        /// <param name="graphNode">GraphCore node snapshot to execute.</param>
        /// <param name="context">Cutscene execution context bound to the node instance.</param>
        /// <returns>True when the node was recognized and completed.</returns>
        public static bool TryExecuteImmediateGraphNode(
            GraphNodeBase graphNode,
            CutsceneExecutionContext context)
        {
            if (graphNode is CutsceneEmitHandyBusEventGraphNode emitHandyBusEventNode)
            {
                return emitHandyBusEventNode.TryExecute(context);
            }

            return graphNode is IGraphExecuteNode executeNode
                && executeNode.TryExecute(context);
        }

        /// <summary>
        /// Attempts to enter one migrated graph node through the shared runtime path.
        /// </summary>
        /// <param name="graphNode">GraphCore node snapshot to execute.</param>
        /// <param name="context">Execution context bound to the current node instance.</param>
        /// <returns>True when the node was recognized and entered.</returns>
        public static bool TryEnterGraphNode(
            GraphNodeBase graphNode,
            CutsceneExecutionContext context)
        {
            if (graphNode == null || context == null)
            {
                return false;
            }

            if (graphNode is IGraphEnterNode enterNode)
            {
                return enterNode.TryEnter(context);
            }

            if (graphNode is CutsceneDialogueConversationGraphNode dialogueConversationNode)
            {
                return dialogueConversationNode.TryEnter(context);
            }

            if (TryExecuteImmediateGraphNode(graphNode, context))
            {
                return true;
            }

            if (!TryGetCutsceneNode(graphNode, out CutsceneNodeBase legacyNode))
            {
                return false;
            }

            legacyNode.OnEnter(context);
            return true;
        }

        /// <summary>
        /// Attempts to tick one migrated graph node through the shared runtime path.
        /// </summary>
        /// <param name="graphNode">GraphCore node snapshot to tick.</param>
        /// <param name="context">Execution context bound to the current node instance.</param>
        /// <returns>True when the node was recognized and ticked.</returns>
        public static bool TryTickGraphNode(
            GraphNodeBase graphNode,
            CutsceneExecutionContext context)
        {
            if (graphNode == null || context == null)
            {
                return false;
            }

            if (graphNode is IGraphTickNode tickNode)
            {
                return tickNode.TryTick(context);
            }

            if (graphNode is IGraphRuntimeNode)
            {
                return true;
            }

            if (graphNode is CutsceneDialogueConversationGraphNode dialogueConversationNode)
            {
                return dialogueConversationNode.TryTick(context);
            }

            if (!TryGetCutsceneNode(graphNode, out CutsceneNodeBase legacyNode))
            {
                return false;
            }

            if (legacyNode.RequiresTick)
            {
                legacyNode.Tick(context);
            }

            return true;
        }

        /// <summary>
        /// Attempts to exit one migrated graph node through the shared runtime path.
        /// </summary>
        /// <param name="graphNode">GraphCore node snapshot to exit.</param>
        /// <param name="context">Execution context bound to the current node instance.</param>
        /// <returns>True when the node was recognized and exited.</returns>
        public static bool TryExitGraphNode(
            GraphNodeBase graphNode,
            CutsceneExecutionContext context)
        {
            if (graphNode == null || context == null)
            {
                return false;
            }

            if (graphNode is IGraphExitNode exitNode)
            {
                return exitNode.TryExit(context);
            }

            if (graphNode is IGraphRuntimeNode)
            {
                return true;
            }

            if (graphNode is CutsceneDialogueConversationGraphNode dialogueConversationNode)
            {
                return dialogueConversationNode.TryExit(context);
            }

            if (!TryGetCutsceneNode(graphNode, out CutsceneNodeBase legacyNode))
            {
                return false;
            }

            legacyNode.OnExit(context);
            return true;
        }

        /// <summary>
        /// Returns whether the provided GraphCore node should be treated as one cutscene finish node.
        /// </summary>
        /// <param name="graphNode">GraphCore node snapshot to inspect.</param>
        /// <returns>True when the node represents one finish node.</returns>
        public static bool IsFinishNode(GraphNodeBase graphNode)
        {
            return graphNode is CutsceneFinishGraphNode
                || (TryGetCutsceneNode(graphNode, out CutsceneNodeBase node)
                    && node is CutsceneFinishNode);
        }

        /// <summary>
        /// Returns whether the provided GraphCore node should be treated as one cutscene parallel node.
        /// </summary>
        /// <param name="graphNode">GraphCore node snapshot to inspect.</param>
        /// <returns>True when the node represents one parallel node.</returns>
        public static bool IsParallelNode(GraphNodeBase graphNode)
        {
            return graphNode is CutsceneParallelGraphNode
                || (TryGetCutsceneNode(graphNode, out CutsceneNodeBase node)
                    && node is CutsceneParallelNode);
        }

        /// <summary>
        /// Attempts to resolve the original cutscene node runtime type represented by one migrated graph node.
        /// This preserves deterministic node identity even when later migration work no longer depends on the live payload adapter instance.
        /// </summary>
        /// <param name="graphNode">Migrated GraphCore node snapshot.</param>
        /// <param name="nodeType">Resolved legacy cutscene node runtime type.</param>
        /// <returns>True when one legacy cutscene node type could be resolved.</returns>
        public static bool TryGetLegacyCutsceneNodeType(
            GraphNodeBase graphNode,
            out Type nodeType)
        {
            nodeType = null;

            if (graphNode == null)
            {
                return false;
            }

            if (TryGetCutsceneNode(graphNode, out CutsceneNodeBase legacyNode))
            {
                nodeType = legacyNode.GetType();
                return true;
            }

            if (graphNode is CutsceneGraphNodeAdapter adapter
                && adapter.TryGetLegacyNodeType(out nodeType))
            {
                return true;
            }

            if (graphNode is CutsceneEntryGraphNode)
            {
                nodeType = typeof(CutsceneEntryNode);
                return true;
            }

            if (graphNode is CutsceneFinishGraphNode)
            {
                nodeType = typeof(CutsceneFinishNode);
                return true;
            }

            if (graphNode is CutsceneParallelGraphNode)
            {
                nodeType = typeof(CutsceneParallelNode);
                return true;
            }

            if (graphNode is CutsceneWaitGraphNode)
            {
                nodeType = typeof(CutsceneWaitNode);
                return true;
            }

            if (graphNode is CutsceneWaitForEventGraphNode)
            {
                nodeType = typeof(CutsceneWaitForEventNode);
                return true;
            }

            if (graphNode is CutscenePlayTimelineGraphNode)
            {
                nodeType = typeof(CutscenePlayTimelineNode);
                return true;
            }

            if (graphNode is CutsceneDialogueConversationGraphNode)
            {
                nodeType = typeof(CutsceneDialogueConversationNode);
                return true;
            }

            if (graphNode is CutsceneSetGameObjectActiveGraphNode)
            {
                nodeType = typeof(CutsceneSetGameObjectActiveNode);
                return true;
            }

            if (graphNode is CutsceneAnimatorTriggerGraphNode)
            {
                nodeType = typeof(CutsceneAnimatorTriggerNode);
                return true;
            }

            if (graphNode is CutsceneEmitHandyBusEventGraphNode)
            {
                nodeType = typeof(CutsceneEmitHandyBusEventNode);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates one legacy Cutscenes graph copy from one GraphCore definition.
        /// This first authored bridge reconstructs native migrated nodes directly and
        /// reuses still-attached legacy payloads when the adapter path remains active.
        /// </summary>
        /// <param name="source">Migrated GraphCore graph definition.</param>
        /// <param name="migratedBlackboard">
        /// Optional authored blackboard instance that should be attached to the reconstructed graph.
        /// </param>
        /// <returns>The reconstructed Cutscenes graph.</returns>
        public static CutsceneGraph CreateCutsceneGraph(
            GraphDefinition source,
            CutsceneGraphBlackboard migratedBlackboard = null)
        {
            CutsceneGraph target = new();
            target.RestoreBlackboard(
                migratedBlackboard ?? CreateCutsceneGraphBlackboard(source?.Blackboard));

            if (source == null)
            {
                return target;
            }

            for (int i = 0; i < source.Nodes.Count; i++)
            {
                CutsceneNodeBase targetNode = CreateCutsceneNode(source.Nodes[i]);

                if (targetNode == null)
                {
                    continue;
                }

                target.AddNode(targetNode);
            }

            for (int i = 0; i < source.Connections.Count; i++)
            {
                GraphConnection sourceConnection = source.Connections[i];

                if (sourceConnection == null)
                {
                    continue;
                }

                CutsceneConnection targetConnection = new(
                    sourceConnection.FromNodeId,
                    sourceConnection.OutputKey,
                    sourceConnection.ToNodeId);

                if (sourceConnection.HasCustomColor)
                {
                    targetConnection.SetCustomColor(sourceConnection.CustomColor);
                }

                target.AddConnection(targetConnection);
            }

            target.EnsureNodeIds();
            return target;
        }

        /// <summary>
        /// Creates one GraphCore graph-definition copy from one legacy Cutscenes graph.
        /// </summary>
        /// <param name="source">Legacy Cutscenes graph.</param>
        /// <param name="migratedBlackboard">
        /// Optional runtime blackboard instance that should be attached to the migrated graph.
        /// </param>
        /// <returns>The migrated GraphCore graph definition.</returns>
        public static GraphDefinition CreateGraphDefinition(
            CutsceneGraph source,
            GraphBlackboard migratedBlackboard = null)
        {
            GraphDefinition target = new();
            target.RestoreBlackboard(migratedBlackboard ?? CreateGraphBlackboard(source?.Blackboard));

            if (source == null)
            {
                return target;
            }

            for (int i = 0; i < source.Nodes.Count; i++)
            {
                CutsceneNodeBase sourceNode = source.Nodes[i];

                if (sourceNode == null)
                {
                    continue;
                }

                GraphNodeBase targetNode = sourceNode switch
                {
                    CutsceneEntryNode entryNode => new CutsceneEntryGraphNode(entryNode),
                    CutsceneFinishNode finishNode => new CutsceneFinishGraphNode(finishNode),
                    CutsceneParallelNode parallelNode => new CutsceneParallelGraphNode(parallelNode),
                    CutsceneWaitNode waitNode => new CutsceneWaitGraphNode(waitNode),
                    CutsceneWaitForEventNode waitForEventNode =>
                        new CutsceneWaitForEventGraphNode(waitForEventNode),
                    CutscenePlayTimelineNode playTimelineNode =>
                        new CutscenePlayTimelineGraphNode(playTimelineNode),
                    CutsceneDialogueConversationNode dialogueConversationNode =>
                        new CutsceneDialogueConversationGraphNode(dialogueConversationNode),
                    CutsceneSetGameObjectActiveNode setGameObjectActiveNode =>
                        new CutsceneSetGameObjectActiveGraphNode(setGameObjectActiveNode),
                    CutsceneAnimatorTriggerNode animatorTriggerNode =>
                        new CutsceneAnimatorTriggerGraphNode(animatorTriggerNode),
                    CutsceneEmitHandyBusEventNode emitHandyBusEventNode =>
                        new CutsceneEmitHandyBusEventGraphNode(emitHandyBusEventNode),
                    _ => new CutsceneGraphNodeAdapter(sourceNode),
                };

                target.AddNode(targetNode, preserveId: true);
            }

            for (int i = 0; i < source.Connections.Count; i++)
            {
                CutsceneConnection sourceConnection = source.Connections[i];

                if (sourceConnection == null)
                {
                    continue;
                }

                GraphConnection targetConnection = new(
                    sourceConnection.FromNodeId,
                    sourceConnection.OutputKey,
                    sourceConnection.ToNodeId);

                if (sourceConnection.HasCustomColor)
                {
                    targetConnection.SetCustomColor(sourceConnection.CustomColor);
                }

                target.AddConnection(targetConnection);
            }

            target.EnsureNodeIds();
            return target;
        }

        /// <summary>
        /// Creates one legacy Cutscenes blackboard copy from one GraphCore blackboard.
        /// </summary>
        /// <param name="source">GraphCore blackboard to reconstruct.</param>
        /// <returns>The reconstructed Cutscenes blackboard.</returns>
        public static CutsceneGraphBlackboard CreateCutsceneGraphBlackboard(GraphBlackboard source)
        {
            CutsceneGraphBlackboard target = new();

            if (source == null)
            {
                return target;
            }

            for (int i = 0; i < source.Entries.Count; i++)
            {
                GraphBlackboardEntry sourceEntry = source.Entries[i];

                if (sourceEntry == null || string.IsNullOrWhiteSpace(sourceEntry.Key))
                {
                    continue;
                }

                GraphBlackboardValue migratedValue = CreateGraphBlackboardValue(
                    sourceEntry.Value);
                Type expectedValueType = migratedValue?.GetExpectedValueType();

                if (migratedValue == null
                    || expectedValueType == null
                    || !target.TrySetBoxedValue(
                        sourceEntry.Key,
                        migratedValue.GetBoxedValue(),
                        expectedValueType)
                    || !target.TryGetEntry(sourceEntry.Key, out CutsceneGraphBlackboardEntry targetEntry))
                {
                    continue;
                }

                targetEntry.AssignId(sourceEntry.Id);
            }

            target.EnsureEntryIds();
            return target;
        }

        /// <summary>
        /// Creates one GraphCore blackboard copy from one legacy Cutscenes blackboard.
        /// </summary>
        /// <param name="source">Legacy Cutscenes blackboard.</param>
        /// <returns>The migrated GraphCore blackboard.</returns>
        public static GraphBlackboard CreateGraphBlackboard(CutsceneGraphBlackboard source)
        {
            GraphBlackboard target = new();

            if (source == null)
            {
                return target;
            }

            for (int i = 0; i < source.Entries.Count; i++)
            {
                CutsceneGraphBlackboardEntry sourceEntry = source.Entries[i];

                if (sourceEntry == null || string.IsNullOrWhiteSpace(sourceEntry.Key))
                {
                    continue;
                }

                GraphBlackboardValue migratedValue = CreateGraphBlackboardValue(
                    sourceEntry.Value);
                Type expectedValueType = migratedValue?.GetExpectedValueType();

                if (migratedValue == null
                    || expectedValueType == null
                    || !target.TrySetBoxedValue(
                        sourceEntry.Key,
                        migratedValue.GetBoxedValue(),
                        expectedValueType,
                        CutsceneGraphFamily.Id)
                    || !target.TryGetEntry(sourceEntry.Key, out GraphBlackboardEntry targetEntry))
                {
                    continue;
                }

                targetEntry.AssignId(sourceEntry.Id);
            }

            target.EnsureEntryIds();
            return target;
        }

        /// <summary>
        /// Creates one GraphCore value-source copy from one legacy Cutscenes value-source.
        /// </summary>
        /// <param name="source">Legacy Cutscenes value-source.</param>
        /// <returns>The migrated GraphCore value-source.</returns>
        public static GraphValueSource CreateGraphValueSource(CutsceneValueSource source)
        {
            if (source == null)
            {
                return null;
            }

            GraphValueSource target = new();
            Type expectedValueType = source.ExpectedValueType;

            if (expectedValueType != null)
            {
                target.SetExpectedValueType(expectedValueType, CutsceneGraphFamily.Id);
            }

            if (source.Mode == CutsceneValueSourceMode.Blackboard)
            {
                CopyBlackboardBinding(source.BlackboardVariable, target.BlackboardVariable);
                target.Mode = GraphValueSourceMode.Blackboard;
                return target;
            }

            GraphBlackboardValue migratedDirectValue = CreateGraphBlackboardValue(
                source.DirectValue);

            if (migratedDirectValue != null)
            {
                target.SetExpectedValueType(
                    migratedDirectValue.GetExpectedValueType(),
                    CutsceneGraphFamily.Id);
                target.DirectValue?.TrySetBoxedValue(migratedDirectValue.GetBoxedValue());
            }

            target.Mode = GraphValueSourceMode.Direct;
            return target;
        }

        /// <summary>
        /// Creates one legacy Cutscenes value-source copy from one GraphCore value-source.
        /// </summary>
        /// <param name="source">GraphCore value-source to reconstruct.</param>
        /// <returns>The reconstructed Cutscenes value-source.</returns>
        public static CutsceneValueSource CreateCutsceneValueSource(GraphValueSource source)
        {
            if (source == null)
            {
                return null;
            }

            CutsceneValueSource target = new();
            Type expectedValueType = source.ExpectedValueType;

            if (expectedValueType != null)
            {
                target.SetExpectedValueType(expectedValueType);
            }

            if (source.Mode == GraphValueSourceMode.Blackboard)
            {
                CopyBlackboardBinding(source.BlackboardVariable, target.BlackboardVariable);
                target.Mode = CutsceneValueSourceMode.Blackboard;
                return target;
            }

            GraphBlackboardValue migratedDirectValue = CreateGraphBlackboardValue(
                source.DirectValue);

            if (migratedDirectValue != null)
            {
                target.SetExpectedValueType(migratedDirectValue.GetExpectedValueType());
                target.DirectValue?.TrySetBoxedValue(migratedDirectValue.GetBoxedValue());
            }

            target.Mode = CutsceneValueSourceMode.Direct;
            return target;
        }

        /// <summary>
        /// Creates one GraphCore variable-reference copy from one legacy Cutscenes reference.
        /// </summary>
        /// <param name="source">Legacy Cutscenes variable-reference.</param>
        /// <returns>The migrated GraphCore variable-reference.</returns>
        public static GraphBlackboardVariableReference CreateGraphVariableReference(
            CutsceneBlackboardVariableReference source)
        {
            GraphBlackboardVariableReference target = new();
            CopyBlackboardBinding(source, target);
            return target;
        }

        /// <summary>
        /// Creates one legacy Cutscenes variable-reference copy from one GraphCore reference.
        /// </summary>
        /// <param name="source">GraphCore variable-reference to reconstruct.</param>
        /// <returns>The reconstructed Cutscenes variable-reference.</returns>
        public static CutsceneBlackboardVariableReference CreateCutsceneVariableReference(
            GraphBlackboardVariableReference source)
        {
            CutsceneBlackboardVariableReference target = new();
            CopyBlackboardBinding(source, target);
            return target;
        }

        /// <summary>
        /// Creates one GraphCore blackboard value wrapper from one legacy Cutscenes wrapper.
        /// </summary>
        /// <param name="source">Legacy Cutscenes blackboard value wrapper.</param>
        /// <returns>The migrated GraphCore wrapper.</returns>
        public static GraphBlackboardValue CreateGraphBlackboardValue(
            GraphBlackboardValue source)
        {
            if (source == null)
            {
                return null;
            }

            Type expectedValueType = source.GetExpectedValueType();

            if (expectedValueType == null
                || !GraphBlackboardValueRegistry.TryCreateValue(
                    expectedValueType,
                    CutsceneGraphFamily.Id,
                    out GraphBlackboardValue target))
            {
                return null;
            }

            target.InitializeForValueType(expectedValueType);

            if (!target.TrySetBoxedValue(source.GetBoxedValue()))
            {
                return null;
            }

            return target;
        }

        /// <summary>
        /// Creates one legacy Cutscenes blackboard value wrapper from one GraphCore wrapper.
        /// </summary>
        /// <param name="source">GraphCore blackboard value wrapper to reconstruct.</param>
        /// <returns>The reconstructed Cutscenes wrapper.</returns>
        public static CutsceneGraphBlackboardValue CreateCutsceneGraphBlackboardValue(
            GraphBlackboardValue source)
        {
            if (source == null)
            {
                return null;
            }

            Type expectedValueType = source.GetExpectedValueType();

            if (expectedValueType == null
                || !CutsceneBlackboardValueRegistry.TryCreateValue(
                    expectedValueType,
                    out CutsceneGraphBlackboardValue target))
            {
                return null;
            }

            target.InitializeForValueType(expectedValueType);

            if (!target.TrySetBoxedValue(source.GetBoxedValue()))
            {
                return null;
            }

            return target;
        }

        private static void CopyBlackboardBinding(
            CutsceneBlackboardVariableReference source,
            GraphBlackboardVariableReference target)
        {
            if (source == null || target == null)
            {
                return;
            }

            if (!source.IsAssigned)
            {
                target.Clear();
                return;
            }

            target.BindScoped(
                GraphBlackboardReferenceScope.GraphLocal,
                string.Empty,
                source.EntryId,
                source.EntryKey,
                source.ValueType);
        }

        private static void CopyBlackboardBinding(
            GraphBlackboardVariableReference source,
            CutsceneBlackboardVariableReference target)
        {
            if (source == null || target == null)
            {
                return;
            }

            if (!source.IsAssigned)
            {
                target.Clear();
                return;
            }

            SerializableGuid entryId = source.Scope == GraphBlackboardReferenceScope.GraphLocal
                ? source.EntryId
                : SerializableGuid.Empty;

            target.RestoreBinding(entryId, source.EntryKey, source.ValueType);
        }

        private static List<GraphPortDefinition> CreatePortDefinitions(
            IReadOnlyList<CutsceneNodePort> sourcePorts)
        {
            if (sourcePorts == null || sourcePorts.Count <= 0)
            {
                return new List<GraphPortDefinition>();
            }

            List<GraphPortDefinition> targetPorts = new(sourcePorts.Count);

            for (int i = 0; i < sourcePorts.Count; i++)
            {
                CutsceneNodePort sourcePort = sourcePorts[i];

                if (sourcePort == null)
                {
                    continue;
                }

                targetPorts.Add(new GraphPortDefinition(
                    sourcePort.Key,
                    sourcePort.DisplayName,
                    sourcePort.IsMandatory));
            }

            return targetPorts;
        }

        private static string GetAuthoredNodeTitle(CutsceneNodeBase sourceNode)
        {
            return sourceNode?.AuthoredTitle ?? string.Empty;
        }

        /// <summary>
        /// Rebuilds one authored Cutscenes node shell from one GraphCore-authored node.
        /// </summary>
        /// <param name="sourceNode">GraphCore-authored node to rebuild.</param>
        /// <returns>The reconstructed Cutscenes-authored node when supported.</returns>
        internal static CutsceneNodeBase CreateCutsceneAuthoringNode(GraphNodeBase sourceNode)
        {
            return CreateCutsceneNode(sourceNode);
        }

        private static CutsceneNodeBase CreateCutsceneNode(GraphNodeBase sourceNode)
        {
            if (sourceNode == null)
            {
                return null;
            }

            CutsceneNodeBase targetNode = sourceNode switch
            {
                CutsceneEntryGraphNode => new CutsceneEntryNode(),
                CutsceneFinishGraphNode => new CutsceneFinishNode(),
                CutsceneParallelGraphNode parallelNode => CreateCutsceneParallelNode(parallelNode),
                CutsceneWaitGraphNode waitNode => CreateCutsceneWaitNode(waitNode),
                CutsceneWaitForEventGraphNode waitForEventNode =>
                    CreateCutsceneWaitForEventNode(waitForEventNode),
                CutscenePlayTimelineGraphNode playTimelineNode =>
                    CreateCutscenePlayTimelineNode(playTimelineNode),
                CutsceneDialogueConversationGraphNode dialogueConversationNode =>
                    CreateCutsceneDialogueConversationNode(dialogueConversationNode),
                CutsceneSetGameObjectActiveGraphNode setGameObjectActiveNode =>
                    CreateCutsceneSetGameObjectActiveNode(setGameObjectActiveNode),
                CutsceneAnimatorTriggerGraphNode animatorTriggerNode =>
                    CreateCutsceneAnimatorTriggerNode(animatorTriggerNode),
                CutsceneEmitHandyBusEventGraphNode emitHandyBusEventNode =>
                    CreateCutsceneEmitHandyBusEventNode(emitHandyBusEventNode),
                CutsceneGraphNodeAdapter adapterNode => CreateCutsceneNodeFromAdapter(adapterNode),
                _ => CreateCutsceneNodeFromLegacyTypeMetadata(sourceNode),
            };

            if (targetNode == null)
            {
                return null;
            }

            targetNode.RestoreAuthoringState(
                sourceNode.Id,
                sourceNode.Title,
                sourceNode.Position);
            return targetNode;
        }

        private static CutsceneNodeBase CreateCutsceneNodeFromAdapter(CutsceneGraphNodeAdapter sourceNode)
        {
            if (sourceNode?.SourceNode == null)
            {
                return CreateCutsceneNodeFromLegacyTypeMetadata(sourceNode);
            }

            return SerializationUtility.CreateCopy(sourceNode.SourceNode) as CutsceneNodeBase
                ?? CreateCutsceneNodeFromLegacyTypeMetadata(sourceNode);
        }

        private static CutsceneNodeBase CreateCutsceneNodeFromLegacyTypeMetadata(GraphNodeBase sourceNode)
        {
            return TryGetLegacyCutsceneNodeType(sourceNode, out Type nodeType)
                && Activator.CreateInstance(nodeType) is CutsceneNodeBase targetNode
                ? targetNode
                : null;
        }

        private static CutsceneParallelNode CreateCutsceneParallelNode(
            CutsceneParallelGraphNode sourceNode)
        {
            CutsceneParallelNode targetNode = new();
            SetPrivateField(
                targetNode,
                "_branchCount",
                System.Math.Max(2, sourceNode.OutputPortCount));
            return targetNode;
        }

        private static CutsceneWaitNode CreateCutsceneWaitNode(CutsceneWaitGraphNode sourceNode)
        {
            CutsceneWaitNode targetNode = new();
            SetPrivateField(
                targetNode,
                "_durationSource",
                CreateCutsceneValueSource(sourceNode.DurationSource));
            SetPrivateField(
                targetNode,
                "_timeModeSource",
                CreateCutsceneValueSource(sourceNode.TimeModeSource));
            return targetNode;
        }

        private static CutsceneWaitForEventNode CreateCutsceneWaitForEventNode(
            CutsceneWaitForEventGraphNode sourceNode)
        {
            CutsceneWaitForEventNode targetNode = new();
            SetPrivateField(targetNode, "_eventSelector", CopyEventSelector(sourceNode.EventSelector));
            return targetNode;
        }

        private static CutscenePlayTimelineNode CreateCutscenePlayTimelineNode(
            CutscenePlayTimelineGraphNode sourceNode)
        {
            CutscenePlayTimelineNode targetNode = new();
            SetPrivateField(
                targetNode,
                "_playableDirectorSource",
                CreateCutsceneValueSource(sourceNode.PlayableDirectorSource));
            SetPrivateField(
                targetNode,
                "_restartOnEnterSource",
                CreateCutsceneValueSource(sourceNode.RestartOnEnterSource));
            SetPrivateField(
                targetNode,
                "_stopOnExitSource",
                CreateCutsceneValueSource(sourceNode.StopOnExitSource));
            return targetNode;
        }

        private static CutsceneDialogueConversationNode CreateCutsceneDialogueConversationNode(
            CutsceneDialogueConversationGraphNode sourceNode)
        {
            CutsceneDialogueConversationNode targetNode = new();
            SetPrivateField(
                targetNode,
                "_conversationTitleSource",
                CreateCutsceneValueSource(sourceNode.ConversationTitleSource));
            SetPrivateField(
                targetNode,
                "_databaseKeySource",
                CreateCutsceneValueSource(sourceNode.DatabaseKeySource));
            SetPrivateField(
                targetNode,
                "_speakerSource",
                CreateCutsceneValueSource(sourceNode.SpeakerSource));
            SetPrivateField(
                targetNode,
                "_listenerSource",
                CreateCutsceneValueSource(sourceNode.ListenerSource));
            SetPrivateField(
                targetNode,
                "_waitForConversationEndSource",
                CreateCutsceneValueSource(sourceNode.WaitForConversationEndSource));
            SetPrivateField(
                targetNode,
                "_continueOnFailureSource",
                CreateCutsceneValueSource(sourceNode.ContinueOnFailureSource));
            return targetNode;
        }

        private static CutsceneSetGameObjectActiveNode CreateCutsceneSetGameObjectActiveNode(
            CutsceneSetGameObjectActiveGraphNode sourceNode)
        {
            CutsceneSetGameObjectActiveNode targetNode = new();
            SetPrivateField(targetNode, "_targetSource", CreateCutsceneValueSource(sourceNode.TargetSource));
            SetPrivateField(targetNode, "_activeSource", CreateCutsceneValueSource(sourceNode.ActiveSource));
            return targetNode;
        }

        private static CutsceneAnimatorTriggerNode CreateCutsceneAnimatorTriggerNode(
            CutsceneAnimatorTriggerGraphNode sourceNode)
        {
            CutsceneAnimatorTriggerNode targetNode = new();
            SetPrivateField(targetNode, "_animatorSource", CreateCutsceneValueSource(sourceNode.AnimatorSource));
            SetPrivateField(
                targetNode,
                "_triggerNameSource",
                CreateCutsceneValueSource(sourceNode.TriggerNameSource));
            SetPrivateField(
                targetNode,
                "_resetTriggerNameSource",
                CreateCutsceneValueSource(sourceNode.ResetTriggerNameSource));
            return targetNode;
        }

        private static CutsceneEmitHandyBusEventNode CreateCutsceneEmitHandyBusEventNode(
            CutsceneEmitHandyBusEventGraphNode sourceNode)
        {
            CutsceneEmitHandyBusEventNode targetNode = new();
            SetPrivateField(targetNode, "_eventSelector", CopyEventSelector(sourceNode.EventSelector));
            return targetNode;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(fieldName))
            {
                return;
            }

            Type targetType = target.GetType();
            FieldInfo field = targetType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            field?.SetValue(target, value);
        }

        private static bool TryResolveLegacyNodeType(
            string serializedTypeName,
            out Type nodeType)
        {
            nodeType = GraphSerializedTypeResolver.Resolve(serializedTypeName);

            return nodeType != null
                && typeof(CutsceneNodeBase).IsAssignableFrom(nodeType);
        }

        private static CutsceneBusEventSelector CopyEventSelector(CutsceneBusEventSelector source)
        {
            CutsceneBusEventSelector target = new();
            target.CopySerializedStateFrom(source);
            return target;
        }

        /// <summary>
        /// Stores one GraphCore-native entry-node snapshot for runtime migration.
        /// </summary>
        [Serializable]
        private sealed class CutsceneEntryGraphNode : GraphNodeBase, IGraphExecuteNode
        {
            private const string DefaultNodeTitle = "Entry";

            [SerializeField] private string _defaultTitle = DefaultNodeTitle;

            /// <summary>
            /// Initializes one native GraphCore entry-node snapshot from one legacy entry node.
            /// </summary>
            /// <param name="sourceNode">Legacy entry node that should be mirrored.</param>
            public CutsceneEntryGraphNode(CutsceneEntryNode sourceNode)
            {
                _defaultTitle = sourceNode?.DisplayTitle ?? DefaultNodeTitle;
                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            /// <inheritdoc />
            public bool TryExecute(IGraphNodeExecutionContext context)
            {
                return context != null
                    && context.TryComplete(GraphExecutionResult.Success(CutsceneNodePorts.Next));
            }
        }

        /// <summary>
        /// Stores one GraphCore-native finish-node snapshot for runtime migration.
        /// </summary>
        [Serializable]
        private sealed class CutsceneFinishGraphNode : GraphNodeBase, IGraphExecuteNode
        {
            private const string DefaultNodeTitle = "Finish";

            [SerializeField] private string _defaultTitle = DefaultNodeTitle;

            /// <summary>
            /// Initializes one native GraphCore finish-node snapshot from one legacy finish node.
            /// </summary>
            /// <param name="sourceNode">Legacy finish node that should be mirrored.</param>
            public CutsceneFinishGraphNode(CutsceneFinishNode sourceNode)
            {
                _defaultTitle = sourceNode?.DisplayTitle ?? DefaultNodeTitle;
                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            /// <inheritdoc />
            public override IReadOnlyList<GraphPortDefinition> GetOutputPorts()
            {
                return GraphPortDefinition.None;
            }

            /// <inheritdoc />
            public bool TryExecute(IGraphNodeExecutionContext context)
            {
                return context != null
                    && context.TryComplete(GraphExecutionResult.Success(CutsceneNodePorts.Complete));
            }
        }

        /// <summary>
        /// Stores one GraphCore-native parallel-node snapshot for runtime migration.
        /// </summary>
        [Serializable]
        private sealed class CutsceneParallelGraphNode : GraphNodeBase, IGraphExecuteNode
        {
            private const string DefaultNodeTitle = "Fork";

            [SerializeField] private string _defaultTitle = DefaultNodeTitle;
            [SerializeField] private string _summary = string.Empty;
            [SerializeField] private List<GraphPortDefinition> _outputPorts = new();

            /// <summary>
            /// Initializes one native GraphCore parallel-node snapshot from one legacy parallel node.
            /// </summary>
            /// <param name="sourceNode">Legacy parallel node that should be mirrored.</param>
            public CutsceneParallelGraphNode(CutsceneParallelNode sourceNode)
            {
                _defaultTitle = sourceNode?.DisplayTitle ?? DefaultNodeTitle;
                _summary = sourceNode?.GetSummary() ?? string.Empty;
                _outputPorts = CreatePortDefinitions(sourceNode?.GetOutputPorts());
                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            internal int OutputPortCount => _outputPorts?.Count ?? 0;

            /// <inheritdoc />
            public override IReadOnlyList<GraphPortDefinition> GetOutputPorts()
            {
                return _outputPorts;
            }

            /// <inheritdoc />
            public override string GetSummary()
            {
                return _summary;
            }

            /// <inheritdoc />
            public bool TryExecute(IGraphNodeExecutionContext context)
            {
                return context != null
                    && context.TryComplete(GraphExecutionResult.Success());
            }
        }

        /// <summary>
        /// Stores one GraphCore-native wait-node snapshot for runtime migration.
        /// </summary>
        [Serializable]
        private sealed class CutsceneWaitGraphNode : GraphNodeBase, IGraphEnterNode, IGraphTickNode
        {
            private const string DefaultNodeTitle = "Wait";
            private const string ElapsedStateKey = "Elapsed";
            private const string DurationStateKey = "Duration";
            private const string TimeModeStateKey = "TimeMode";

            [SerializeField] private string _defaultTitle = DefaultNodeTitle;
            [SerializeField] private string _summary = string.Empty;
            [SerializeField]
            private GraphValueSource _durationSource =
                GraphValueSource.CreateDirect(1f, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _timeModeSource =
                GraphValueSource.CreateDirect(CutsceneTimeMode.Scaled, CutsceneGraphFamily.Id);

            /// <summary>
            /// Initializes one native GraphCore wait-node snapshot from one legacy wait node.
            /// </summary>
            /// <param name="sourceNode">Legacy wait node that should be mirrored.</param>
            public CutsceneWaitGraphNode(CutsceneWaitNode sourceNode)
            {
                _defaultTitle = sourceNode?.DisplayTitle ?? DefaultNodeTitle;
                _summary = sourceNode?.GetSummary() ?? string.Empty;
                _durationSource = CreateGraphValueSource(sourceNode?.DurationSource)
                    ?? GraphValueSource.CreateDirect(1f, CutsceneGraphFamily.Id);
                _timeModeSource = CreateGraphValueSource(sourceNode?.TimeModeSource)
                    ?? GraphValueSource.CreateDirect(CutsceneTimeMode.Scaled, CutsceneGraphFamily.Id);
                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
                EnsureValueSourcesConfigured();
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            internal GraphValueSource DurationSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _durationSource;
                }
            }

            internal GraphValueSource TimeModeSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _timeModeSource;
                }
            }

            /// <inheritdoc />
            public override bool RequiresTick => true;

            /// <inheritdoc />
            public override string GetSummary()
            {
                return _summary;
            }

            /// <summary>
            /// Executes the native wait enter logic.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the node enter path was accepted.</returns>
            public bool TryEnter(IGraphNodeExecutionContext context)
            {
                EnsureValueSourcesConfigured();

                if (!_durationSource.TryGetValue(context.RuntimeBlackboard, out float duration))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Wait node requires one valid duration source."));
                }

                if (!_timeModeSource.TryGetValue(context.RuntimeBlackboard, out CutsceneTimeMode timeMode))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Wait node requires one valid time-mode source."));
                }

                duration = Mathf.Max(0f, duration);
                context.SetNodeState(ElapsedStateKey, 0f);
                context.SetNodeState(DurationStateKey, duration);
                context.SetNodeState(TimeModeStateKey, timeMode);

                if (duration <= 0f)
                {
                    return context.TryComplete(GraphExecutionResult.Success());
                }

                return true;
            }

            /// <summary>
            /// Executes the native wait tick logic.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the node tick path was accepted.</returns>
            public bool TryTick(IGraphNodeExecutionContext context)
            {
                if (context is not IGraphNodeTimeContext timeContext)
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Wait node requires one execution context that exposes delta-time data."));
                }

                context.TryGetNodeState(ElapsedStateKey, out float elapsed);

                if (!context.TryGetNodeState(DurationStateKey, out float duration)
                    || !context.TryGetNodeState(TimeModeStateKey, out CutsceneTimeMode timeMode))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Wait node lost its resolved runtime state."));
                }

                elapsed += timeMode == CutsceneTimeMode.Unscaled
                    ? timeContext.UnscaledDeltaTime
                    : timeContext.DeltaTime;
                context.SetNodeState(ElapsedStateKey, elapsed);

                if (elapsed >= duration)
                {
                    return context.TryComplete(GraphExecutionResult.Success());
                }

                return true;
            }

            private void EnsureValueSourcesConfigured()
            {
                _durationSource ??= GraphValueSource.CreateDirect(1f, CutsceneGraphFamily.Id);
                _timeModeSource ??= GraphValueSource.CreateDirect(
                    CutsceneTimeMode.Scaled,
                    CutsceneGraphFamily.Id);

                _durationSource.SetExpectedValueType(typeof(float), CutsceneGraphFamily.Id);
                _timeModeSource.SetExpectedValueType(
                    typeof(CutsceneTimeMode),
                    CutsceneGraphFamily.Id);
            }
        }

        /// <summary>
        /// Stores one GraphCore-native wait-for-event snapshot for runtime migration.
        /// </summary>
        [Serializable]
        private sealed class CutsceneWaitForEventGraphNode : GraphNodeBase, IGraphEnterNode, IGraphExitNode
        {
            private const string DefaultNodeTitle = "Wait For Event";
            private const string RuntimeStateKey = "RuntimeState";

            [SerializeField] private string _defaultTitle = DefaultNodeTitle;
            [SerializeField] private string _summary = string.Empty;
            [SerializeField] private CutsceneBusEventSelector _eventSelector = new();

            /// <summary>
            /// Initializes one native GraphCore wait-for-event snapshot from one legacy node.
            /// </summary>
            /// <param name="sourceNode">Legacy wait-for-event node that should be mirrored.</param>
            public CutsceneWaitForEventGraphNode(CutsceneWaitForEventNode sourceNode)
            {
                _defaultTitle = sourceNode?.DisplayTitle ?? DefaultNodeTitle;
                _summary = sourceNode?.GetSummary() ?? string.Empty;
                _eventSelector = CopyEventSelector(sourceNode?.EventSelector);
                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            internal CutsceneBusEventSelector EventSelector => _eventSelector ??= new();

            /// <inheritdoc />
            public override string GetSummary()
            {
                return _summary;
            }

            /// <summary>
            /// Enters the native wait-for-event runtime path.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the enter path was accepted.</returns>
            public bool TryEnter(IGraphNodeExecutionContext context)
            {
                RuntimeState state = context.GetOrCreateNodeState(
                    RuntimeStateKey,
                    static () => new RuntimeState());
                state.Received = false;

                DisposeRuntimeState(state);

                SerializableGuid executionId = context.CurrentNodeExecutionId;

                if (_eventSelector.SelectionMode
                    == CutsceneBusEventSelector.EventSelectionMode.RegisteredEvent)
                {
                    if (!CutsceneBusEventRegistry.TrySubscribe(
                            _eventSelector.EventReference,
                            _ =>
                            {
                                state.Received = true;
                                context.TryCompleteNode(
                                    executionId,
                                    GraphExecutionResult.Success());
                            },
                            out IDisposable subscription))
                    {
                        return context.TryComplete(GraphExecutionResult.Failure(
                            "Wait For Event node requires one valid registered event selection."));
                    }

                    state.Subscription = subscription;
                    context.SetNodeState(RuntimeStateKey, state);
                    return true;
                }

                state.Binding = new EventBinding<CutsceneExternalEventRaisedEvent>(cutsceneEvent =>
                {
                    if (!string.Equals(
                            cutsceneEvent.EventName,
                            _eventSelector.EventName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    state.Received = true;
                    context.TryCompleteNode(executionId, GraphExecutionResult.Success());
                });

                HandyBus<CutsceneExternalEventRaisedEvent>.Register(state.Binding);
                context.SetNodeState(RuntimeStateKey, state);
                return true;
            }

            /// <summary>
            /// Exits the native wait-for-event runtime path.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the exit path was accepted.</returns>
            public bool TryExit(IGraphNodeExecutionContext context)
            {
                if (!context.TryGetNodeState(RuntimeStateKey, out RuntimeState state))
                {
                    return true;
                }

                DisposeRuntimeState(state);
                context.SetNodeState(RuntimeStateKey, state);
                return true;
            }

            private static void DisposeRuntimeState(RuntimeState state)
            {
                if (state == null)
                {
                    return;
                }

                state.Subscription?.Dispose();
                state.Subscription = null;

                if (state.Binding != null)
                {
                    HandyBus<CutsceneExternalEventRaisedEvent>.Deregister(state.Binding);
                    state.Binding = null;
                }
            }

            private sealed class RuntimeState
            {
                public bool Received;
                public EventBinding<CutsceneExternalEventRaisedEvent> Binding;
                public IDisposable Subscription;
            }
        }

        /// <summary>
        /// Stores one GraphCore-native play-timeline snapshot for runtime migration.
        /// </summary>
        [Serializable]
        private sealed class CutscenePlayTimelineGraphNode : GraphNodeBase, IGraphEnterNode, IGraphExitNode
        {
            private const string DefaultNodeTitle = "Play Timeline";
            private const string RuntimeStateKey = "RuntimeState";

            [SerializeField] private string _defaultTitle = DefaultNodeTitle;
            [SerializeField] private string _summary = string.Empty;
            [SerializeField]
            private GraphValueSource _playableDirectorSource =
                GraphValueSource.CreateDirect<PlayableDirector>(null, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _restartOnEnterSource =
                GraphValueSource.CreateDirect(true, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _stopOnExitSource =
                GraphValueSource.CreateDirect(true, CutsceneGraphFamily.Id);

            /// <summary>
            /// Initializes one native GraphCore play-timeline snapshot from one legacy node.
            /// </summary>
            /// <param name="sourceNode">Legacy play-timeline node that should be mirrored.</param>
            public CutscenePlayTimelineGraphNode(CutscenePlayTimelineNode sourceNode)
            {
                _defaultTitle = sourceNode?.DisplayTitle ?? DefaultNodeTitle;
                _summary = sourceNode?.GetSummary() ?? string.Empty;
                _playableDirectorSource = CreateGraphValueSource(sourceNode?.PlayableDirectorSource)
                    ?? GraphValueSource.CreateDirect<PlayableDirector>(null, CutsceneGraphFamily.Id);
                _restartOnEnterSource = CreateGraphValueSource(sourceNode?.RestartOnEnterSource)
                    ?? GraphValueSource.CreateDirect(true, CutsceneGraphFamily.Id);
                _stopOnExitSource = CreateGraphValueSource(sourceNode?.StopOnExitSource)
                    ?? GraphValueSource.CreateDirect(true, CutsceneGraphFamily.Id);
                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
                EnsureValueSourcesConfigured();
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            internal GraphValueSource PlayableDirectorSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _playableDirectorSource;
                }
            }

            internal GraphValueSource RestartOnEnterSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _restartOnEnterSource;
                }
            }

            internal GraphValueSource StopOnExitSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _stopOnExitSource;
                }
            }

            /// <inheritdoc />
            public override string GetSummary()
            {
                return _summary;
            }

            /// <summary>
            /// Enters the native play-timeline runtime path.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the enter path was accepted.</returns>
            public bool TryEnter(IGraphNodeExecutionContext context)
            {
                EnsureValueSourcesConfigured();

                if (!_playableDirectorSource.TryGetValue(
                        context.RuntimeBlackboard,
                        out PlayableDirector playableDirector))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Play Timeline node requires one valid PlayableDirector source."));
                }

                if (!_restartOnEnterSource.TryGetValue(context.RuntimeBlackboard, out bool restartOnEnter)
                    || !_stopOnExitSource.TryGetValue(context.RuntimeBlackboard, out bool stopOnExit))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Play Timeline node requires valid bool sources for enter and exit behavior."));
                }

                if (playableDirector == null || playableDirector.playableAsset == null)
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Play Timeline node requires the PlayableDirector to reference a Timeline asset."));
                }

                RuntimeState state = context.GetOrCreateNodeState(
                    RuntimeStateKey,
                    static () => new RuntimeState());
                Unbind(state);
                state.StopOnExit = stopOnExit;

                if (restartOnEnter)
                {
                    if (playableDirector.state == PlayState.Playing)
                    {
                        playableDirector.Stop();
                    }

                    playableDirector.time = 0d;
                    playableDirector.Evaluate();
                }

                SerializableGuid executionId = context.CurrentNodeExecutionId;
                PlayableDirector targetDirector = playableDirector;

                state.Director = targetDirector;
                state.StoppedHandler = _ => context.TryCompleteNode(
                    executionId,
                    GraphExecutionResult.Success());

                targetDirector.stopped += state.StoppedHandler;
                context.SetNodeState(RuntimeStateKey, state);
                targetDirector.Play();
                return true;
            }

            /// <summary>
            /// Exits the native play-timeline runtime path.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the exit path was accepted.</returns>
            public bool TryExit(IGraphNodeExecutionContext context)
            {
                if (!context.TryGetNodeState(RuntimeStateKey, out RuntimeState state))
                {
                    return true;
                }

                PlayableDirector boundDirector = state.Director;
                Unbind(state);

                if (state.StopOnExit
                    && boundDirector != null
                    && boundDirector.state == PlayState.Playing)
                {
                    boundDirector.Stop();
                }

                context.SetNodeState(RuntimeStateKey, state);
                return true;
            }

            private void EnsureValueSourcesConfigured()
            {
                _playableDirectorSource ??= GraphValueSource.CreateDirect<PlayableDirector>(
                    null,
                    CutsceneGraphFamily.Id);
                _restartOnEnterSource ??= GraphValueSource.CreateDirect(
                    true,
                    CutsceneGraphFamily.Id);
                _stopOnExitSource ??= GraphValueSource.CreateDirect(
                    true,
                    CutsceneGraphFamily.Id);

                _playableDirectorSource.SetExpectedValueType(
                    typeof(PlayableDirector),
                    CutsceneGraphFamily.Id);
                _restartOnEnterSource.SetExpectedValueType(
                    typeof(bool),
                    CutsceneGraphFamily.Id);
                _stopOnExitSource.SetExpectedValueType(
                    typeof(bool),
                    CutsceneGraphFamily.Id);
            }

            private static void Unbind(RuntimeState state)
            {
                if (state == null)
                {
                    return;
                }

                if (state.Director != null && state.StoppedHandler != null)
                {
                    state.Director.stopped -= state.StoppedHandler;
                }

                state.Director = null;
                state.StoppedHandler = null;
            }

            private sealed class RuntimeState
            {
                public PlayableDirector Director;
                public Action<PlayableDirector> StoppedHandler;
                public bool StopOnExit;
            }
        }

        /// <summary>
        /// Stores one GraphCore-native dialogue-conversation snapshot for runtime migration.
        /// </summary>
        [Serializable]
        private sealed class CutsceneDialogueConversationGraphNode : GraphNodeBase
        {
            private const string DefaultNodeTitle = "Start Conversation";
            private const string HandleStateKey = "DialogueHandle";

            [SerializeField] private string _defaultTitle = DefaultNodeTitle;
            [SerializeField] private string _summary = string.Empty;
            [SerializeField]
            private GraphValueSource _conversationTitleSource =
                GraphValueSource.CreateDirect(string.Empty, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _databaseKeySource =
                GraphValueSource.CreateDirect(string.Empty, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _speakerSource =
                GraphValueSource.CreateDirect<Transform>(null, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _listenerSource =
                GraphValueSource.CreateDirect<Transform>(null, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _waitForConversationEndSource =
                GraphValueSource.CreateDirect(true, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _continueOnFailureSource =
                GraphValueSource.CreateDirect(false, CutsceneGraphFamily.Id);

            /// <summary>
            /// Initializes one native GraphCore dialogue-conversation snapshot from one legacy node.
            /// </summary>
            /// <param name="sourceNode">Legacy dialogue-conversation node that should be mirrored.</param>
            public CutsceneDialogueConversationGraphNode(
                CutsceneDialogueConversationNode sourceNode)
            {
                _defaultTitle = sourceNode?.DisplayTitle ?? DefaultNodeTitle;
                _summary = sourceNode?.GetSummary() ?? string.Empty;
                _conversationTitleSource = CreateGraphValueSource(sourceNode?.ConversationTitleSource)
                    ?? GraphValueSource.CreateDirect(string.Empty, CutsceneGraphFamily.Id);
                _databaseKeySource = CreateGraphValueSource(sourceNode?.DatabaseKeySource)
                    ?? GraphValueSource.CreateDirect(string.Empty, CutsceneGraphFamily.Id);
                _speakerSource = CreateGraphValueSource(sourceNode?.SpeakerSource)
                    ?? GraphValueSource.CreateDirect<Transform>(null, CutsceneGraphFamily.Id);
                _listenerSource = CreateGraphValueSource(sourceNode?.ListenerSource)
                    ?? GraphValueSource.CreateDirect<Transform>(null, CutsceneGraphFamily.Id);
                _waitForConversationEndSource =
                    CreateGraphValueSource(sourceNode?.WaitForConversationEndSource)
                    ?? GraphValueSource.CreateDirect(true, CutsceneGraphFamily.Id);
                _continueOnFailureSource =
                    CreateGraphValueSource(sourceNode?.ContinueOnFailureSource)
                    ?? GraphValueSource.CreateDirect(false, CutsceneGraphFamily.Id);
                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
                EnsureValueSourcesConfigured();
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            internal GraphValueSource ConversationTitleSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _conversationTitleSource;
                }
            }

            internal GraphValueSource DatabaseKeySource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _databaseKeySource;
                }
            }

            internal GraphValueSource SpeakerSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _speakerSource;
                }
            }

            internal GraphValueSource ListenerSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _listenerSource;
                }
            }

            internal GraphValueSource WaitForConversationEndSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _waitForConversationEndSource;
                }
            }

            internal GraphValueSource ContinueOnFailureSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _continueOnFailureSource;
                }
            }

            /// <inheritdoc />
            public override bool RequiresTick
            {
                get
                {
                    EnsureValueSourcesConfigured();

                    if (_waitForConversationEndSource.Mode == GraphValueSourceMode.Blackboard)
                    {
                        return true;
                    }

                    return _waitForConversationEndSource.DirectValue?.GetBoxedValue() as bool?
                        ?? true;
                }
            }

            /// <inheritdoc />
            public override string GetSummary()
            {
                return _summary;
            }

            /// <summary>
            /// Enters the native dialogue-conversation runtime path.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the enter path was accepted.</returns>
            public bool TryEnter(CutsceneExecutionContext context)
            {
                EnsureValueSourcesConfigured();

                if (!_conversationTitleSource.TryGetValue(context.RuntimeBlackboard, out string conversationTitle)
                    || !_databaseKeySource.TryGetValue(context.RuntimeBlackboard, out string databaseKey)
                    || !_speakerSource.TryGetValue(context.RuntimeBlackboard, out Transform speaker)
                    || !_listenerSource.TryGetValue(context.RuntimeBlackboard, out Transform listener)
                    || !_waitForConversationEndSource.TryGetValue(
                        context.RuntimeBlackboard,
                        out bool waitForConversationEnd)
                    || !_continueOnFailureSource.TryGetValue(
                        context.RuntimeBlackboard,
                        out bool continueOnFailure))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Start Conversation node requires valid configured value sources."));
                }

                if (!context.Service.TryGetDialogueBridge(out ICutsceneDialogueBridge bridge)
                    || bridge == null
                    || !bridge.IsAvailable)
                {
                    return CompleteBridgeUnavailable(context, continueOnFailure);
                }

                CutsceneDialogueHandle handle = bridge.StartConversation(
                    new CutsceneDialogueRequest(
                        conversationTitle,
                        databaseKey,
                        speaker,
                        listener));

                context.SetNodeState(HandleStateKey, handle);

                if (!waitForConversationEnd)
                {
                    return context.TryComplete(GraphExecutionResult.Success());
                }

                return true;
            }

            /// <summary>
            /// Ticks the native dialogue-conversation runtime path.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the tick path was accepted.</returns>
            public bool TryTick(CutsceneExecutionContext context)
            {
                EnsureValueSourcesConfigured();

                if (!context.TryGetNodeState(HandleStateKey, out CutsceneDialogueHandle handle)
                    || !handle.IsValid
                    || !context.Service.TryGetDialogueBridge(out ICutsceneDialogueBridge bridge)
                    || bridge == null)
                {
                    return true;
                }

                if (!bridge.TryGetResult(handle, out CutsceneDialogueResult result)
                    || !result.HasCompleted)
                {
                    return true;
                }

                if (!_continueOnFailureSource.TryGetValue(
                        context.RuntimeBlackboard,
                        out bool continueOnFailure))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Start Conversation node requires one valid continue-on-failure source."));
                }

                if (result.Succeeded || continueOnFailure)
                {
                    return context.TryComplete(GraphExecutionResult.Success());
                }

                return context.TryComplete(GraphExecutionResult.Failure(result.FailureReason));
            }

            /// <summary>
            /// Exits the native dialogue-conversation runtime path.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the exit path was accepted.</returns>
            public bool TryExit(CutsceneExecutionContext context)
            {
                if (context.TryGetNodeState(HandleStateKey, out CutsceneDialogueHandle handle)
                    && handle.IsValid
                    && context.Service.TryGetDialogueBridge(out ICutsceneDialogueBridge bridge)
                    && bridge != null)
                {
                    bridge.CancelConversation(handle);
                }

                return true;
            }

            private bool CompleteBridgeUnavailable(
                CutsceneExecutionContext context,
                bool continueOnFailure)
            {
                if (continueOnFailure)
                {
                    return context.TryComplete(GraphExecutionResult.Success());
                }

                return context.TryComplete(GraphExecutionResult.Failure(
                    "Dialogue System bridge is unavailable for this cutscene conversation node."));
            }

            private void EnsureValueSourcesConfigured()
            {
                _conversationTitleSource ??= GraphValueSource.CreateDirect(
                    string.Empty,
                    CutsceneGraphFamily.Id);
                _databaseKeySource ??= GraphValueSource.CreateDirect(
                    string.Empty,
                    CutsceneGraphFamily.Id);
                _speakerSource ??= GraphValueSource.CreateDirect<Transform>(
                    null,
                    CutsceneGraphFamily.Id);
                _listenerSource ??= GraphValueSource.CreateDirect<Transform>(
                    null,
                    CutsceneGraphFamily.Id);
                _waitForConversationEndSource ??= GraphValueSource.CreateDirect(
                    true,
                    CutsceneGraphFamily.Id);
                _continueOnFailureSource ??= GraphValueSource.CreateDirect(
                    false,
                    CutsceneGraphFamily.Id);

                _conversationTitleSource.SetExpectedValueType(
                    typeof(string),
                    CutsceneGraphFamily.Id);
                _databaseKeySource.SetExpectedValueType(
                    typeof(string),
                    CutsceneGraphFamily.Id);
                _speakerSource.SetExpectedValueType(
                    typeof(Transform),
                    CutsceneGraphFamily.Id);
                _listenerSource.SetExpectedValueType(
                    typeof(Transform),
                    CutsceneGraphFamily.Id);
                _waitForConversationEndSource.SetExpectedValueType(
                    typeof(bool),
                    CutsceneGraphFamily.Id);
                _continueOnFailureSource.SetExpectedValueType(
                    typeof(bool),
                    CutsceneGraphFamily.Id);
            }
        }

        /// <summary>
        /// Stores one GraphCore-native set-gameobject-active snapshot for runtime migration.
        /// </summary>
        [Serializable]
        private sealed class CutsceneSetGameObjectActiveGraphNode : GraphNodeBase, IGraphExecuteNode
        {
            private const string DefaultNodeTitle = "Set GameObject Active";

            [SerializeField] private string _defaultTitle = DefaultNodeTitle;
            [SerializeField] private string _summary = string.Empty;
            [SerializeField]
            private GraphValueSource _targetSource =
                GraphValueSource.CreateDirect<GameObject>(null, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _activeSource =
                GraphValueSource.CreateDirect(true, CutsceneGraphFamily.Id);

            /// <summary>
            /// Initializes one native GraphCore set-gameobject-active snapshot from one legacy node.
            /// </summary>
            /// <param name="sourceNode">Legacy set-gameobject-active node that should be mirrored.</param>
            public CutsceneSetGameObjectActiveGraphNode(CutsceneSetGameObjectActiveNode sourceNode)
            {
                _defaultTitle = sourceNode?.DisplayTitle ?? DefaultNodeTitle;
                _summary = sourceNode?.GetSummary() ?? string.Empty;
                _targetSource = CreateGraphValueSource(sourceNode?.TargetSource)
                    ?? GraphValueSource.CreateDirect<GameObject>(null, CutsceneGraphFamily.Id);
                _activeSource = CreateGraphValueSource(sourceNode?.ActiveSource)
                    ?? GraphValueSource.CreateDirect(true, CutsceneGraphFamily.Id);
                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
                EnsureValueSourcesConfigured();
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            internal GraphValueSource TargetSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _targetSource;
                }
            }

            internal GraphValueSource ActiveSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _activeSource;
                }
            }

            /// <inheritdoc />
            public override string GetSummary()
            {
                return _summary;
            }

            /// <summary>
            /// Executes the native set-gameobject-active runtime path.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the execution path was accepted.</returns>
            public bool TryExecute(IGraphNodeExecutionContext context)
            {
                EnsureValueSourcesConfigured();

                if (!_targetSource.TryGetValue(context.RuntimeBlackboard, out UnityEngine.Object targetObject))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Set GameObject Active node requires one valid target source."));
                }

                if (!_activeSource.TryGetValue(context.RuntimeBlackboard, out bool isActive))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Set GameObject Active node requires one valid active-state source."));
                }

                GameObject targetGameObject = ResolveGameObject(targetObject);

                if (targetGameObject == null)
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Set GameObject Active node requires one GameObject or Component target."));
                }

                targetGameObject.SetActive(isActive);
                return context.TryComplete(GraphExecutionResult.Success());
            }

            private void EnsureValueSourcesConfigured()
            {
                _targetSource ??= GraphValueSource.CreateDirect<GameObject>(
                    null,
                    CutsceneGraphFamily.Id);
                _activeSource ??= GraphValueSource.CreateDirect(
                    true,
                    CutsceneGraphFamily.Id);

                _targetSource.SetExpectedValueType(
                    typeof(GameObject),
                    CutsceneGraphFamily.Id);
                _activeSource.SetExpectedValueType(
                    typeof(bool),
                    CutsceneGraphFamily.Id);
            }

            private static GameObject ResolveGameObject(UnityEngine.Object value)
            {
                return value switch
                {
                    GameObject gameObject => gameObject,
                    Component component => component.gameObject,
                    _ => null,
                };
            }
        }

        /// <summary>
        /// Stores one GraphCore-native animator-trigger snapshot for runtime migration.
        /// </summary>
        [Serializable]
        private sealed class CutsceneAnimatorTriggerGraphNode : GraphNodeBase, IGraphExecuteNode
        {
            private const string DefaultNodeTitle = "Animator Trigger";

            [SerializeField] private string _defaultTitle = DefaultNodeTitle;
            [SerializeField] private string _summary = string.Empty;
            [SerializeField]
            private GraphValueSource _animatorSource =
                GraphValueSource.CreateDirect<Animator>(null, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _triggerNameSource =
                GraphValueSource.CreateDirect(string.Empty, CutsceneGraphFamily.Id);
            [SerializeField]
            private GraphValueSource _resetTriggerNameSource =
                GraphValueSource.CreateDirect(string.Empty, CutsceneGraphFamily.Id);

            /// <summary>
            /// Initializes one native GraphCore animator-trigger snapshot from one legacy node.
            /// </summary>
            /// <param name="sourceNode">Legacy animator-trigger node that should be mirrored.</param>
            public CutsceneAnimatorTriggerGraphNode(CutsceneAnimatorTriggerNode sourceNode)
            {
                _defaultTitle = sourceNode?.DisplayTitle ?? DefaultNodeTitle;
                _summary = sourceNode?.GetSummary() ?? string.Empty;
                _animatorSource = CreateGraphValueSource(sourceNode?.AnimatorSource)
                    ?? GraphValueSource.CreateDirect<Animator>(null, CutsceneGraphFamily.Id);
                _triggerNameSource = CreateGraphValueSource(sourceNode?.TriggerNameSource)
                    ?? GraphValueSource.CreateDirect(string.Empty, CutsceneGraphFamily.Id);
                _resetTriggerNameSource = CreateGraphValueSource(sourceNode?.ResetTriggerNameSource)
                    ?? GraphValueSource.CreateDirect(string.Empty, CutsceneGraphFamily.Id);
                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
                EnsureValueSourcesConfigured();
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            internal GraphValueSource AnimatorSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _animatorSource;
                }
            }

            internal GraphValueSource TriggerNameSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _triggerNameSource;
                }
            }

            internal GraphValueSource ResetTriggerNameSource
            {
                get
                {
                    EnsureValueSourcesConfigured();
                    return _resetTriggerNameSource;
                }
            }

            /// <inheritdoc />
            public override string GetSummary()
            {
                return _summary;
            }

            /// <summary>
            /// Executes the native animator-trigger runtime path.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the execution path was accepted.</returns>
            public bool TryExecute(IGraphNodeExecutionContext context)
            {
                EnsureValueSourcesConfigured();

                if (!_animatorSource.TryGetValue(context.RuntimeBlackboard, out Animator animator))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Animator Trigger node requires one valid animator source."));
                }

                if (!_triggerNameSource.TryGetValue(context.RuntimeBlackboard, out string triggerName)
                    || string.IsNullOrWhiteSpace(triggerName))
                {
                    return context.TryComplete(GraphExecutionResult.Failure(
                        "Animator Trigger node requires one valid trigger source."));
                }

                string resetTriggerName = string.Empty;

                if (_resetTriggerNameSource.TryGetValue(
                        context.RuntimeBlackboard,
                        out string resolvedResetTriggerName))
                {
                    resetTriggerName = resolvedResetTriggerName ?? string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(resetTriggerName))
                {
                    animator.ResetTrigger(resetTriggerName);
                }

                animator.SetTrigger(triggerName);
                return context.TryComplete(GraphExecutionResult.Success());
            }

            private void EnsureValueSourcesConfigured()
            {
                _animatorSource ??= GraphValueSource.CreateDirect<Animator>(
                    null,
                    CutsceneGraphFamily.Id);
                _triggerNameSource ??= GraphValueSource.CreateDirect(
                    string.Empty,
                    CutsceneGraphFamily.Id);
                _resetTriggerNameSource ??= GraphValueSource.CreateDirect(
                    string.Empty,
                    CutsceneGraphFamily.Id);

                _animatorSource.SetExpectedValueType(
                    typeof(Animator),
                    CutsceneGraphFamily.Id);
                _triggerNameSource.SetExpectedValueType(
                    typeof(string),
                    CutsceneGraphFamily.Id);
                _resetTriggerNameSource.SetExpectedValueType(
                    typeof(string),
                    CutsceneGraphFamily.Id);
            }
        }

        /// <summary>
        /// Stores one GraphCore-native emit-handybus-event snapshot for runtime migration.
        /// </summary>
        [Serializable]
        private sealed class CutsceneEmitHandyBusEventGraphNode : GraphNodeBase, IGraphRuntimeNode
        {
            private const string DefaultNodeTitle = "Emit HandyBus Event";

            [SerializeField] private string _defaultTitle = DefaultNodeTitle;
            [SerializeField] private string _summary = string.Empty;
            [SerializeField] private CutsceneBusEventSelector _eventSelector = new();

            /// <summary>
            /// Initializes one native GraphCore emit-handybus-event snapshot from one legacy node.
            /// </summary>
            /// <param name="sourceNode">Legacy emit-handybus-event node that should be mirrored.</param>
            public CutsceneEmitHandyBusEventGraphNode(CutsceneEmitHandyBusEventNode sourceNode)
            {
                _defaultTitle = sourceNode?.DisplayTitle ?? DefaultNodeTitle;
                _summary = sourceNode?.GetSummary() ?? string.Empty;
                _eventSelector = CopyEventSelector(sourceNode?.EventSelector);
                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            internal CutsceneBusEventSelector EventSelector => _eventSelector ??= new();

            /// <inheritdoc />
            public override string GetSummary()
            {
                return _summary;
            }

            /// <summary>
            /// Executes the native emit-handybus-event runtime path.
            /// </summary>
            /// <param name="context">Execution context bound to the current node instance.</param>
            /// <returns>True when the execution path was accepted.</returns>
            public bool TryExecute(CutsceneExecutionContext context)
            {
                if (_eventSelector.SelectionMode
                    == CutsceneBusEventSelector.EventSelectionMode.RegisteredEvent)
                {
                    if (!CutsceneBusEventRegistry.TryCreateEventInstance(
                            _eventSelector.EventReference,
                            out IEvent cutsceneEvent)
                        || !CutsceneBusEventRegistry.TryDispatch(cutsceneEvent))
                    {
                        return context.TryComplete(GraphExecutionResult.Failure(
                            "Emit HandyBus Event node requires one valid registered event selection."));
                    }

                    return context.TryComplete(GraphExecutionResult.Success());
                }

                HandyBus<CutsceneExternalEventRaisedEvent>.Raise(
                    new CutsceneExternalEventRaisedEvent(
                        context.Director,
                        _eventSelector.EventName));
                return context.TryComplete(GraphExecutionResult.Success());
            }
        }

        /// <summary>
        /// Stores one GraphCore node snapshot that mirrors one legacy Cutscenes node.
        /// </summary>
        [Serializable]
        private sealed class CutsceneGraphNodeAdapter : GraphNodeBase
        {
            [SerializeReference] private CutsceneNodeBase _sourceNode;
            [SerializeField] private List<GraphPortDefinition> _outputPorts = new();
            [SerializeField] private string _defaultTitle = string.Empty;
            [SerializeField] private string _summary = string.Empty;
            [SerializeField] private string _legacyNodeTypeName = string.Empty;
            [SerializeField] private bool _hasInputPort = true;
            [SerializeField] private bool _participatesInAutoArrange = true;
            [SerializeField] private bool _participatesInTopologyValidation = true;
            [SerializeField] private bool _usesRuntimeStateStyling = true;
            [SerializeField] private bool _requiresTick;

            /// <summary>
            /// Initializes one GraphCore node snapshot from one legacy Cutscenes node.
            /// </summary>
            /// <param name="sourceNode">Legacy node that should be mirrored.</param>
            public CutsceneGraphNodeAdapter(CutsceneNodeBase sourceNode)
            {
                _sourceNode = sourceNode;
                _outputPorts = CreatePortDefinitions(sourceNode?.GetOutputPorts());
                _defaultTitle = sourceNode?.DisplayTitle ?? nameof(CutsceneNodeBase);
                _summary = sourceNode?.GetSummary() ?? string.Empty;
                _legacyNodeTypeName = sourceNode?.GetType().AssemblyQualifiedName ?? string.Empty;
                _hasInputPort = sourceNode?.HasInputPort ?? true;
                _participatesInAutoArrange = sourceNode?.ParticipatesInAutoArrange ?? true;
                _participatesInTopologyValidation =
                    sourceNode?.ParticipatesInTopologyValidation ?? true;
                _usesRuntimeStateStyling = sourceNode?.UsesRuntimeStateStyling ?? true;
                _requiresTick = sourceNode?.RequiresTick ?? false;

                Title = GetAuthoredNodeTitle(sourceNode);
                Position = sourceNode?.Position ?? default;
                RestoreId(sourceNode?.Id ?? SerializableGuid.Empty);
            }

            /// <summary>
            /// Gets the original Cutscenes node mirrored by this adapter.
            /// </summary>
            public CutsceneNodeBase SourceNode => _sourceNode;

            /// <summary>
            /// Gets the serialized legacy cutscene node type name preserved for deterministic migration.
            /// </summary>
            public string LegacyNodeTypeName => _legacyNodeTypeName;

            /// <summary>
            /// Attempts to resolve the serialized legacy cutscene node runtime type.
            /// </summary>
            /// <param name="nodeType">Resolved node type when available.</param>
            /// <returns>True when the stored type name resolves to one cutscene node type.</returns>
            public bool TryGetLegacyNodeType(out Type nodeType)
            {
                return TryResolveLegacyNodeType(_legacyNodeTypeName, out nodeType);
            }

            /// <inheritdoc />
            protected override string DefaultTitle => _defaultTitle;

            /// <inheritdoc />
            public override bool HasInputPort => _hasInputPort;

            /// <inheritdoc />
            public override bool ParticipatesInAutoArrange => _participatesInAutoArrange;

            /// <inheritdoc />
            public override bool ParticipatesInTopologyValidation => _participatesInTopologyValidation;

            /// <inheritdoc />
            public override bool UsesRuntimeStateStyling => _usesRuntimeStateStyling;

            /// <inheritdoc />
            public override bool RequiresTick => _requiresTick;

            /// <inheritdoc />
            public override IReadOnlyList<GraphPortDefinition> GetOutputPorts()
            {
                return _outputPorts;
            }

            /// <inheritdoc />
            public override string GetSummary()
            {
                return _summary;
            }
        }
    }
}