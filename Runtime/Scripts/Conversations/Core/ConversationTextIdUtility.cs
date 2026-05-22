using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.ConversationsModule.Core
{
    /// <summary>
    /// Builds stable identifiers for authored conversation text payloads.
    /// </summary>
    public static class ConversationTextIdUtility
    {
        /// <summary>
        /// Builds one stable text identifier from the owning conversation and node ids.
        /// </summary>
        /// <param name="conversationId">Conversation that owns the text payload.</param>
        /// <param name="nodeId">Node that owns the text payload.</param>
        /// <returns>Stable text identifier that can later back localization lookup.</returns>
        public static string Build(SerializableGuid conversationId, SerializableGuid nodeId)
        {
            string conversationToken = conversationId == SerializableGuid.Empty
                ? "missing-conversation"
                : conversationId.ToHexString().ToLowerInvariant();
            string nodeToken = nodeId == SerializableGuid.Empty
                ? "missing-node"
                : nodeId.ToHexString().ToLowerInvariant();

            return $"conversation.{conversationToken}.text.{nodeToken}";
        }
    }
}