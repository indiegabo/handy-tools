using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace IndieGabo.HandyTools.PoolingModule
{
    /// <summary>
    /// Owns one independent runtime instance for a pool definition asset.
    /// </summary>
    /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
    public sealed class HandyPoolRuntime<TBehaviour>
        where TBehaviour : MonoBehaviour, IPoolSubject<TBehaviour>
    {
        private readonly string _ownerName;
        private readonly int _defaultSize;
        private readonly int _defaultMaxSize;
        private readonly bool _enableCollectionChecksInPlayerBuilds;
        private readonly Dictionary<TBehaviour, PoolRuntimeDefinition<TBehaviour>> _configuredByPrefab;
        private readonly Dictionary<PoolIdentifier, PoolRuntimeDefinition<TBehaviour>> _configuredByIdentifier;
        private readonly Dictionary<TBehaviour, PoolEntryRuntime> _activeByPrefab;
        private readonly Dictionary<PoolIdentifier, PoolEntryRuntime> _activeByIdentifier;
        private readonly Dictionary<TBehaviour, PoolEntryRuntime> _subjectOwners;
        private readonly List<PoolEntryRuntime> _entryBuffer;
        private Transform _container;
        private bool _isAvailable;

        #region Constructors

        internal HandyPoolRuntime(
            string ownerName,
            IReadOnlyList<PoolRuntimeDefinition<TBehaviour>> definitions,
            int defaultSize,
            int defaultMaxSize,
            bool enableCollectionChecksInPlayerBuilds
        )
        {
            if (string.IsNullOrWhiteSpace(ownerName))
            {
                throw new ArgumentException(
                    "Owner name cannot be null, empty, or whitespace.",
                    nameof(ownerName)
                );
            }

            _ownerName = ownerName;
            _defaultSize = Math.Max(0, defaultSize);
            _defaultMaxSize = Math.Max(1, defaultMaxSize);
            _enableCollectionChecksInPlayerBuilds = enableCollectionChecksInPlayerBuilds;
            _configuredByPrefab = new Dictionary<TBehaviour, PoolRuntimeDefinition<TBehaviour>>(
                definitions?.Count ?? 0
            );
            _configuredByIdentifier = new Dictionary<PoolIdentifier, PoolRuntimeDefinition<TBehaviour>>(
                definitions?.Count ?? 0
            );
            _activeByPrefab = new Dictionary<TBehaviour, PoolEntryRuntime>(
                definitions?.Count ?? 0
            );
            _activeByIdentifier = new Dictionary<PoolIdentifier, PoolEntryRuntime>(
                definitions?.Count ?? 0
            );
            _subjectOwners = new Dictionary<TBehaviour, PoolEntryRuntime>();
            _entryBuffer = new List<PoolEntryRuntime>(definitions?.Count ?? 0);

            RegisterDefinitions(definitions);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the runtime currently owns active
        /// pools.
        /// </summary>
        public bool IsAvailable => _isAvailable;

        #endregion

        #region Runtime Lifecycle

        /// <summary>
        /// Initializes every configured pool entry for this runtime instance.
        /// </summary>
        /// <param name="container">Optional parent for created subjects.</param>
        /// <param name="additionalPrewarmPerPool">
        /// Additional prewarm amount applied to every configured subpool.
        /// Fractional values are rounded up.
        /// </param>
        public void Initialize(
            Transform container = null,
            float additionalPrewarmPerPool = 0
        )
        {
            if (_isAvailable)
            {
                Dismiss();
            }

            _container = container;
            _isAvailable = true;

            foreach (PoolRuntimeDefinition<TBehaviour> definition in _configuredByPrefab.Values)
            {
                EnsureEntry(definition, additionalPrewarmPerPool);
            }
        }

        /// <summary>
        /// Destroys every created subject and unregisters every identified
        /// active subpool owned by this runtime.
        /// </summary>
        public void Dismiss()
        {
            if (_activeByPrefab.Count > 0)
            {
                _entryBuffer.Clear();
                foreach (PoolEntryRuntime entry in _activeByPrefab.Values)
                {
                    _entryBuffer.Add(entry);
                }

                for (int index = 0; index < _entryBuffer.Count; index++)
                {
                    _entryBuffer[index].Dismiss();
                }

                _entryBuffer.Clear();
            }

            _activeByPrefab.Clear();
            _activeByIdentifier.Clear();
            _subjectOwners.Clear();
            _container = null;
            _isAvailable = false;
        }

        #endregion

        #region Pool Creation

        /// <summary>
        /// Creates a prefab-keyed subpool when missing and optionally prewarms
        /// additional instances.
        /// </summary>
        /// <param name="prefab">Prefab used to back the subpool.</param>
        /// <param name="additionalPrewarmCount">
        /// Additional prewarm count for the created subpool.
        /// </param>
        public void RequestPoolCreation(
            TBehaviour prefab,
            float additionalPrewarmCount = 0
        )
        {
            if (prefab == null)
            {
                return;
            }

            EnsureLazyAvailability();
            PoolRuntimeDefinition<TBehaviour> definition = GetOrCreateDefinition(prefab);
            EnsureEntry(definition, additionalPrewarmCount);
        }

        /// <summary>
        /// Creates an identified subpool from a configured entry when missing
        /// and optionally prewarms additional instances.
        /// </summary>
        /// <param name="identifier">Configured pool identifier.</param>
        /// <param name="additionalPrewarmCount">
        /// Additional prewarm count for the created subpool.
        /// </param>
        public void RequestPoolCreation(
            PoolIdentifier identifier,
            float additionalPrewarmCount = 0
        )
        {
            EnsureLazyAvailability();
            PoolRuntimeDefinition<TBehaviour> definition = GetRequiredDefinition(identifier);
            EnsureEntry(definition, additionalPrewarmCount);
        }

        /// <summary>
        /// Creates a new runtime-defined identified subpool when missing and
        /// optionally prewarms additional instances.
        /// </summary>
        /// <param name="identifier">Identifier assigned to the subpool.</param>
        /// <param name="prefab">Prefab used to back the subpool.</param>
        /// <param name="additionalPrewarmCount">
        /// Additional prewarm count for the created subpool.
        /// </param>
        public void RequestPoolCreation(
            PoolIdentifier identifier,
            TBehaviour prefab,
            float additionalPrewarmCount = 0
        )
        {
            if (prefab == null)
            {
                return;
            }

            EnsureLazyAvailability();
            PoolRuntimeDefinition<TBehaviour> definition = GetOrCreateDefinition(
                identifier,
                prefab
            );
            EnsureEntry(definition, additionalPrewarmCount);
        }

        #endregion

        #region Getting

        /// <summary>
        /// Retrieves one active instance from the prefab-keyed subpool.
        /// </summary>
        /// <param name="prefab">Prefab used to resolve the subpool.</param>
        /// <returns>Active pooled subject.</returns>
        public TBehaviour Get(TBehaviour prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            EnsureLazyAvailability();
            PoolRuntimeDefinition<TBehaviour> definition = GetOrCreateDefinition(prefab);
            return EnsureEntry(definition).Get();
        }

        /// <summary>
        /// Retrieves one active instance from the identified subpool.
        /// </summary>
        /// <param name="identifier">Pool identifier.</param>
        /// <returns>Active pooled subject.</returns>
        public TBehaviour Get(PoolIdentifier identifier)
        {
            EnsureLazyAvailability();
            PoolRuntimeDefinition<TBehaviour> definition = GetRequiredDefinition(identifier);
            return EnsureEntry(definition).Get();
        }

        /// <summary>
        /// Attempts to retrieve one active instance from an already-created
        /// prefab-keyed subpool.
        /// </summary>
        /// <param name="prefab">Prefab used to resolve the subpool.</param>
        /// <param name="subject">Retrieved active subject.</param>
        /// <returns>True when the subpool already exists.</returns>
        public bool TryGet(TBehaviour prefab, out TBehaviour subject)
        {
            if (prefab != null && _activeByPrefab.TryGetValue(prefab, out PoolEntryRuntime entry))
            {
                return entry.TryGet(out subject);
            }

            subject = null;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve one active instance from an already-created
        /// identified subpool.
        /// </summary>
        /// <param name="identifier">Pool identifier.</param>
        /// <param name="subject">Retrieved active subject.</param>
        /// <returns>True when the subpool already exists.</returns>
        public bool TryGet(PoolIdentifier identifier, out TBehaviour subject)
        {
            if (
                identifier.IsValid
                && _activeByIdentifier.TryGetValue(identifier, out PoolEntryRuntime entry)
            )
            {
                return entry.TryGet(out subject);
            }

            subject = null;
            return false;
        }

        #endregion

        #region Releasing

        /// <summary>
        /// Returns one subject to its owning subpool.
        /// </summary>
        /// <param name="subject">Subject to return.</param>
        public void Release(TBehaviour subject)
        {
            if (!TryRelease(subject))
            {
                throw new InvalidOperationException(
                    $"Subject '{subject}' is not owned by runtime '{_ownerName}'."
                );
            }
        }

        /// <summary>
        /// Attempts to return one subject to its owning subpool.
        /// </summary>
        /// <param name="subject">Subject to return.</param>
        /// <returns>True when the subject belongs to this runtime.</returns>
        public bool TryRelease(TBehaviour subject)
        {
            return subject != null
                && _subjectOwners.TryGetValue(subject, out PoolEntryRuntime entry)
                && entry.TryRelease(subject);
        }

        #endregion

        #region Diagnostics

        /// <summary>
        /// Gets whether a prefab-keyed subpool is currently active.
        /// </summary>
        /// <param name="prefab">Prefab used to resolve the subpool.</param>
        /// <returns>True when the subpool is active.</returns>
        public bool HasPool(TBehaviour prefab)
        {
            return prefab != null && _activeByPrefab.ContainsKey(prefab);
        }

        /// <summary>
        /// Gets whether an identified subpool is currently active.
        /// </summary>
        /// <param name="identifier">Pool identifier.</param>
        /// <returns>True when the subpool is active.</returns>
        public bool HasPool(PoolIdentifier identifier)
        {
            return identifier.IsValid && _activeByIdentifier.ContainsKey(identifier);
        }

        /// <summary>
        /// Attempts to capture runtime counters for one active prefab-keyed
        /// subpool.
        /// </summary>
        /// <param name="prefab">Prefab used to resolve the subpool.</param>
        /// <param name="statistics">Captured statistics snapshot.</param>
        /// <returns>True when the subpool is active.</returns>
        public bool TryGetStatistics(
            TBehaviour prefab,
            out PoolStatistics statistics
        )
        {
            if (prefab != null && _activeByPrefab.TryGetValue(prefab, out PoolEntryRuntime entry))
            {
                statistics = entry.GetStatistics();
                return true;
            }

            statistics = default;
            return false;
        }

        /// <summary>
        /// Attempts to capture runtime counters for one active identified
        /// subpool.
        /// </summary>
        /// <param name="identifier">Pool identifier.</param>
        /// <param name="statistics">Captured statistics snapshot.</param>
        /// <returns>True when the subpool is active.</returns>
        public bool TryGetStatistics(
            PoolIdentifier identifier,
            out PoolStatistics statistics
        )
        {
            if (
                identifier.IsValid
                && _activeByIdentifier.TryGetValue(identifier, out PoolEntryRuntime entry)
            )
            {
                statistics = entry.GetStatistics();
                return true;
            }

            statistics = default;
            return false;
        }

        #endregion

        #region Internal Runtime Helpers

        internal Transform Container => _container;

        internal bool ResolveCollectionChecks(bool requestedCollectionCheck)
        {
            if (!requestedCollectionCheck)
            {
                return false;
            }

            return Application.isEditor
                || Debug.isDebugBuild
                || _enableCollectionChecksInPlayerBuilds;
        }

        private void TrackSubject(TBehaviour subject, PoolEntryRuntime owner)
        {
            if (subject == null || owner == null)
            {
                return;
            }

            _subjectOwners[subject] = owner;
        }

        internal void ForgetSubject(TBehaviour subject)
        {
            if (subject != null)
            {
                _subjectOwners.Remove(subject);
            }
        }

        #endregion

        #region Definition Management

        private void RegisterDefinitions(IReadOnlyList<PoolRuntimeDefinition<TBehaviour>> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            for (int index = 0; index < definitions.Count; index++)
            {
                PoolRuntimeDefinition<TBehaviour> definition = definitions[index];
                if (definition.Prefab == null)
                {
                    continue;
                }

                if (_configuredByPrefab.ContainsKey(definition.Prefab))
                {
                    throw new InvalidOperationException(
                        $"Pool definition '{_ownerName}' contains duplicate prefab '{definition.Prefab.name}'."
                    );
                }

                _configuredByPrefab.Add(definition.Prefab, definition);

                if (!definition.HasIdentifier)
                {
                    continue;
                }

                if (_configuredByIdentifier.ContainsKey(definition.Identifier))
                {
                    throw new InvalidOperationException(
                        $"Pool definition '{_ownerName}' contains duplicate identifier '{definition.Identifier}'."
                    );
                }

                _configuredByIdentifier.Add(definition.Identifier, definition);
            }
        }

        private PoolRuntimeDefinition<TBehaviour> GetOrCreateDefinition(TBehaviour prefab)
        {
            if (_configuredByPrefab.TryGetValue(prefab, out PoolRuntimeDefinition<TBehaviour> definition))
            {
                return definition;
            }

            return CreateDynamicDefinition(prefab);
        }

        private PoolRuntimeDefinition<TBehaviour> GetRequiredDefinition(PoolIdentifier identifier)
        {
            if (!identifier.IsValid)
            {
                throw new ArgumentException(
                    "Pool identifier must be valid.",
                    nameof(identifier)
                );
            }

            if (_configuredByIdentifier.TryGetValue(identifier, out PoolRuntimeDefinition<TBehaviour> definition))
            {
                return definition;
            }

            throw new InvalidOperationException(
                $"Runtime '{_ownerName}' does not define a pool with identifier '{identifier}'."
            );
        }

        private PoolRuntimeDefinition<TBehaviour> GetOrCreateDefinition(
            PoolIdentifier identifier,
            TBehaviour prefab
        )
        {
            if (!identifier.IsValid)
            {
                throw new ArgumentException(
                    "Pool identifier must be valid.",
                    nameof(identifier)
                );
            }

            if (_configuredByIdentifier.TryGetValue(identifier, out PoolRuntimeDefinition<TBehaviour> configured))
            {
                if (configured.Prefab != prefab)
                {
                    throw new InvalidOperationException(
                        $"Runtime '{_ownerName}' already binds identifier '{identifier}' to prefab '{configured.Prefab.name}'."
                    );
                }

                return configured;
            }

            if (_configuredByPrefab.TryGetValue(prefab, out PoolRuntimeDefinition<TBehaviour> byPrefab))
            {
                if (byPrefab.HasIdentifier && byPrefab.Identifier != identifier)
                {
                    throw new InvalidOperationException(
                        $"Runtime '{_ownerName}' already binds prefab '{prefab.name}' to identifier '{byPrefab.Identifier}'."
                    );
                }

                return byPrefab.HasIdentifier
                    ? byPrefab
                    : PromoteDefinitionIdentifier(byPrefab, identifier);
            }

            return RegisterDynamicDefinition(CreateDynamicDefinition(prefab, identifier));
        }

        private PoolRuntimeDefinition<TBehaviour> PromoteDefinitionIdentifier(
            PoolRuntimeDefinition<TBehaviour> definition,
            PoolIdentifier identifier
        )
        {
            PoolRuntimeDefinition<TBehaviour> promotedDefinition = new(
                definition.Prefab,
                identifier,
                true,
                definition.InitialCapacity,
                definition.MaxSize,
                definition.PrewarmCount,
                definition.CollectionCheck
            );

            _configuredByPrefab[definition.Prefab] = promotedDefinition;
            _configuredByIdentifier[identifier] = promotedDefinition;

            if (_activeByPrefab.TryGetValue(definition.Prefab, out PoolEntryRuntime activeEntry))
            {
                activeEntry.PromoteIdentifier(identifier);
                _activeByIdentifier[identifier] = activeEntry;
            }

            return promotedDefinition;
        }

        private PoolRuntimeDefinition<TBehaviour> CreateDynamicDefinition(
            TBehaviour prefab,
            PoolIdentifier identifier = default
        )
        {
            return new PoolRuntimeDefinition<TBehaviour>(
                prefab,
                identifier,
                identifier.IsValid,
                _defaultSize,
                _defaultMaxSize,
                0,
                true
            );
        }

        private PoolRuntimeDefinition<TBehaviour> RegisterDynamicDefinition(
            PoolRuntimeDefinition<TBehaviour> definition
        )
        {
            if (definition.Prefab != null && !_configuredByPrefab.ContainsKey(definition.Prefab))
            {
                _configuredByPrefab.Add(definition.Prefab, definition);
            }

            if (definition.HasIdentifier && !_configuredByIdentifier.ContainsKey(definition.Identifier))
            {
                _configuredByIdentifier.Add(definition.Identifier, definition);
            }

            return definition;
        }

        #endregion

        #region Active Entries

        private void EnsureLazyAvailability()
        {
            if (!_isAvailable)
            {
                _isAvailable = true;
            }
        }

        private PoolEntryRuntime EnsureEntry(
            PoolRuntimeDefinition<TBehaviour> definition,
            float additionalPrewarmCount = 0
        )
        {
            if (_activeByPrefab.TryGetValue(definition.Prefab, out PoolEntryRuntime existingEntry))
            {
                if (definition.HasIdentifier)
                {
                    existingEntry.PromoteIdentifier(definition.Identifier);
                    _activeByIdentifier[definition.Identifier] = existingEntry;
                }

                existingEntry.Prewarm(Mathf.Max(0, Mathf.CeilToInt(additionalPrewarmCount)));
                return existingEntry;
            }

            PoolEntryRuntime entry = new(this, definition);
            _activeByPrefab.Add(definition.Prefab, entry);

            if (definition.HasIdentifier)
            {
                _activeByIdentifier.Add(definition.Identifier, entry);
            }

            entry.Prewarm(definition.PrewarmCount + Mathf.Max(0, Mathf.CeilToInt(additionalPrewarmCount)));
            return entry;
        }

        #endregion

        private sealed class PoolEntryRuntime : IHandyPool<TBehaviour>
        {
            private readonly HandyPoolRuntime<TBehaviour> _owner;
            private readonly PoolRuntimeDefinition<TBehaviour> _definition;
            private readonly HashSet<TBehaviour> _createdSubjects;
            private readonly List<TBehaviour> _destroyBuffer;
            private readonly List<TBehaviour> _prewarmBuffer;
            private PoolIdentifier _identifier;
            private ObjectPool<TBehaviour> _pool;
            private bool _isRegistered;

            public PoolEntryRuntime(
                HandyPoolRuntime<TBehaviour> owner,
                PoolRuntimeDefinition<TBehaviour> definition
            )
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _definition = definition;
                _createdSubjects = new HashSet<TBehaviour>();
                _destroyBuffer = new List<TBehaviour>();
                _prewarmBuffer = new List<TBehaviour>();
                _identifier = definition.Identifier;
            }

            public PoolIdentifier Identifier => _identifier;

            public TBehaviour Prefab => _definition.Prefab;

            public bool IsAvailable => _pool != null;

            public TBehaviour Get()
            {
                EnsurePool();
                return _pool.Get();
            }

            public bool TryGet(out TBehaviour subject)
            {
                if (_pool == null)
                {
                    subject = null;
                    return false;
                }

                subject = _pool.Get();
                return true;
            }

            public void Release(TBehaviour subject)
            {
                if (!TryRelease(subject))
                {
                    throw new InvalidOperationException(
                        $"Subject '{subject}' does not belong to pool '{GetDisplayName()}'."
                    );
                }
            }

            public bool TryRelease(TBehaviour subject)
            {
                if (subject == null || _pool == null || !_createdSubjects.Contains(subject))
                {
                    return false;
                }

                _pool.Release(subject);
                return true;
            }

            public PoolStatistics GetStatistics()
            {
                int countAll = _pool != null ? _pool.CountAll : 0;
                int countInactive = _pool != null ? _pool.CountInactive : 0;

                return new PoolStatistics(
                    Identifier,
                    GetDisplayName(),
                    countAll,
                    countInactive,
                    _definition.InitialCapacity,
                    _definition.MaxSize
                );
            }

            public void Prewarm(int count)
            {
                if (count <= 0)
                {
                    return;
                }

                EnsurePool();
                _prewarmBuffer.Clear();

                for (int index = 0; index < count; index++)
                {
                    _prewarmBuffer.Add(_pool.Get());
                }

                for (int index = 0; index < _prewarmBuffer.Count; index++)
                {
                    _pool.Release(_prewarmBuffer[index]);
                }

                _prewarmBuffer.Clear();
            }

            public void Dismiss()
            {
                if (_isRegistered)
                {
                    PoolRegistry.Deregister(Identifier, this);
                    _isRegistered = false;
                }

                if (_pool == null)
                {
                    return;
                }

                _destroyBuffer.Clear();

                foreach (TBehaviour subject in _createdSubjects)
                {
                    if (subject != null)
                    {
                        _destroyBuffer.Add(subject);
                    }
                }

                for (int index = 0; index < _destroyBuffer.Count; index++)
                {
                    TBehaviour subject = _destroyBuffer[index];
                    _owner.ForgetSubject(subject);
                    DestroyOwnedSubject(subject.gameObject);
                }

                _destroyBuffer.Clear();
                _createdSubjects.Clear();
                _prewarmBuffer.Clear();
                _pool = null;
            }

            public void PromoteIdentifier(PoolIdentifier identifier)
            {
                if (!identifier.IsValid)
                {
                    return;
                }

                if (_identifier.IsValid)
                {
                    if (_identifier != identifier)
                    {
                        throw new InvalidOperationException(
                            $"Pool '{GetDisplayName()}' already uses identifier '{_identifier}'."
                        );
                    }

                    return;
                }

                _identifier = identifier;

                if (_pool != null && !_isRegistered)
                {
                    PoolRegistry.Register(_identifier, this);
                    _isRegistered = true;
                }
            }

            private void EnsurePool()
            {
                if (_pool != null)
                {
                    return;
                }

                _pool = new ObjectPool<TBehaviour>(
                    Create,
                    OnTakenFromPool,
                    OnReturnedToPool,
                    OnDestroyInPool,
                    _owner.ResolveCollectionChecks(_definition.CollectionCheck),
                    _definition.InitialCapacity,
                    _definition.MaxSize
                );

                if (_identifier.IsValid)
                {
                    PoolRegistry.Register(_identifier, this);
                    _isRegistered = true;
                }
            }

            private TBehaviour Create()
            {
                TBehaviour subject = UnityEngine.Object.Instantiate(
                    _definition.Prefab,
                    _owner.Container
                );

                subject.gameObject.SetActive(false);
                subject.SetPool(this);
                _createdSubjects.Add(subject);
                _owner.TrackSubject(subject, this);
                return subject;
            }

            private void OnTakenFromPool(TBehaviour subject)
            {
                subject.gameObject.SetActive(true);
                subject.OnTakenFromPool();
            }

            private void OnReturnedToPool(TBehaviour subject)
            {
                subject.OnReturnedToPool();
                subject.gameObject.SetActive(false);
            }

            private void OnDestroyInPool(TBehaviour subject)
            {
                _createdSubjects.Remove(subject);
                _owner.ForgetSubject(subject);

                if (subject != null)
                {
                    DestroyOwnedSubject(subject.gameObject);
                }
            }

            private static void DestroyOwnedSubject(GameObject subjectObject)
            {
                if (subjectObject == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(subjectObject);
                    return;
                }

                UnityEngine.Object.DestroyImmediate(subjectObject);
            }

            private string GetDisplayName()
            {
                return Identifier.IsValid ? Identifier.ToString() : Prefab.name;
            }
        }
    }

    internal readonly struct PoolRuntimeDefinition<TBehaviour>
        where TBehaviour : MonoBehaviour
    {
        public PoolRuntimeDefinition(
            TBehaviour prefab,
            PoolIdentifier identifier,
            bool hasIdentifier,
            int initialCapacity,
            int maxSize,
            int prewarmCount,
            bool collectionCheck
        )
        {
            Prefab = prefab;
            Identifier = identifier;
            HasIdentifier = hasIdentifier;
            InitialCapacity = Math.Max(0, initialCapacity);
            MaxSize = Math.Max(1, maxSize);
            PrewarmCount = Math.Max(0, prewarmCount);
            CollectionCheck = collectionCheck;
        }

        public TBehaviour Prefab { get; }

        public PoolIdentifier Identifier { get; }

        public bool HasIdentifier { get; }

        public int InitialCapacity { get; }

        public int MaxSize { get; }

        public int PrewarmCount { get; }

        public bool CollectionCheck { get; }
    }
}