using System;
using System.Collections.Generic;
using System.Reflection;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [Serializable]
    public abstract class CutsceneNodeBase : GraphNodeBase
    {
        private static readonly Dictionary<Type, string> DefaultTitlesByType = new();

        [ShowInInspector, ReadOnly]
        public new string DisplayTitle => base.DisplayTitle;

        [ShowInInspector, ReadOnly]
        public string GraphNodeTitle => base.DisplayTitle;

        internal string AuthoredTitle => Title ?? string.Empty;

        protected override string DefaultTitle => ResolveRegisteredDefaultTitle();

        /// <summary>
        /// Gets whether the graph editor should render one input port for this node.
        /// </summary>
        public override bool HasInputPort => true;

        /// <summary>
        /// Gets whether editor auto-arrange should reposition this node.
        /// </summary>
        public override bool ParticipatesInAutoArrange => true;

        /// <summary>
        /// Gets whether topology validation should include this node.
        /// </summary>
        public override bool ParticipatesInTopologyValidation => true;

        /// <summary>
        /// Gets whether runtime visualization may override the authored palette.
        /// </summary>
        public override bool UsesRuntimeStateStyling => true;

        public new virtual IReadOnlyList<CutsceneNodePort> GetOutputPorts()
        {
            return CutsceneNodePort.NextOnly;
        }

        public override bool RequiresTick => false;

        public override string GetSummary()
        {
            return string.Empty;
        }

        public virtual void OnEnter(CutsceneExecutionContext context)
        {
            context.TryComplete(CutsceneNodeResult.Success());
        }

        public virtual void Tick(CutsceneExecutionContext context)
        {
        }

        public virtual void OnExit(CutsceneExecutionContext context)
        {
        }

        /// <summary>
        /// Restores authored identity and layout state during graph migration flows.
        /// </summary>
        /// <param name="id">Stable node identifier to restore.</param>
        /// <param name="title">Authored title override to restore.</param>
        /// <param name="position">Authored graph position to restore.</param>
        internal void RestoreAuthoringState(
            SerializableGuid id,
            string title,
            Vector2 position)
        {
            RestoreId(id);
            Title = title ?? string.Empty;
            Position = position;
        }

        private string ResolveRegisteredDefaultTitle()
        {
            Type nodeType = GetType();

            return GraphNodeMenuMetadataUtility.ResolveDefaultTitle<CutsceneNodeMenuAttribute>(
                nodeType,
                DefaultTitlesByType);
        }
    }
}