using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    [System.Serializable]
    [CutsceneNodeMenu(
        "Dialogue/Start Conversation",
        "Start Conversation",
        requiresDialogueSystem: true)]
    public sealed class CutsceneDialogueConversationNode : CutsceneNodeBase
    {
        private const string HandleStateKey = "DialogueHandle";

        [SerializeField]
        [CutsceneValueSourceType(typeof(string))]
        private CutsceneValueSource _conversationTitleSource =
            CutsceneValueSource.CreateDirect(string.Empty);

        [SerializeField]
        [CutsceneValueSourceType(typeof(string))]
        private CutsceneValueSource _databaseKeySource =
            CutsceneValueSource.CreateDirect(string.Empty);

        [SerializeField]
        [CutsceneValueSourceType(typeof(Transform))]
        private CutsceneValueSource _speakerSource =
            CutsceneValueSource.CreateDirect<Transform>(null);

        [SerializeField]
        [CutsceneValueSourceType(typeof(Transform))]
        private CutsceneValueSource _listenerSource =
            CutsceneValueSource.CreateDirect<Transform>(null);

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _waitForConversationEndSource =
            CutsceneValueSource.CreateDirect(true);

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _continueOnFailureSource =
            CutsceneValueSource.CreateDirect(false);

        public void Configure(
            string conversationTitle,
            string databaseKey,
            Transform speaker,
            Transform listener,
            bool waitForConversationEnd,
            bool continueOnFailure)
        {
            EnsureValueSourcesConfigured();
            _conversationTitleSource.SetDirectValue(conversationTitle ?? string.Empty);
            _databaseKeySource.SetDirectValue(databaseKey ?? string.Empty);
            _speakerSource.SetDirectValue(speaker);
            _listenerSource.SetDirectValue(listener);
            _waitForConversationEndSource.SetDirectValue(waitForConversationEnd);
            _continueOnFailureSource.SetDirectValue(continueOnFailure);
        }

        public override bool RequiresTick
        {
            get
            {
                EnsureValueSourcesConfigured();

                if (_waitForConversationEndSource.Mode == CutsceneValueSourceMode.Blackboard)
                {
                    return true;
                }

                return _waitForConversationEndSource.DirectValue?.GetBoxedValue() as bool?
                    ?? true;
            }
        }

        public override string GetSummary()
        {
            EnsureValueSourcesConfigured();
            return _conversationTitleSource.GetSummary();
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            EnsureValueSourcesConfigured();

            if (!_conversationTitleSource.TryGetValue(context, out string conversationTitle)
                || !_databaseKeySource.TryGetValue(context, out string databaseKey)
                || !_speakerSource.TryGetValue(context, out Transform speaker)
                || !_listenerSource.TryGetValue(context, out Transform listener)
                || !_waitForConversationEndSource.TryGetValue(context, out bool waitForConversationEnd)
                || !_continueOnFailureSource.TryGetValue(context, out bool continueOnFailure))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Start Conversation node requires valid configured value sources."));
                return;
            }

            if (!context.Service.TryGetDialogueBridge(out ICutsceneDialogueBridge bridge)
                || bridge == null
                || !bridge.IsAvailable)
            {
                CompleteBridgeUnavailable(context, continueOnFailure);
                return;
            }

            CutsceneDialogueHandle handle = bridge.StartConversation(
                new CutsceneDialogueRequest(
                    conversationTitle,
                    databaseKey,
                    speaker,
                    listener));

            context.SetNodeState(HandleStateKey, handle);

            if (!waitForConversationEnd)
            {
                context.TryComplete(CutsceneNodeResult.Success());
            }
        }

        public override void Tick(CutsceneExecutionContext context)
        {
            EnsureValueSourcesConfigured();

            if (!context.TryGetNodeState(HandleStateKey, out CutsceneDialogueHandle handle)
                || !handle.IsValid
                || !context.Service.TryGetDialogueBridge(out ICutsceneDialogueBridge bridge)
                || bridge == null)
            {
                return;
            }

            if (!bridge.TryGetResult(handle, out CutsceneDialogueResult result) || !result.HasCompleted)
            {
                return;
            }

            if (!_continueOnFailureSource.TryGetValue(context, out bool continueOnFailure))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Start Conversation node requires one valid continue-on-failure source."));
                return;
            }

            if (result.Succeeded || continueOnFailure)
            {
                context.TryComplete(CutsceneNodeResult.Success());
                return;
            }

            context.TryComplete(CutsceneNodeResult.Failure(result.FailureReason));
        }

        public override void OnExit(CutsceneExecutionContext context)
        {
            if (context.TryGetNodeState(HandleStateKey, out CutsceneDialogueHandle handle)
                && handle.IsValid
                && context.Service.TryGetDialogueBridge(out ICutsceneDialogueBridge bridge)
                && bridge != null)
            {
                bridge.CancelConversation(handle);
            }
        }

        private void CompleteBridgeUnavailable(
            CutsceneExecutionContext context,
            bool continueOnFailure)
        {
            if (continueOnFailure)
            {
                context.TryComplete(CutsceneNodeResult.Success());
                return;
            }

            context.TryComplete(CutsceneNodeResult.Failure(
                "Dialogue System bridge is unavailable for this cutscene conversation node."));
        }

        private void EnsureValueSourcesConfigured()
        {
            _conversationTitleSource ??= CutsceneValueSource.CreateDirect(string.Empty);
            _databaseKeySource ??= CutsceneValueSource.CreateDirect(string.Empty);
            _speakerSource ??= CutsceneValueSource.CreateDirect<Transform>(null);
            _listenerSource ??= CutsceneValueSource.CreateDirect<Transform>(null);
            _waitForConversationEndSource ??= CutsceneValueSource.CreateDirect(true);
            _continueOnFailureSource ??= CutsceneValueSource.CreateDirect(false);

            _conversationTitleSource.SetExpectedValueType(typeof(string));
            _databaseKeySource.SetExpectedValueType(typeof(string));
            _speakerSource.SetExpectedValueType(typeof(Transform));
            _listenerSource.SetExpectedValueType(typeof(Transform));
            _waitForConversationEndSource.SetExpectedValueType(typeof(bool));
            _continueOnFailureSource.SetExpectedValueType(typeof(bool));
        }
    }
}