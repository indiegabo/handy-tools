namespace IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem
{
    public readonly struct CutsceneDialogueResult
    {
        public CutsceneDialogueResult(bool hasCompleted, bool succeeded, string failureReason)
        {
            HasCompleted = hasCompleted;
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public bool HasCompleted { get; }

        public bool Succeeded { get; }

        public string FailureReason { get; }
    }
}