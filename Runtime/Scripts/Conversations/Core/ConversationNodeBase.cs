using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.ConversationsModule.Core
{
    /// <summary>
    /// Defines the base authored node shape used by Conversations graphs.
    /// </summary>
    [Serializable]
    public abstract class ConversationNodeBase : GraphNodeBase
    {
        private static readonly Dictionary<Type, string> _defaultTitlesByType = new();

        /// <summary>
        /// Gets the fallback title used when no custom node title is authored.
        /// </summary>
        protected override string DefaultTitle => ResolveRegisteredDefaultTitle();

        /// <summary>
        /// Gets the declared output ports for the node.
        /// </summary>
        /// <returns>The declared output ports.</returns>
        public override IReadOnlyList<GraphPortDefinition> GetOutputPorts()
        {
            return GraphPortDefinition.NextOnly;
        }

        /// <summary>
        /// Gets one short summary shown by authoring surfaces.
        /// </summary>
        /// <returns>The authored summary string.</returns>
        public override string GetSummary()
        {
            return string.Empty;
        }

        /// <summary>
        /// Resolves the default title from the registered Conversations menu metadata.
        /// </summary>
        /// <returns>The fallback title for the node type.</returns>
        private string ResolveRegisteredDefaultTitle()
        {
            return GraphNodeMenuMetadataUtility.ResolveDefaultTitle<ConversationNodeMenuAttribute>(
                GetType(),
                _defaultTitlesByType);
        }
    }
}