using System;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CutsceneNodeMenuAttribute : Attribute
    {
        public CutsceneNodeMenuAttribute(string menuPath, bool requiresDialogueSystem = false)
        {
            MenuPath = menuPath;
            DefaultTitle = string.Empty;
            RequiresDialogueSystem = requiresDialogueSystem;
        }

        public CutsceneNodeMenuAttribute(
            string menuPath,
            string defaultTitle,
            bool requiresDialogueSystem = false)
        {
            MenuPath = menuPath;
            DefaultTitle = defaultTitle ?? string.Empty;
            RequiresDialogueSystem = requiresDialogueSystem;
        }

        public string MenuPath { get; }

        public string DefaultTitle { get; }

        public bool RequiresDialogueSystem { get; }
    }
}