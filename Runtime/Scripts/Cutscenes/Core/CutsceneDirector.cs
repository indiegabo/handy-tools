using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Services;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Core
{
    [DisallowMultipleComponent]
    public sealed class CutsceneDirector : HandyBehaviour
    {
        /// <summary>
        /// Stores one persisted foldout state for the graph blackboard UI.
        /// </summary>
        [Serializable]
        private sealed class BlackboardFoldoutState
        {
            [SerializeField]
            private string _entryKey = string.Empty;

            [SerializeField]
            private bool _isExpanded = true;

            /// <summary>
            /// Initializes one empty foldout state for Unity serialization.
            /// </summary>
            public BlackboardFoldoutState() { }

            /// <summary>
            /// Initializes one foldout state for the provided entry key.
            /// </summary>
            /// <param name="entryKey">Unique blackboard entry key.</param>
            /// <param name="isExpanded">Whether the foldout is expanded.</param>
            public BlackboardFoldoutState(
                string entryKey,
                bool isExpanded)
            {
                _entryKey = entryKey ?? string.Empty;
                _isExpanded = isExpanded;
            }

            /// <summary>
            /// Gets the stored blackboard entry key.
            /// </summary>
            public string EntryKey => _entryKey ?? string.Empty;

            /// <summary>
            /// Gets the stored foldout expansion state.
            /// </summary>
            public bool IsExpanded => _isExpanded;

            /// <summary>
            /// Replaces the stored blackboard entry key.
            /// </summary>
            /// <param name="entryKey">New unique blackboard entry key.</param>
            public void SetEntryKey(string entryKey)
            {
                _entryKey = entryKey ?? string.Empty;
            }

            /// <summary>
            /// Replaces the stored foldout expansion state.
            /// </summary>
            /// <param name="isExpanded">New expansion state.</param>
            public void SetExpanded(bool isExpanded)
            {
                _isExpanded = isExpanded;
            }
        }

        [SerializeField]
        private string _title = "Cutscene";

        [TextArea(2, 4)]
        [SerializeField]
        private string _description;

        [SerializeField]
        private CutsceneDirectorPlayPolicy _playPolicy = CutsceneDirectorPlayPolicy.IgnoreIfAlreadyRunning;

        [SerializeField]
        private CutsceneTimeMode _timeMode = CutsceneTimeMode.Scaled;

        [SerializeField]
        private bool _autoplayOnStart;

        [SerializeField]
        private bool _oneShot;

        [SerializeField]
        private bool _cancelOnDisable = true;

        [SerializeField]
        private CutsceneGraph _graph = new();

        [HideInInspector]
        [SerializeField]
        private List<BlackboardFoldoutState> _blackboardFoldoutStates = new();

        private ICutsceneService _service;
        private CutsceneRun _latestRun;
        private bool _hasPlayedOnce;

        public string Title => _title;

        public string Description => _description;

        public CutsceneDirectorPlayPolicy PlayPolicy => _playPolicy;

        public CutsceneTimeMode TimeMode => _timeMode;

        public bool AutoplayOnStart => _autoplayOnStart;

        public bool OneShot => _oneShot;

        public CutsceneGraph Graph => _graph ??= CutsceneGraph.CreateDefault();

        public bool IsRunning => _service != null && _service.IsRunning(this);

        public CutsceneRunStatus RuntimeStatus
        {
            get
            {
                return TryGetActiveRun(out CutsceneRun run)
                    ? run.Status
                    : CutsceneRunStatus.Idle;
            }
        }

        public string RuntimeFailureReason
        {
            get
            {
                return TryGetActiveRun(out CutsceneRun run)
                    ? run.FailureReason
                    : string.Empty;
            }
        }

        public bool CanPlay => _service != null && (!_oneShot || !_hasPlayedOnce || IsRunning);

        private void Awake()
        {
            ResolveService();
            Graph.EnsureNodeIds();
        }

        private void Start()
        {
            ResolveService();

            if (_autoplayOnStart)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            if (_cancelOnDisable)
            {
                Cancel();
            }
        }

        private void OnValidate()
        {
            Graph.EnsureNodeIds();
        }

        public void Play()
        {
            ResolveService();

            if (!CanPlay)
            {
                return;
            }

            CutsceneRun run = _service.StartDirector(this);

            if (run != null)
            {
                _latestRun = run;
                _hasPlayedOnce = _hasPlayedOnce || _oneShot;
            }
        }

        public void Restart()
        {
            ResolveService();
            _service?.StopDirector(this, "Restart requested.");

            CutsceneRun run = _service?.StartDirector(this);

            if (run != null)
            {
                _latestRun = run;
            }
        }

        public void Cancel()
        {
            ResolveService();
            _service?.StopDirector(this, "Cancelled by director.");
        }

        public bool TryGetActiveRun(out CutsceneRun run)
        {
            ResolveService();
            run = null;
            return _service != null && _service.TryGetActiveRun(this, out run);
        }

        public bool TryGetRuntimeRun(out CutsceneRun run)
        {
            if (TryGetActiveRun(out run))
            {
                _latestRun = run;
                return true;
            }

            run = _latestRun;
            return run != null;
        }

        public void ReplaceGraph(CutsceneGraph graph)
        {
            _graph = graph ?? CutsceneGraph.CreateDefault();
            _graph.EnsureNodeIds();
            _latestRun = null;
        }

        /// <summary>
        /// Attempts to resolve one persisted foldout state for a blackboard entry.
        /// </summary>
        /// <param name="entryKey">Unique blackboard entry key.</param>
        /// <param name="isExpanded">Stored expansion state when available.</param>
        /// <returns>True when one persisted state exists for the provided key.</returns>
        public bool TryGetBlackboardFoldoutState(
            string entryKey,
            out bool isExpanded)
        {
            isExpanded = default;
            string normalizedKey = NormalizeBlackboardFoldoutKey(entryKey);

            if (string.IsNullOrEmpty(normalizedKey))
            {
                return false;
            }

            BlackboardFoldoutState state = FindBlackboardFoldoutState(normalizedKey);

            if (state == null)
            {
                return false;
            }

            isExpanded = state.IsExpanded;
            return true;
        }

        /// <summary>
        /// Persists one foldout expansion state for a blackboard entry.
        /// </summary>
        /// <param name="entryKey">Unique blackboard entry key.</param>
        /// <param name="isExpanded">Expansion state that should be stored.</param>
        /// <returns>True when the serialized state changed.</returns>
        public bool SetBlackboardFoldoutState(
            string entryKey,
            bool isExpanded)
        {
            string normalizedKey = NormalizeBlackboardFoldoutKey(entryKey);

            if (string.IsNullOrEmpty(normalizedKey))
            {
                return false;
            }

            BlackboardFoldoutState state = FindBlackboardFoldoutState(normalizedKey);

            if (state == null)
            {
                _blackboardFoldoutStates.Add(new BlackboardFoldoutState(
                    normalizedKey,
                    isExpanded));
                return true;
            }

            if (state.IsExpanded == isExpanded)
            {
                return false;
            }

            state.SetExpanded(isExpanded);
            return true;
        }

        /// <summary>
        /// Removes one persisted foldout state for a blackboard entry.
        /// </summary>
        /// <param name="entryKey">Unique blackboard entry key.</param>
        /// <returns>True when at least one serialized state entry was removed.</returns>
        public bool RemoveBlackboardFoldoutState(string entryKey)
        {
            string normalizedKey = NormalizeBlackboardFoldoutKey(entryKey);

            if (string.IsNullOrEmpty(normalizedKey))
            {
                return false;
            }

            return _blackboardFoldoutStates.RemoveAll(candidate => string.Equals(
                candidate.EntryKey,
                normalizedKey,
                StringComparison.OrdinalIgnoreCase)) > 0;
        }

        /// <summary>
        /// Renames one persisted foldout state when the authored blackboard key changes.
        /// </summary>
        /// <param name="oldEntryKey">Previous unique blackboard entry key.</param>
        /// <param name="newEntryKey">New unique blackboard entry key.</param>
        /// <returns>True when the serialized state changed.</returns>
        public bool RenameBlackboardFoldoutState(
            string oldEntryKey,
            string newEntryKey)
        {
            string normalizedOldKey = NormalizeBlackboardFoldoutKey(oldEntryKey);
            string normalizedNewKey = NormalizeBlackboardFoldoutKey(newEntryKey);

            if (string.IsNullOrEmpty(normalizedOldKey)
                || string.IsNullOrEmpty(normalizedNewKey)
                || string.Equals(
                    normalizedOldKey,
                    normalizedNewKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            BlackboardFoldoutState sourceState =
                FindBlackboardFoldoutState(normalizedOldKey);

            if (sourceState == null)
            {
                return false;
            }

            BlackboardFoldoutState targetState =
                FindBlackboardFoldoutState(normalizedNewKey);

            if (targetState != null)
            {
                targetState.SetExpanded(sourceState.IsExpanded);
                RemoveBlackboardFoldoutState(normalizedOldKey);
                return true;
            }

            sourceState.SetEntryKey(normalizedNewKey);
            return true;
        }

        private void ResolveService()
        {
            if (_service != null)
            {
                return;
            }

            ServiceLocator.TryGet(out _service);
        }

        /// <summary>
        /// Resolves one persisted foldout state by key.
        /// </summary>
        /// <param name="entryKey">Normalized blackboard entry key.</param>
        /// <returns>The stored foldout state when available.</returns>
        private BlackboardFoldoutState FindBlackboardFoldoutState(string entryKey)
        {
            return _blackboardFoldoutStates.Find(candidate => string.Equals(
                candidate.EntryKey,
                entryKey,
                StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Normalizes one blackboard foldout state key for serialized storage.
        /// </summary>
        /// <param name="entryKey">Requested blackboard entry key.</param>
        /// <returns>The normalized key used by serialized state entries.</returns>
        private static string NormalizeBlackboardFoldoutKey(string entryKey)
        {
            return string.IsNullOrWhiteSpace(entryKey)
                ? string.Empty
                : entryKey.Trim();
        }
    }
}