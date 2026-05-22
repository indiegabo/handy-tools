using IndieGabo.HandyTools.ConversationsModule.Blackboard;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.GraphCore;
using UnityEditor;

namespace IndieGabo.HandyTools.Editor.ConversationsModule
{
    /// <summary>
    /// Registers editor-time Conversations graph metadata required by authoring tools.
    /// </summary>
    public static class ConversationEditorRegistration
    {
        /// <summary>
        /// Ensures the Conversations graph family and editor-visible blackboard wrappers
        /// are registered whenever the editor domain loads.
        /// </summary>
        [InitializeOnLoadMethod]
        public static void RegisterEditorTypes()
        {
            ConversationGraphFamily.Register();
            GraphBlackboardValueRegistry.RegisterFamilyWrapper(
                ConversationGraphFamily.Id,
                typeof(ConversationActorIdBlackboardValue));
        }
    }
}