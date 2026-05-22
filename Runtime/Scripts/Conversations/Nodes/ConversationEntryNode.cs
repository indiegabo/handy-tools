using IndieGabo.HandyTools.ConversationsModule.Core;

namespace IndieGabo.HandyTools.ConversationsModule.Nodes
{
    /// <summary>
    /// Declares the required entry node used by authored Conversations graphs.
    /// </summary>
    [System.Serializable]
    [ConversationNodeMenu("Flow/Entry", "Entry")]
    public sealed class ConversationEntryNode : ConversationNodeBase
    {
        /// <summary>
        /// Gets whether the node exposes one input port.
        /// </summary>
        public override bool HasInputPort => false;
    }
}