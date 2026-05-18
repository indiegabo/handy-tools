namespace IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem
{
    public interface ICutsceneDialogueBridge
    {
        bool IsAvailable { get; }

        CutsceneDialogueHandle StartConversation(CutsceneDialogueRequest request);

        bool TryGetResult(CutsceneDialogueHandle handle, out CutsceneDialogueResult result);

        void CancelConversation(CutsceneDialogueHandle handle);
    }
}