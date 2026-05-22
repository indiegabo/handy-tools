using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.GraphCore;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Plays one authored conversation reference without requiring a scene-authored trigger
    /// component and exposes the runtime contract consumed by conversation presenters.
    /// </summary>
    public sealed class ConversationAuthoredPlaybackController :
        IConversationPlaybackController,
        IDisposable
    {
        #region Constants

        private const string IdleStatus = "Conversation playback is idle.";

        #endregion

        #region Fields

        private readonly MonoBehaviour _owner;

        private readonly GraphBlackboard _runtimeBlackboard;

        private readonly List<InputAction> _ownedEnabledActions = new();

        private ConversationReference _conversation = new();

        private bool _reactToInputActions = true;

        private ConversationSession _session;

        private GameObject _resolvedPresenterPrefab;

        private GameObject _presenterInstance;

        private ConversationPresenterRoot _presenterRoot;

        private string _statusMessage = IdleStatus;

        private string _failureReason = string.Empty;

        #endregion

        #region Events

        /// <summary>
        /// Raised after the playback state, active session, or active line changes.
        /// </summary>
        public event Action PlaybackStateChanged;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes one authored playback controller bound to the provided runtime owner.
        /// </summary>
        /// <param name="owner">MonoBehaviour that owns the runtime conversation lifetime.</param>
        /// <param name="runtimeBlackboard">
        /// Optional runtime blackboard used by the active conversation session.
        /// </param>
        public ConversationAuthoredPlaybackController(
            MonoBehaviour owner,
            GraphBlackboard runtimeBlackboard = null)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _runtimeBlackboard = runtimeBlackboard ?? new GraphBlackboard();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the authored table used to resolve the current conversation selection.
        /// </summary>
        public ConversationTable Table => _conversation?.Table;

        /// <summary>
        /// Gets the configured authored conversation reference.
        /// </summary>
        public ConversationReference Conversation => _conversation ??= new ConversationReference();

        /// <summary>
        /// Gets the runtime blackboard used by the active session.
        /// </summary>
        public GraphBlackboard RuntimeBlackboard => _runtimeBlackboard;

        /// <summary>
        /// Gets the active runtime session when one conversation is currently bound.
        /// </summary>
        public ConversationSession Session => _session;

        /// <summary>
        /// Gets whether the controller is loading runtime data asynchronously.
        /// </summary>
        public bool IsLoading => false;

        /// <summary>
        /// Gets the latest runtime status message.
        /// </summary>
        public string StatusMessage => _statusMessage ?? string.Empty;

        /// <summary>
        /// Gets the latest failure reason recorded by the controller.
        /// </summary>
        public string FailureReason => _failureReason ?? string.Empty;

        #endregion

        #region Public API

        /// <summary>
        /// Replaces the authored conversation selection used by subsequent playback requests.
        /// </summary>
        /// <param name="conversation">Conversation reference that should be copied.</param>
        /// <param name="reactToInputActions">
        /// Whether authored input actions should be observed while the session is active.
        /// </param>
        public void ConfigureRuntimePlayback(
            ConversationReference conversation,
            bool reactToInputActions = true)
        {
            _conversation ??= new ConversationReference();
            _conversation.CopyFrom(conversation);
            _reactToInputActions = reactToInputActions;
            Table?.EnsureAuthoringIds();
        }

        /// <summary>
        /// Updates authored input-action routing while the current session is active.
        /// </summary>
        public void Tick()
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
        /// Starts playback for the configured authored conversation selection.
        /// </summary>
        public void Play()
        {
            ConversationPresenterRuntimeCache.ReserveConversation(_owner);
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

        /// <summary>
        /// Releases the current presenter, session, and temporary input-action ownership.
        /// </summary>
        public void Dispose()
        {
            UnsubscribeFromSession();
            DisableOwnedActions();
            ReleasePresenterInstance();
            ReleaseSession();
            ConversationPresenterRuntimeCache.ReleaseConversation(_owner);
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
                _owner,
                this);
            _presenterInstance = _presenterRoot?.gameObject;
        }

        /// <summary>
        /// Releases the currently active presenter back into the runtime cache.
        /// </summary>
        private void ReleasePresenterInstance()
        {
            if (_presenterRoot != null)
            {
                ConversationPresenterRuntimeCache.ReleasePresenter(_owner, _presenterRoot);
            }

            _presenterRoot = null;
            _presenterInstance = null;
            _resolvedPresenterPrefab = null;
        }

        /// <summary>
        /// Binds the controller to one runtime session and refreshes presenter notifications.
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
        /// Updates the public controller status from the active session lifecycle.
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
                    ConversationPresenterRuntimeCache.ReleaseConversation(_owner);
                    break;

                case ConversationSessionState.Canceled:
                    _statusMessage = "Conversation canceled.";
                    ReleasePresenterInstance();
                    ConversationPresenterRuntimeCache.ReleaseConversation(_owner);
                    break;

                case ConversationSessionState.Faulted:
                    _statusMessage = "Conversation faulted.";
                    ReleasePresenterInstance();
                    ConversationPresenterRuntimeCache.ReleaseConversation(_owner);
                    break;

                default:
                    _statusMessage = IdleStatus;
                    break;
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// Applies one failure state to the controller.
        /// </summary>
        /// <param name="reason">Failure reason that should be exposed publicly.</param>
        private void ApplyFailure(string reason)
        {
            ReleasePresenterInstance();
            ReleaseSession();
            ConversationPresenterRuntimeCache.ReleaseConversation(_owner);
            _statusMessage = "Conversation playback failed.";
            _failureReason = string.IsNullOrWhiteSpace(reason)
                ? "Conversation playback failed without a diagnostic message."
                : reason;
            NotifyStateChanged();
        }

        /// <summary>
        /// Ensures the configured table actions are enabled while the session is active.
        /// </summary>
        private void EnsureTrackedActions()
        {
            TrackOwnedAction(Table?.ContinueAction);
            TrackOwnedAction(Table?.CancelAction);
            TrackOwnedAction(Table?.SkipAction);
        }

        /// <summary>
        /// Enables and tracks one action owned temporarily by the controller.
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
        /// Disables every input action temporarily enabled by the controller.
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
        /// Raises presenter-facing state notifications.
        /// </summary>
        private void NotifyStateChanged()
        {
            PlaybackStateChanged?.Invoke();
        }

        #endregion
    }
}