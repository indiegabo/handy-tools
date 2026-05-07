using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.PoolingModule
{
    /// <summary>
    /// Base ScriptableObject pool definition that can spawn one or more
    /// independent runtime instances.
    /// </summary>
    public abstract class HandyPool<TBehaviour> : ScriptableObject
        where TBehaviour : MonoBehaviour, IPoolSubject<TBehaviour>
    {
        #region Inspector

        [SerializeField]
        [Min(0)]
        private int _defaultSize;

        [SerializeField]
        [Min(1)]
        private int _maxSize = 10000;

        [SerializeField]
        private bool _enableCollectionChecksInPlayerBuilds;

        [SerializeField]
        private List<PoolEntryConfiguration> _entries = new();

        [SerializeField]
        private UnityEvent _initialized;

        [SerializeField]
        private UnityEvent _dismissed;

        #endregion

        #region Fields

        private HandyPoolRuntime<TBehaviour> _defaultRuntime;

        #endregion

        #region Getters

        /// <summary>
        /// Gets whether the default runtime instance currently owns active
        /// pools.
        /// </summary>
        public bool IsAvailable => _defaultRuntime != null && _defaultRuntime.IsAvailable;

        #endregion

        #region Initializing

        /// <summary>
        /// Creates one independent runtime instance from this pool definition.
        /// </summary>
        /// <returns>Independent pool runtime instance.</returns>
        public HandyPoolRuntime<TBehaviour> CreateRuntime()
        {
            return new HandyPoolRuntime<TBehaviour>(
                name,
                BuildRuntimeDefinitions(),
                _defaultSize,
                _maxSize,
                _enableCollectionChecksInPlayerBuilds
            );
        }

        /// <summary>
        /// Initializes the default runtime instance and optionally prewarms all
        /// configured entries.
        /// </summary>
        /// <param name="container">Optional parent for instantiated subjects.</param>
        /// <param name="initialAmount">
        /// Additional prewarm amount per configured subpool. Fractional values
        /// are rounded up.
        /// </param>
        public void Initialize(Transform container = null, float initialAmount = 0)
        {
            _defaultRuntime = CreateRuntime();
            _defaultRuntime.Initialize(container, initialAmount);
            _initialized?.Invoke();
        }

        /// <summary>
        /// Dismisses the default runtime instance and destroys all tracked
        /// pooled subjects it owns.
        /// </summary>
        public void Dismiss()
        {
            _defaultRuntime?.Dismiss();
            _dismissed?.Invoke();
        }

        #endregion

        #region Creating

        /// <summary>
        /// Creates a prefab-keyed subpool in the default runtime when missing
        /// and optionally prewarms additional instances.
        /// </summary>
        /// <param name="prefab">Prefab used to create pooled instances.</param>
        /// <param name="initialAmount">
        /// Additional prewarm amount for the created subpool. Fractional
        /// values are rounded up.
        /// </param>
        public void RequestPoolCreation(TBehaviour prefab, float initialAmount = 0)
        {
            EnsureDefaultRuntime().RequestPoolCreation(prefab, initialAmount);
        }

        /// <summary>
        /// Creates an identified configured subpool in the default runtime
        /// when missing and optionally prewarms additional instances.
        /// </summary>
        /// <param name="identifier">Configured pool identifier.</param>
        /// <param name="initialAmount">
        /// Additional prewarm amount for the created subpool. Fractional
        /// values are rounded up.
        /// </param>
        public void RequestPoolCreation(
            PoolIdentifier identifier,
            float initialAmount = 0
        )
        {
            EnsureDefaultRuntime().RequestPoolCreation(identifier, initialAmount);
        }

        /// <summary>
        /// Creates a new runtime-defined identified subpool in the default
        /// runtime when missing and optionally prewarms additional instances.
        /// </summary>
        /// <param name="identifier">Identifier assigned to the subpool.</param>
        /// <param name="prefab">Prefab used to create pooled instances.</param>
        /// <param name="initialAmount">
        /// Additional prewarm amount for the created subpool. Fractional
        /// values are rounded up.
        /// </param>
        public void RequestPoolCreation(
            PoolIdentifier identifier,
            TBehaviour prefab,
            float initialAmount = 0
        )
        {
            EnsureDefaultRuntime().RequestPoolCreation(
                identifier,
                prefab,
                initialAmount
            );
        }

        #endregion

        #region Getting

        /// <summary>
        /// Gets an instance from the default runtime for the provided prefab.
        /// </summary>
        /// <param name="prefab">Prefab pool to use.</param>
        /// <returns>An active pooled instance.</returns>
        public TBehaviour Get(TBehaviour prefab)
        {
            return EnsureDefaultRuntime().Get(prefab);
        }

        /// <summary>
        /// Gets an instance from the default runtime for the provided
        /// identifier.
        /// </summary>
        /// <param name="identifier">Configured pool identifier.</param>
        /// <returns>An active pooled instance.</returns>
        public TBehaviour Get(PoolIdentifier identifier)
        {
            return EnsureDefaultRuntime().Get(identifier);
        }

        /// <summary>
        /// Attempts to get an instance from an already-created prefab pool in
        /// the default runtime.
        /// </summary>
        /// <param name="prefab">Prefab pool to query.</param>
        /// <param name="subject">Retrieved active pooled instance.</param>
        /// <returns>True when the prefab pool already exists.</returns>
        public bool TryGet(TBehaviour prefab, out TBehaviour subject)
        {
            if (_defaultRuntime != null)
            {
                return _defaultRuntime.TryGet(prefab, out subject);
            }

            subject = null;
            return false;
        }

        /// <summary>
        /// Attempts to get an instance from an already-created identified pool
        /// in the default runtime.
        /// </summary>
        /// <param name="identifier">Configured pool identifier.</param>
        /// <param name="subject">Retrieved active pooled instance.</param>
        /// <returns>True when the identified pool already exists.</returns>
        public bool TryGet(PoolIdentifier identifier, out TBehaviour subject)
        {
            if (_defaultRuntime != null)
            {
                return _defaultRuntime.TryGet(identifier, out subject);
            }

            subject = null;
            return false;
        }

        /// <summary>
        /// Returns one subject to its owning subpool in the default runtime.
        /// </summary>
        /// <param name="subject">Subject to return.</param>
        /// <returns>True when the subject belongs to the default runtime.</returns>
        public bool Release(TBehaviour subject)
        {
            return _defaultRuntime != null && _defaultRuntime.TryRelease(subject);
        }

        /// <summary>
        /// Gets whether a prefab-keyed subpool is currently active in the
        /// default runtime.
        /// </summary>
        /// <param name="prefab">Prefab used to resolve the subpool.</param>
        /// <returns>True when the subpool is active.</returns>
        public bool HasPool(TBehaviour prefab)
        {
            return _defaultRuntime != null && _defaultRuntime.HasPool(prefab);
        }

        /// <summary>
        /// Gets whether an identified subpool is currently active in the
        /// default runtime.
        /// </summary>
        /// <param name="identifier">Pool identifier.</param>
        /// <returns>True when the subpool is active.</returns>
        public bool HasPool(PoolIdentifier identifier)
        {
            return _defaultRuntime != null && _defaultRuntime.HasPool(identifier);
        }

        /// <summary>
        /// Attempts to capture runtime counters for one active prefab-keyed
        /// subpool in the default runtime.
        /// </summary>
        /// <param name="prefab">Prefab used to resolve the subpool.</param>
        /// <param name="statistics">Captured statistics snapshot.</param>
        /// <returns>True when the subpool is active.</returns>
        public bool TryGetStatistics(
            TBehaviour prefab,
            out PoolStatistics statistics
        )
        {
            if (_defaultRuntime != null)
            {
                return _defaultRuntime.TryGetStatistics(prefab, out statistics);
            }

            statistics = default;
            return false;
        }

        /// <summary>
        /// Attempts to capture runtime counters for one active identified
        /// subpool in the default runtime.
        /// </summary>
        /// <param name="identifier">Pool identifier.</param>
        /// <param name="statistics">Captured statistics snapshot.</param>
        /// <returns>True when the subpool is active.</returns>
        public bool TryGetStatistics(
            PoolIdentifier identifier,
            out PoolStatistics statistics
        )
        {
            if (_defaultRuntime != null)
            {
                return _defaultRuntime.TryGetStatistics(identifier, out statistics);
            }

            statistics = default;
            return false;
        }

        #endregion

        #region Helpers

        private HandyPoolRuntime<TBehaviour> EnsureDefaultRuntime()
        {
            _defaultRuntime ??= CreateRuntime();
            return _defaultRuntime;
        }

        private List<PoolRuntimeDefinition<TBehaviour>> BuildRuntimeDefinitions()
        {
            List<PoolRuntimeDefinition<TBehaviour>> definitions = new(
                _entries != null ? _entries.Count : 0
            );

            if (_entries == null)
            {
                return definitions;
            }

            for (int index = 0; index < _entries.Count; index++)
            {
                PoolEntryConfiguration entry = _entries[index];
                if (entry == null || !entry.IsValid)
                {
                    continue;
                }

                definitions.Add(entry.ToRuntimeDefinition(_defaultSize, _maxSize));
            }

            return definitions;
        }

        [Serializable]
        private sealed class PoolEntryConfiguration
        {
            [SerializeField]
            private TBehaviour _prefab;

            [SerializeField]
            private string _identifier;

            [SerializeField]
            [Min(-1)]
            private int _initialCapacity = -1;

            [SerializeField]
            [Min(-1)]
            private int _maxSize = -1;

            [SerializeField]
            [Min(0)]
            private int _prewarmCount;

            [SerializeField]
            private bool _collectionCheck = true;

            public TBehaviour Prefab => _prefab;

            public bool IsValid => _prefab != null;

            public PoolRuntimeDefinition<TBehaviour> ToRuntimeDefinition(
                int defaultSize,
                int defaultMaxSize
            )
            {
                int initialCapacity = _initialCapacity >= 0
                    ? _initialCapacity
                    : defaultSize;
                int maxSize = _maxSize > 0 ? _maxSize : defaultMaxSize;
                maxSize = Mathf.Max(1, maxSize);
                initialCapacity = Mathf.Clamp(initialCapacity, 0, maxSize);

                bool hasIdentifier = TryGetIdentifier(out PoolIdentifier identifier);
                return new PoolRuntimeDefinition<TBehaviour>(
                    _prefab,
                    identifier,
                    hasIdentifier,
                    initialCapacity,
                    maxSize,
                    _prewarmCount,
                    _collectionCheck
                );
            }

            private bool TryGetIdentifier(out PoolIdentifier identifier)
            {
                if (string.IsNullOrWhiteSpace(_identifier))
                {
                    identifier = default;
                    return false;
                }

                identifier = new PoolIdentifier(_identifier);
                return true;
            }
        }

        #endregion
    }
}