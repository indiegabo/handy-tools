using System;
using IndieGabo.HandyTools.GraphCore;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CutsceneNodeMenuAttribute : Attribute, IGraphNodeMenuMetadata
    {
        public CutsceneNodeMenuAttribute(
            string menuPath,
            bool requiresDialogueSystem = false,
            bool requiresConversationsModule = false)
        {
            MenuPath = menuPath;
            DefaultTitle = string.Empty;
            RequiresDialogueSystem = requiresDialogueSystem;
            RequiresConversationsModule = requiresConversationsModule;
        }

        public CutsceneNodeMenuAttribute(
            string menuPath,
            string defaultTitle,
            bool requiresDialogueSystem = false,
            bool requiresConversationsModule = false)
        {
            MenuPath = menuPath;
            DefaultTitle = defaultTitle ?? string.Empty;
            RequiresDialogueSystem = requiresDialogueSystem;
            RequiresConversationsModule = requiresConversationsModule;
        }

        public string MenuPath { get; }

        public string DefaultTitle { get; }

        public bool RequiresDialogueSystem { get; }

        public bool RequiresConversationsModule { get; }
    }
}