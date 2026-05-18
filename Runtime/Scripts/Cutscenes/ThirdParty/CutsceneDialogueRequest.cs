using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem
{
    public readonly struct CutsceneDialogueRequest
    {
        public CutsceneDialogueRequest(
            string conversationTitle,
            string databaseKey,
            Transform speaker,
            Transform listener)
        {
            ConversationTitle = conversationTitle;
            DatabaseKey = databaseKey;
            Speaker = speaker;
            Listener = listener;
        }

        public string ConversationTitle { get; }

        public string DatabaseKey { get; }

        public Transform Speaker { get; }

        public Transform Listener { get; }
    }
}