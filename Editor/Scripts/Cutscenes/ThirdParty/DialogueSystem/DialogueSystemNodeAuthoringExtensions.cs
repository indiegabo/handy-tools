using System;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Actions;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.DialogueSystem
{
    public static class DialogueSystemNodeAuthoringExtensions
    {
        public static bool ValidateConversationTitle(
            CutsceneDialogueConversationNode node,
            out string message)
        {
            if (node == null)
            {
                message = "The dialogue node reference is missing.";
                return false;
            }

            string summary = node.GetSummary();

            if (string.IsNullOrWhiteSpace(summary))
            {
                message = "Dialogue conversation nodes should define a conversation title before runtime.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        public static string GetAvailabilityMessage()
        {
            return "Dialogue System authoring extensions are active for Cutscenes in this project.";
        }
    }
}