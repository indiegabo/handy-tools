using System;

namespace IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem
{
    [Serializable]
    public readonly struct CutsceneDialogueHandle
    {
        public CutsceneDialogueHandle(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Id);
    }
}