using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndieGabo.HandyTools.ConversationsModule.Cache;
using IndieGabo.HandyTools.ConversationsModule.Core;
using IndieGabo.HandyTools.ConversationsModule.Loading;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.GraphCore;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using IndieGabo.HandyTools.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.ConversationsModule
{
    /// <summary>
    /// Loads one exported conversation payload and drives one runtime session through
    /// table-level input bindings.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConversationRunner : MonoBehaviour, IConversationPlaybackController
    {
        #region Constants

        private const string IdleStatus = "Conversation runner is idle.";

        #endregion

        #region Fields

        [SerializeField]
        private ConversationTable _table;

        [SerializeField]
        private string _conversationTitle = string.Empty;

        [SerializeField]
        private bool _playOnStart = true;

        [SerializeField]
        private bool _reactToInputActions = true;

        [SerializeField]
        private GraphBlackboard _runtimeBlackboard = new();

        [SerializeField]
        private MonoBehaviour _scopeResolverBehaviour;

        private readonly List<InputAction> _ownedEnabledActions = new();

        private CancellationTokenSource _loadCancellationTokenSource;

        private IConversationLoader _loader;

        private IConversationCatalogProvider _catalogProvider;

        private IConversationCache _cache;

        private ConversationSession _session;

        private int _activeLoadVersion;

        private bool _isLoading;

        private string _statusMessage = IdleStatus;

        private string _failureReason = string.Empty;

        #endregion

        #region Events

        /// <summary>
        /// Raised after the runner status or active session changes.
        /// </summary>
        public event Action<ConversationRunner> StateChanged;

        /// <summary>
        /// Raised after the playback state, active session, or active line changes.
        /// </summary>
        public event Action PlaybackStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the authored table used to resolve conversation ids and input bindings.
        /// </summary>
        public ConversationTable Table => _table;

        /// <summary>
        /// Gets the configured authored conversation title used for selection.
        /// </summary>
        public string ConversationTitle => _conversationTitle ?? string.Empty;

        /// <summary>
        /// Gets the runtime blackboard used by the active session.
        /// </summary>
        public GraphBlackboard RuntimeBlackboard => _runtimeBlackboard ??= new GraphBlackboard();

        /// <summary>
        /// Gets the active runtime session when one conversation is loaded.
        /// </summary>
        public ConversationSession Session => _session;

        /// <summary>
        /// Gets whether one payload load is currently in flight.
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// Gets whether the active session currently exposes one playable line.
        /// </summary>
        public bool IsRunning => _session?.HasActiveLine ?? false;

        /// <summary>
        /// Gets the latest runner status message.
        /// </summary>
        public string StatusMessage => _statusMessage ?? string.Empty;

        /// <summary>
        /// Gets the latest failure reason recorded by the runner.
        /// </summary>
        public string FailureReason => _failureReason ?? string.Empty;

        /// <summary>
        /// Gets the resolved conversation title owned by the active session.
        /// </summary>
        public string ResolvedConversationTitle => _session?.Conversation?.Title ?? string.Empty;

        private IGraphBlackboardScopeResolver ScopeResolver =>
            _scopeResolverBehaviour as IGraphBlackboardScopeResolver;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Caches runtime services and the blackboard before playback begins.
        /// </summary>
        private void Awake()
        {
            _runtimeBlackboard ??= new GraphBlackboard();
            EnsureRuntimeServices();
        }

        /// <summary>
        /// Re-enables any runner-owned input actions and refreshes bound listeners.
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
        /// Routes configured table-level input actions into the active session.
        /// </summary>
        private void Update()
        {
            EnsureTrackedActions();

            _session?.Tick(UnityEngine.Time.deltaTime, UnityEngine.Time.unscaledDeltaTime);

            if (!_reactToInputActions || _isLoading || _session == null || !_session.HasActiveLine)
            {
                return;
            }

            if (WasActionTriggered(_table?.CancelAction))
            {
                CancelConversation();
                return;
            }

            if (WasActionTriggered(_table?.SkipAction))
            {
                SkipConversation();
                return;
            }

            if (WasActionTriggered(_table?.ContinueAction))
            {
                AdvanceConversation();
            }
        }

        /// <summary>
        /// Stops in-flight loads and releases runner-owned input actions.
        /// </summary>
        private void OnDisable()
        {
            CancelPendingLoad("Conversation load canceled.");
            UnsubscribeFromSession();
            DisableOwnedActions();
        }

        /// <summary>
        /// Stops in-flight loads and releases runner-owned input actions permanently.
        /// </summary>
        private void OnDestroy()
        {
            CancelPendingLoad(null);
            UnsubscribeFromSession();
            DisableOwnedActions();
            ConversationPresenterRuntimeCache.ReleaseConversation(this);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts loading and running the configured conversation.
        /// </summary>
        public void Play()
        {
            ConversationPresenterRuntimeCache.ReserveConversation(this);
            _ = PlayAsyncInternal(skipReservation: true);
        }

        /// <summary>
        /// Starts loading and running the configured conversation.
        /// </summary>
        /// <returns>True when the payload loaded successfully and the session started.</returns>
        public async Task<bool> PlayAsync()
        {
            return await PlayAsyncInternal(skipReservation: false);
        }

        /// <summary>
        /// Starts loading and running the configured conversation.
        /// </summary>
        /// <param name="skipReservation">Whether the caller already reserved the active-conversation slot.</param>
        /// <returns>True when the payload loaded successfully and the session started.</returns>
        private async Task<bool> PlayAsyncInternal(bool skipReservation)
        {
            if (!skipReservation)
            {
                ConversationPresenterRuntimeCache.ReserveConversation(this);
            }

            CancelPendingLoad(null);
            UnbindSession();

            if (!EnsureRuntimeServices())
            {
                ApplyFailure("Conversation runtime services could not be resolved.");
                return false;
            }

            if (!TryResolveConversationSelection(
                    out SerializableGuid conversationId,
                    out string resolvedTitle,
                    out string failureReason))
            {
                ApplyFailure(failureReason);
                return false;
            }

            _activeLoadVersion++;
            int loadVersion = _activeLoadVersion;
            _isLoading = true;
            _failureReason = string.Empty;
            _statusMessage = $"Loading '{resolvedTitle}'...";
            NotifyStateChanged();

            _loadCancellationTokenSource = new CancellationTokenSource();

            try
            {
                ConversationLoadResult loadResult = await _loader.LoadAsync(
                    conversationId,
                    _loadCancellationTokenSource.Token);

                if (!IsCurrentLoad(loadVersion))
                {
                    return false;
                }

                _isLoading = false;

                if (!loadResult.Succeeded || loadResult.ConversationData == null)
                {
                    ApplyFailure(
                        string.IsNullOrWhiteSpace(loadResult.FailureReason)
                            ? $"Conversation '{resolvedTitle}' failed to load."
                            : loadResult.FailureReason);
                    return false;
                }

                BindSession(
                    new ConversationSession(
                        loadResult.ConversationData,
                        RuntimeBlackboard,
                        ScopeResolver,
                        await ConversationLocalizationOverlayLoader.LoadTextMapAsync(
                            ConversationRuntimeSettings.Instance,
                            conversationId,
                            _loadCancellationTokenSource.Token)));

                if (!_session.Start())
                {
                    ApplyFailure(
                        string.IsNullOrWhiteSpace(_session.FailureReason)
                            ? $"Conversation '{resolvedTitle}' did not start successfully."
                            : _session.FailureReason);
                    return false;
                }

                UpdateStatusFromSession(
                    loadResult.LoadedFromCache
                        ? "Conversation loaded from cache."
                        : "Conversation loaded successfully.");
                return _session.State == ConversationSessionState.Running;
            }
            catch (OperationCanceledException)
            {
                if (IsCurrentLoad(loadVersion))
                {
                    _isLoading = false;
                    _statusMessage = "Conversation load canceled.";
                    ConversationPresenterRuntimeCache.ReleaseConversation(this);
                    NotifyStateChanged();
                }

                return false;
            }
            catch (Exception exception)
            {
                if (IsCurrentLoad(loadVersion))
                {
                    ApplyFailure(exception.Message);
                }

                return false;
            }
        }

        /// <summary>
        /// Advances the active session to its next line or terminal state.
        /// </summary>
        /// <returns>True when the advance request was accepted.</returns>
        public bool AdvanceConversation()
        {
            bool advanced = _session != null && _session.Advance();

            if (advanced)
            {
                UpdateStatusFromSession(null);
            }

            return advanced;
        }

        /// <summary>
        /// Ends the active conversation through the skip action.
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
        /// Cancels the active session or one pending load.
        /// </summary>
        /// <returns>True when the cancel request was accepted.</returns>
        public bool CancelConversation()
        {
            bool canceledPendingLoad = CancelPendingLoad("Conversation load canceled.");
            bool canceledSession = _session != null && _session.Cancel();

            if (canceledSession)
            {
                UpdateStatusFromSession(null);
            }

            return canceledPendingLoad || canceledSession;
        }

        /// <summary>
        /// Resolves one authored portrait for the provided runtime conversant.
        /// </summary>
        /// <param name="actor">Runtime conversant that should be presented.</param>
        /// <returns>The resolved authored portrait when available.</returns>
        public Sprite ResolveActorPortrait(ConversationActorData actor)
        {
            if (actor == null
                || _table == null
                || !_table.TryGetActor(actor.ActorId, out ConversationActorDefinition actorDefinition))
            {
                return null;
            }

            return actorDefinition.Portrait;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Ensures the runner has one working loader, either from the service locator or
        /// from one locally created runtime stack.
        /// </summary>
        /// <returns>True when a loader is available.</returns>
        private bool EnsureRuntimeServices()
        {
            if (_loader != null)
            {
                return true;
            }

            if (ServiceLocator.TryGet<IConversationLoader>(out IConversationLoader loader)
                && loader != null)
            {
                _loader = loader;
                return true;
            }

            ConversationRuntimeSettings settings = ConversationRuntimeSettings.Instance;
            _cache = ConversationLoaderFactory.CreateCache(settings);
            _catalogProvider = ConversationLoaderFactory.CreateCatalogProvider(settings);
            _loader = ConversationLoaderFactory.CreateLoader(settings, _catalogProvider, _cache);
            return _loader != null;
        }

        /// <summary>
        /// Resolves the conversation that should be loaded.
        /// </summary>
        /// <param name="conversationId">Resolved conversation identifier.</param>
        /// <param name="resolvedTitle">Resolved authored title.</param>
        /// <param name="failureReason">Failure reason when selection cannot be resolved.</param>
        /// <returns>True when one valid conversation selection is available.</returns>
        private bool TryResolveConversationSelection(
            out SerializableGuid conversationId,
            out string resolvedTitle,
            out string failureReason)
        {
            conversationId = SerializableGuid.Empty;
            resolvedTitle = string.Empty;
            failureReason = string.Empty;

            if (_table == null)
            {
                failureReason = "ConversationRunner requires one ConversationTable asset.";
                return false;
            }

            _table.EnsureAuthoringIds();

            if (_table.Conversations == null || _table.Conversations.Count == 0)
            {
                failureReason = "The configured ConversationTable does not contain authored conversations.";
                return false;
            }

            string requestedTitle = _conversationTitle?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(requestedTitle))
            {
                ConversationDefinition firstConversation = _table.Conversations[0];
                conversationId = firstConversation.ConversationId;
                resolvedTitle = firstConversation.Title;
                return true;
            }

            for (int index = 0; index < _table.Conversations.Count; index++)
            {
                ConversationDefinition conversation = _table.Conversations[index];

                if (conversation == null)
                {
                    continue;
                }

                if (!string.Equals(
                        conversation.Title,
                        requestedTitle,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                conversationId = conversation.ConversationId;
                resolvedTitle = conversation.Title;
                return true;
            }

            failureReason =
                $"The configured ConversationTable does not contain a conversation named '{requestedTitle}'.";
            return false;
        }

        /// <summary>
        /// Binds one freshly loaded session to the runner.
        /// </summary>
        /// <param name="session">Loaded session instance.</param>
        private void BindSession(ConversationSession session)
        {
            UnsubscribeFromSession();
            _session = session;
            SubscribeToSession();
            NotifyStateChanged();
        }

        /// <summary>
        /// Unbinds and clears the current session.
        /// </summary>
        private void UnbindSession()
        {
            UnsubscribeFromSession();
            _session = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Subscribes to the current session when present.
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
        /// Unsubscribes from the current session when present.
        /// </summary>
        private void UnsubscribeFromSession()
        {
            if (_session == null)
            {
                return;
            }

            _session.Changed -= HandleSessionChanged;
        }

        /// <summary>
        /// Refreshes the runner status after one session mutation.
        /// </summary>
        /// <param name="session">Session that changed.</param>
        private void HandleSessionChanged(ConversationSession session)
        {
            _ = session;
            UpdateStatusFromSession(null);
        }

        /// <summary>
        /// Updates the status message from the current session state.
        /// </summary>
        /// <param name="runningPrefix">Optional prefix used while the session remains active.</param>
        private void UpdateStatusFromSession(string runningPrefix)
        {
            if (_session == null)
            {
                _statusMessage = IdleStatus;
                _failureReason = string.Empty;
                NotifyStateChanged();
                return;
            }

            _failureReason = _session.FailureReason;

            switch (_session.State)
            {
                case ConversationSessionState.Running:
                    _statusMessage = string.IsNullOrWhiteSpace(runningPrefix)
                        ? (_session.HasActiveLine
                            ? "Conversation is presenting the current line."
                            : "Conversation is executing the current node.")
                        : runningPrefix;
                    break;

                case ConversationSessionState.Completed:
                    _statusMessage = "Conversation completed.";
                    ConversationPresenterRuntimeCache.ReleaseConversation(this);
                    break;

                case ConversationSessionState.Canceled:
                    _statusMessage = "Conversation canceled.";
                    ConversationPresenterRuntimeCache.ReleaseConversation(this);
                    break;

                case ConversationSessionState.Faulted:
                    _statusMessage = "Conversation faulted.";
                    ConversationPresenterRuntimeCache.ReleaseConversation(this);
                    break;

                default:
                    _statusMessage = IdleStatus;
                    break;
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// Records one runner failure and clears the current session.
        /// </summary>
        /// <param name="reason">Failure reason to record.</param>
        private void ApplyFailure(string reason)
        {
            UnsubscribeFromSession();
            _session = null;
            _isLoading = false;
            ConversationPresenterRuntimeCache.ReleaseConversation(this);
            _failureReason = string.IsNullOrWhiteSpace(reason)
                ? "Conversation playback failed without a diagnostic message."
                : reason;
            _statusMessage = "Conversation failed.";
            NotifyStateChanged();
        }

        /// <summary>
        /// Cancels one pending load request.
        /// </summary>
        /// <param name="statusMessage">Optional status message recorded after cancellation.</param>
        /// <returns>True when one pending load was canceled.</returns>
        private bool CancelPendingLoad(string statusMessage)
        {
            if (_loadCancellationTokenSource == null)
            {
                return false;
            }

            _loadCancellationTokenSource.Cancel();
            _loadCancellationTokenSource.Dispose();
            _loadCancellationTokenSource = null;

            bool hadPendingLoad = _isLoading;
            _isLoading = false;

            if (hadPendingLoad && !string.IsNullOrWhiteSpace(statusMessage))
            {
                _statusMessage = statusMessage;
                NotifyStateChanged();
            }

            return hadPendingLoad;
        }

        /// <summary>
        /// Gets whether one completed async callback still belongs to the active load request.
        /// </summary>
        /// <param name="loadVersion">Completed load version.</param>
        /// <returns>True when the completion still belongs to the active request.</returns>
        private bool IsCurrentLoad(int loadVersion)
        {
            return loadVersion == _activeLoadVersion;
        }

        /// <summary>
        /// Ensures the resolved table input actions are enabled when the runner owns them.
        /// </summary>
        private void EnsureTrackedActions()
        {
            EnsureActionEnabled(_table?.ContinueAction);
            EnsureActionEnabled(_table?.CancelAction);
            EnsureActionEnabled(_table?.SkipAction);
        }

        /// <summary>
        /// Enables one input action when it is currently disabled.
        /// </summary>
        /// <param name="actionReference">Action reference that should be enabled.</param>
        private void EnsureActionEnabled(InputActionReference actionReference)
        {
            InputAction action = actionReference?.action;

            if (action == null || action.enabled)
            {
                return;
            }

            action.Enable();

            if (!_ownedEnabledActions.Contains(action))
            {
                _ownedEnabledActions.Add(action);
            }
        }

        /// <summary>
        /// Disables the input actions that were enabled by this runner.
        /// </summary>
        private void DisableOwnedActions()
        {
            for (int index = 0; index < _ownedEnabledActions.Count; index++)
            {
                InputAction action = _ownedEnabledActions[index];

                if (action == null || !action.enabled)
                {
                    continue;
                }

                action.Disable();
            }

            _ownedEnabledActions.Clear();
        }

        /// <summary>
        /// Gets whether one action triggered this frame.
        /// </summary>
        /// <param name="actionReference">Action reference to inspect.</param>
        /// <returns>True when the action triggered this frame.</returns>
        private bool WasActionTriggered(InputActionReference actionReference)
        {
            InputAction action = actionReference?.action;

            if (action == null)
            {
                return false;
            }

            EnsureActionEnabled(actionReference);
            return action.triggered;
        }

        /// <summary>
        /// Raises the runner state-changed event.
        /// </summary>
        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(this);
            PlaybackStateChanged?.Invoke();
        }

        #endregion
    }
}