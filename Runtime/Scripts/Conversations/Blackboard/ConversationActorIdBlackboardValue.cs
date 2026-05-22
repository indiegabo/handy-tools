using System;
using IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.ConversationsModule.Blackboard
{
    /// <summary>
    /// Stores one Conversations-specific actor identifier payload.
    /// </summary>
    [Serializable]
    [GraphBlackboardValueDescriptor("Actor Id", typeof(ConversationActorId), Order = 0)]
    public sealed class ConversationActorIdBlackboardValue :
        GraphBlackboardTypedValue<ConversationActorId>
    {
    }
}