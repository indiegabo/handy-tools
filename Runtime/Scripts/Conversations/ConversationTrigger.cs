using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.GraphCore;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Plays one authored conversation directly from a ConversationTable and spawns the
    /// effective presenter prefab resolved from the table defaults and conversation overrides.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConversationTrigger : MonoBehaviour, IConversationPlaybackController
    {
        #region Constants

        private const string IdleStatus = "Conversation trigger is idle.";

        #endregion

        #region Fields

        [SerializeField]
        private ConversationReference _conversation = new();

        [SerializeField, HideInInspector, FormerlySerializedAs("_table")]
        private ConversationTable _legacyTable;

        [SerializeField, HideInInspector, FormerlySerializedAs("_conversationTitle")]
        private string _conversationTitle = string.Empty;

        [SerializeField]
        private bool _playOnStart = true;

        [SerializeField]
        private bool _reactToInputActions = true;

        [SerializeField]
        private GraphBlackboard _runtimeBlackboard = new();

        private readonly List<InputAction> _ownedEnabledActions = new();

        private ConversationSession _session;

        private GameObject _resolvedPresenterPrefab;

        private GameObject _presenterInstance;

        private ConversationPresenterRoot _presenterRoot;

        private string _statusMessage = IdleStatus;

        private string _failureReason = string.Empty;

        #endregion

        #region Events

        /// <summary>
        /// Raised after the trigger state or session changes.
        /// </summary>
        public event Action<ConversationTrigger> StateChanged;

        /// <summary>
        /// Raised after the playback state, active session, or active line changes.
        /// </summary>
        public event Action PlaybackStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the authored table used to resolve the conversation selection.
        /// </summary>
        public ConversationTable Table => _conversation?.Table != null
            ? _conversation.Table
            : _legacyTable;

        /// <summary>
        /// Gets the authored conversation title stored by the current selection.
        /// </summary>
        public string ConversationTitle => !string.IsNullOrWhiteSpace(_conversation?.ConversationTitle)
            ? _conversation.ConversationTitle
            : _conversationTitle ?? string.Empty;

        /// <summary>
        /// Gets the serialized conversation selection used by the trigger.
        /// </summary>
        public ConversationReference Conversation => _conversation ??= new ConversationReference();

        /// <summary>
        /// Gets the runtime blackboard used by the active session.
        /// </summary>
        public GraphBlackboard RuntimeBlackboard => _runtimeBlackboard ??= new GraphBlackboard();

        /// <summary>
        /// Gets the active runtime session when one conversation is currently bound.
        /// </summary>
        public ConversationSession Session => _session;

        /// <summary>
        /// Gets whether the trigger is loading runtime data asynchronously.
        /// </summary>
        public bool IsLoading => false;

        /// <summary>
        /// Gets the latest runtime status message.
        /// </summary>
        public string StatusMessage => _statusMessage ?? string.Empty;

        /// <summary>
        /// Gets the latest failure reason recorded by the trigger.
        /// </summary>
        public string FailureReason => _failureReason ?? string.Empty;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Ensures the authored table and local blackboard are ready before playback begins.
        /// </summary>
        private void Awake()
        {
            _runtimeBlackboard ??= new GraphBlackboard();
            MigrateLegacyConversationSelectionIfNeeded();
            Table?.EnsureAuthoringIds();
        }

        /// <summary>
        /// Keeps the serialized conversation selection synchronized while editing components.
        /// </summary>
        private void OnValidate()
        {
            _runtimeBlackboard ??= new GraphBlackboard();
            MigrateLegacyConversationSelectionIfNeeded();
            Table?.EnsureAuthoringIds();
        }

        /// <summary>
        /// Restores input-action listeners and session notifications after the trigger is enabled.
        /// </summary>
        private void OnEnable()
        {
            EnsureTrackedActions();
            SubscribeToSession();
            NotifyStateChanged();
        }

        /// <summary>
        /// Starts playback automatically when configured.
        /// </summary>
        private void Start()
        {
            if (_playOnStart)
            {
                Play();
            }
        }

        /// <summary>
        /// Routes configured table-level input actions into the active authored session.
        /// </summary>
        private void Update()
        {
            EnsureTrackedActions();

            _session?.Tick(Time.deltaTime, Time.unscaledDeltaTime);

            if (!_reactToInputActions || _session == null || !_session.HasActiveLine)
            {
                return;
            }

            if (WasActionTriggered(Table?.CancelAction))
            {
                CancelConversation();
                return;
            }

            if (WasActionTriggered(Table?.SkipAction))
            {
                SkipConversation();
                return;
            }

            if (WasActionTriggered(Table?.ContinueAction))
            {
                AdvanceConversation();
            }
        }

        /// <summary>
        /// Releases temporary listeners while the trigger is inactive.
        /// </summary>
        private void OnDisable()
        {
            UnsubscribeFromSession();
            DisableOwnedActions();
        }

        /// <summary>
        /// Releases the active presenter instance and runtime listeners permanently.
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeFromSession();
            DisableOwnedActions();
            ReleasePresenterInstance();
            ConversationPresenterRuntimeCache.ReleaseConversation(this);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts playback for the configured authored conversation.
        /// </summary>
        /// <param name="conversation">
        /// Conversation selection that should be used by subsequent playback requests.
        /// </param>
        /// <param name="reactToInputActions">
        /// Whether the trigger should monitor authored input actions while the session is active.
        /// </param>
        public void ConfigureRuntimePlayback(
            ConversationReference conversation,
            bool reactToInputActions = true)
        {
            _conversation ??= new ConversationReference();
            _conversation.CopyFrom(conversation);
            _legacyTable = null;
            _conversationTitle = string.Empty;
            _playOnStart = false;
            _reactToInputActions = reactToInputActions;
            _runtimeBlackboard ??= new GraphBlackboard();
            Table?.EnsureAuthoringIds();
        }

        /// <summary>
        /// Starts playback for the configured authored conversation.
        /// </summary>
        public void Play()
        {
            MigrateLegacyConversationSelectionIfNeeded();
            ConversationPresenterRuntimeCache.ReserveConversation(this);
            ReleaseSession();
            RuntimeBlackboard.Clear();
            _failureReason = string.Empty;

            if (!ConversationAuthoredRuntimeBuilder.TryResolveConversation(
                Conversation,
                    out ConversationDefinition conversation,
                    out string failureReason))
            {
                ApplyFailure(failureReason);
                return;
            }

            EnsurePresenterInstance(conversation);

            try
            {
                ConversationData conversationData =
                    ConversationAuthoredRuntimeBuilder.BuildConversationData(
                        Table,
                        conversation);
                BindSession(new ConversationSession(conversationData, RuntimeBlackboard));

                if (!_session.Start())
                {
                    ApplyFailure(
                        string.IsNullOrWhiteSpace(_session.FailureReason)
                            ? $"Conversation '{conversationData.Title}' did not start successfully."
                            : _session.FailureReason);
                    return;
                }

                UpdateStatusFromSession();
            }
            catch (Exception exception)
            {
                ApplyFailure(exception.Message);
            }
        }

        /// <summary>
        /// Advances the active session to its next authored line.
        /// </summary>
        /// <returns>True when the advance request was accepted.</returns>
        public bool AdvanceConversation()
        {
            bool advanced = _session != null && _session.Advance();

            if (advanced)
            {
                UpdateStatusFromSession();
            }

            return advanced;
        }

        /// <summary>
        /// Ends the active conversation through the authored skip action.
        /// </summary>
        /// <returns>True when the skip request was accepted.</returns>
        public bool SkipConversation()
        {
            bool skipped = _session != null && _session.Cancel();

            if (skipped)
            {
                _statusMessage = "Conversation skipped.";
                NotifyStateChanged();
            }

            return skipped;
        }

        /// <summary>
        /// Cancels the active authored session.
        /// </summary>
        /// <returns>True when the cancel request was accepted.</returns>
        public bool CancelConversation()
        {
            bool canceled = _session != null && _session.Cancel();

            if (canceled)
            {
                UpdateStatusFromSession();
            }

            return canceled;
        }

        /// <summary>
        /// Resolves one authored portrait for the provided runtime conversant.
        /// </summary>
        /// <param name="actor">Runtime conversant that should be presented.</param>
        /// <returns>The resolved authored portrait when available.</returns>
        public Sprite ResolveActorPortrait(ConversationActorData actor)
        {
            if (actor == null
                || Table == null
                || !Table.TryGetActor(actor.ActorId, out ConversationActorDefinition actorDefinition))
            {
                return null;
            }

            return actorDefinition.Portrait;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Instantiates or rebinds the effective presenter prefab for the provided conversation.
        /// </summary>
        /// <param name="conversation">Conversation whose presentation should be resolved.</param>
        private void EnsurePresenterInstance(ConversationDefinition conversation)
        {
            GameObject presenterPrefab = Table?.ResolvePresenterPrefab(conversation);

            if (_presenterInstance != null && _resolvedPresenterPrefab == presenterPrefab)
            {
                _presenterRoot?.Bind(this);
                return;
            }

            ReleasePresenterInstance();
            _resolvedPresenterPrefab = presenterPrefab;

            if (presenterPrefab == null)
            {
                return;
            }

            _presenterRoot = ConversationPresenterRuntimeCache.AcquirePresenter(
                presenterPrefab,
                this);
            _presenterInstance = _presenterRoot?.gameObject;

            if (_presenterRoot == null)
            {
                return;
            }
        }

        /// <summary>
        /// Releases the currently active presenter back into the runtime cache.
        /// </summary>
        private void ReleasePresenterInstance()
        {
            if (_presenterRoot != null)
            {
                ConversationPresenterRuntimeCache.ReleasePresenter(this, _presenterRoot);
            }

            _presenterRoot = null;
            _presenterInstance = null;
            _resolvedPresenterPrefab = null;
        }

        /// <summary>
        /// Binds the trigger to one runtime session and refreshes controller notifications.
        /// </summary>
        /// <param name="session">Session that should become active.</param>
        private void BindSession(ConversationSession session)
        {
            if (ReferenceEquals(_session, session))
            {
                SubscribeToSession();
                NotifyStateChanged();
                return;
            }

            UnsubscribeFromSession();
            _session = session;
            SubscribeToSession();
            NotifyStateChanged();
        }

        /// <summary>
        /// Releases the current session subscription and cached session reference.
        /// </summary>
        private void ReleaseSession()
        {
            UnsubscribeFromSession();
            _session = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Subscribes to the current session mutations.
        /// </summary>
        private void SubscribeToSession()
        {
            if (_session == null)
            {
                return;
            }

            _session.Changed -= HandleSessionChanged;
            _session.Changed += HandleSessionChanged;
        }

        /// <summary>
        /// Releases the current session mutation subscription.
        /// </summary>
        private void UnsubscribeFromSession()
        {
            if (_session != null)
            {
                _session.Changed -= HandleSessionChanged;
            }
        }

        /// <summary>
        /// Reacts to one active session mutation.
        /// </summary>
        /// <param name="session">Session that changed.</param>
        private void HandleSessionChanged(ConversationSession session)
        {
            _ = session;
            UpdateStatusFromSession();
        }

        /// <summary>
        /// Updates the public trigger status from the active session lifecycle.
        /// </summary>
        private void UpdateStatusFromSession()
        {
            if (_session == null)
            {
                _statusMessage = IdleStatus;
                NotifyStateChanged();
                return;
            }

            _failureReason = _session.FailureReason;

            switch (_session.State)
            {
                case ConversationSessionState.Running:
                    _statusMessage = _session.HasActiveLine
                        ? "Conversation is presenting the current line."
                        : "Conversation is executing the current node.";
                    break;

                case ConversationSessionState.Completed:
                    _statusMessage = "Conversation completed.";
                    ReleasePresenterInstance();
                    ConversationPresenterRuntimeCache.ReleaseConversation(this);
                    break;

                case ConversationSessionState.Canceled:
                    _statusMessage = "Conversation canceled.";
                    ReleasePresenterInstance();
                    ConversationPresenterRuntimeCache.ReleaseConversation(this);
                    break;

                case ConversationSessionState.Faulted:
                    _statusMessage = "Conversation faulted.";
                    ReleasePresenterInstance();
                    ConversationPresenterRuntimeCache.ReleaseConversation(this);
                    break;

                default:
                    _statusMessage = IdleStatus;
                    break;
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// Applies one failure state to the trigger.
        /// </summary>
        /// <param name="reason">Failure reason that should be exposed publicly.</param>
        private void ApplyFailure(string reason)
        {
            ReleasePresenterInstance();
            ReleaseSession();
            ConversationPresenterRuntimeCache.ReleaseConversation(this);
            _statusMessage = "Conversation trigger failed.";
            _failureReason = string.IsNullOrWhiteSpace(reason)
                ? "Conversation trigger failed without a diagnostic message."
                : reason;
            NotifyStateChanged();
        }

        /// <summary>
        /// Ensures the configured table actions are enabled while the trigger is active.
        /// </summary>
        private void EnsureTrackedActions()
        {
            TrackOwnedAction(Table?.ContinueAction);
            TrackOwnedAction(Table?.CancelAction);
            TrackOwnedAction(Table?.SkipAction);
        }

        /// <summary>
        /// Migrates previously serialized table-plus-title fields into the new conversation
        /// reference payload without breaking older scenes and prefabs.
        /// </summary>
        private void MigrateLegacyConversationSelectionIfNeeded()
        {
            _conversation ??= new ConversationReference();

            if (_conversation.Table != null
                || (_legacyTable == null && string.IsNullOrWhiteSpace(_conversationTitle)))
            {
                return;
            }

            if (_legacyTable != null)
            {
                _legacyTable.EnsureAuthoringIds();

                if (!string.IsNullOrWhiteSpace(_conversationTitle)
                    && ConversationAuthoredRuntimeBuilder.TryResolveConversation(
                        _legacyTable,
                        _conversationTitle,
                        out ConversationDefinition legacyConversation,
                        out _))
                {
                    _conversation.SetSelection(_legacyTable, legacyConversation);
                }
                else
                {
                    _conversation.SetSelection(
                        _legacyTable,
                        IndieGabo.HandyTools.Utils.SerializableGuid.Empty,
                        _conversationTitle);
                }
            }
            else
            {
                _conversation.SetSelection(
                    null,
                    IndieGabo.HandyTools.Utils.SerializableGuid.Empty,
                    _conversationTitle);
            }

            _legacyTable = null;
            _conversationTitle = string.Empty;
        }

        /// <summary>
        /// Enables and tracks one action owned temporarily by the trigger.
        /// </summary>
        /// <param name="actionReference">Action reference that should be enabled.</param>
        private void TrackOwnedAction(InputActionReference actionReference)
        {
            InputAction action = actionReference?.action;

            if (action == null || action.enabled || _ownedEnabledActions.Contains(action))
            {
                return;
            }

            action.Enable();
            _ownedEnabledActions.Add(action);
        }

        /// <summary>
        /// Disables every input action temporarily enabled by the trigger.
        /// </summary>
        private void DisableOwnedActions()
        {
            for (int index = 0; index < _ownedEnabledActions.Count; index++)
            {
                InputAction action = _ownedEnabledActions[index];

                if (action != null && action.enabled)
                {
                    action.Disable();
                }
            }

            _ownedEnabledActions.Clear();
        }

        /// <summary>
        /// Checks whether one configured input action fired this frame.
        /// </summary>
        /// <param name="actionReference">Action reference that should be inspected.</param>
        /// <returns>True when the action fired this frame.</returns>
        private static bool WasActionTriggered(InputActionReference actionReference)
        {
            InputAction action = actionReference?.action;
            return action != null && action.enabled && action.WasPerformedThisFrame();
        }

        /// <summary>
        /// Raises the trigger-facing and presenter-facing state notifications.
        /// </summary>
        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(this);
            PlaybackStateChanged?.Invoke();
        }

        #endregion
    }
}