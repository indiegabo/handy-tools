using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.ConversationsModule.Nodes
{
    /// <summary>
    /// Defines the shared authored node shape used by Conversations action nodes.
    /// </summary>
    [Serializable]
    public abstract class ConversationActionNodeBase : ConversationNodeBase
    {
        private static readonly IReadOnlyList<GraphPortDefinition> _optionalNextOutputPorts =
            new[]
            {
                new GraphPortDefinition(
                    GraphPortKeys.Next,
                    GraphPortKeys.Next,
                    isMandatory: false),
            };

        /// <summary>
        /// Gets the declared output ports for the node.
        /// Action nodes can terminate the conversation when no next connection exists.
        /// </summary>
        /// <returns>The optional next output declaration.</returns>
        public override IReadOnlyList<GraphPortDefinition> GetOutputPorts()
        {
            return _optionalNextOutputPorts;
        }
    }
}