using IndieGabo.HandyTools.ConversationsModule;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.CutscenesModule.Core;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    /// <summary>
    /// Starts one authored Conversations module flow from one stable conversation reference and
    /// completes after the conversation finishes or is canceled.
    /// </summary>
    [System.Serializable]
    [CutsceneNodeMenu(
        "Conversations/Start Conversation",
        "Start Conversation",
        requiresConversationsModule: true)]
    public sealed class CutsceneConversationReferenceNode : CutsceneNodeBase
    {
        private const string RuntimeStateKey = "ConversationRuntime";

        [SerializeField]
        private ConversationReference _conversation = new();

        /// <summary>
        /// Gets whether the node requires per-frame polling while the conversation is active.
        /// </summary>
        public override bool RequiresTick => true;

        /// <summary>
        /// Stores the authored conversation reference used by the node.
        /// </summary>
        /// <param name="conversation">Conversation selection that should be copied.</param>
        public void Configure(ConversationReference conversation)
        {
            (_conversation ??= new ConversationReference()).CopyFrom(conversation);
        }

        /// <summary>
        /// Stores one authored conversation selection using its owning table and definition.
        /// </summary>
        /// <param name="table">Table that owns the selected conversation.</param>
        /// <param name="conversation">Conversation that should play when the node starts.</param>
        public void Configure(
            ConversationTable table,
            ConversationDefinition conversation)
        {
            (_conversation ??= new ConversationReference()).SetSelection(table, conversation);
        }

        /// <summary>
        /// Gets one short summary for graph authoring surfaces.
        /// </summary>
        /// <returns>The authored conversation title or a fallback label.</returns>
        public override string GetSummary()
        {
            string conversationTitle = _conversation?.ConversationTitle ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(conversationTitle))
            {
                return conversationTitle;
            }

            return _conversation?.Table != null
                ? _conversation.Table.name
                : "Conversation";
        }

        /// <summary>
        /// Starts the configured authored conversation.
        /// </summary>
        /// <param name="context">Runtime execution context that owns the node instance.</param>
        public override void OnEnter(CutsceneExecutionContext context)
        {
            _conversation ??= new ConversationReference();

            if (!ConversationsModuleDefinition.IsActive)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Start Conversation node requires the Conversations module to be active."));
                return;
            }

            if (_conversation.Table == null)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Start Conversation node requires one ConversationTable reference."));
                return;
            }

            RuntimeState runtimeState = context.GetOrCreateNodeState(
                RuntimeStateKey,
                () => RuntimeState.Create(context.Director));

            if (runtimeState?.Controller == null)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Start Conversation node could not create one runtime playback controller."));
                return;
            }

            runtimeState.Controller.ConfigureRuntimePlayback(_conversation);
            runtimeState.Controller.Play();

            if (runtimeState.Controller.Session == null)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    BuildFailureMessage(runtimeState.Controller.FailureReason)));
                return;
            }

            TryCompleteFromConversationState(context, runtimeState.Controller);
        }

        /// <summary>
        /// Polls the active authored conversation until it reaches a terminal state.
        /// </summary>
        /// <param name="context">Runtime execution context that owns the node instance.</param>
        public override void Tick(CutsceneExecutionContext context)
        {
            if (!context.TryGetNodeState(RuntimeStateKey, out RuntimeState runtimeState)
                || runtimeState?.Controller == null)
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Start Conversation node lost its runtime playback state."));
                return;
            }

            runtimeState.Controller.Tick();
            TryCompleteFromConversationState(context, runtimeState.Controller);
        }

        /// <summary>
        /// Cancels any active runtime conversation and releases the temporary trigger object.
        /// </summary>
        /// <param name="context">Runtime execution context that owns the node instance.</param>
        public override void OnExit(CutsceneExecutionContext context)
        {
            if (!context.TryGetNodeState(RuntimeStateKey, out RuntimeState runtimeState)
                || runtimeState == null)
            {
                return;
            }

            ConversationSession session = runtimeState.Controller?.Session;

            if (runtimeState.Controller != null
                && session != null
                && session.State == ConversationSessionState.Running)
            {
                runtimeState.Controller.CancelConversation();
            }

            runtimeState.Dispose();
            context.RemoveNodeState(RuntimeStateKey);
        }

        /// <summary>
        /// Completes the node when the bound conversation reaches one terminal state.
        /// </summary>
        /// <param name="context">Runtime execution context that owns the node instance.</param>
        /// <param name="trigger">Runtime trigger driving the authored conversation.</param>
        private static void TryCompleteFromConversationState(
            CutsceneExecutionContext context,
            ConversationAuthoredPlaybackController controller)
        {
            ConversationSession session = controller?.Session;

            if (session == null)
            {
                return;
            }

            switch (session.State)
            {
                case ConversationSessionState.Completed:
                case ConversationSessionState.Canceled:
                    context.TryComplete(CutsceneNodeResult.Success());
                    break;

                case ConversationSessionState.Faulted:
                    context.TryComplete(CutsceneNodeResult.Failure(
                        BuildFailureMessage(controller.FailureReason)));
                    break;
            }
        }

        /// <summary>
        /// Creates one deterministic failure message for runtime diagnostics.
        /// </summary>
        /// <param name="failureReason">Failure reason reported by the Conversations runtime.</param>
        /// <returns>The formatted failure message exposed by the cutscene node.</returns>
        private static string BuildFailureMessage(string failureReason)
        {
            return string.IsNullOrWhiteSpace(failureReason)
                ? "Start Conversation node failed without a diagnostic message."
                : failureReason;
        }

        /// <summary>
        /// Stores the runtime objects created temporarily for one node execution.
        /// </summary>
        private sealed class RuntimeState
        {
            /// <summary>
            /// Initializes one runtime state wrapper.
            /// </summary>
            /// <param name="controller">Runtime playback controller used to play the conversation.</param>
            private RuntimeState(
                ConversationAuthoredPlaybackController controller)
            {
                Controller = controller;
            }

            /// <summary>
            /// Gets the runtime playback controller used by the node execution.
            /// </summary>
            public ConversationAuthoredPlaybackController Controller { get; }

            /// <summary>
            /// Creates one runtime state instance bound to the owning cutscene director.
            /// </summary>
            /// <param name="owner">Director that should own the runtime conversation lifetime.</param>
            /// <returns>The created runtime state.</returns>
            public static RuntimeState Create(CutsceneDirector owner)
            {
                return new RuntimeState(new ConversationAuthoredPlaybackController(owner));
            }

            /// <summary>
            /// Releases the runtime playback controller.
            /// </summary>
            public void Dispose()
            {
                Controller?.Dispose();
            }
        }
    }
}