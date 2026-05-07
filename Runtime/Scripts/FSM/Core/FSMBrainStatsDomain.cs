using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Resolves default stats assets and runtime overrides for one FSM brain.
    /// </summary>
    public sealed class FSMBrainStatsDomain
    {
        #region Fields

        private readonly FSMBrain _brain;
        private readonly Dictionary<Type, FSMStatsAsset> _runtimeOverrides = new();

        private FSMStatsRegistry _registry;

        #endregion

        #region Events

        /// <summary>
        /// Raised whenever the active stats asset for one type changes.
        /// </summary>
        public event Action<Type, FSMStatsAsset> StatsChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the stats domain delegated by one FSM brain.
        /// </summary>
        /// <param name="brain">The brain that owns the domain.</param>
        public FSMBrainStatsDomain(FSMBrain brain)
        {
            _brain = brain;
        }

        #endregion

        #region Retrieval

        /// <summary>
        /// Retrieves the active stats asset for the provided generic type.
        /// </summary>
        /// <typeparam name="T">The stats asset type to resolve.</typeparam>
        /// <returns>The active stats asset, or null when missing.</returns>
        public T Get<T>() where T : FSMStatsAsset
        {
            return TryGet(out T stats) ? stats : null;
        }

        /// <summary>
        /// Retrieves the active stats asset for the provided runtime type.
        /// </summary>
        /// <param name="statsType">The stats asset type to resolve.</param>
        /// <returns>The active stats asset, or null when missing.</returns>
        public FSMStatsAsset Get(Type statsType)
        {
            return TryGet(statsType, out FSMStatsAsset stats) ? stats : null;
        }

        /// <summary>
        /// Tries to retrieve the active stats asset for the provided generic type.
        /// </summary>
        /// <typeparam name="T">The stats asset type to resolve.</typeparam>
        /// <param name="stats">The active stats asset when found.</param>
        /// <returns>True when an active stats asset is available.</returns>
        public bool TryGet<T>(out T stats) where T : FSMStatsAsset
        {
            if (TryGet(typeof(T), out FSMStatsAsset resolvedStats))
            {
                stats = resolvedStats as T;
                return stats != null;
            }

            stats = null;
            return false;
        }

        /// <summary>
        /// Tries to retrieve the active stats asset for the provided runtime type.
        /// </summary>
        /// <param name="statsType">The stats asset type to resolve.</param>
        /// <param name="stats">The active stats asset when found.</param>
        /// <returns>True when an active stats asset is available.</returns>
        public bool TryGet(Type statsType, out FSMStatsAsset stats)
        {
            ValidateStatsType(statsType);

            if (_runtimeOverrides.TryGetValue(statsType, out stats))
            {
                return stats != null;
            }

            FSMStatsRegistry registry = ResolveRegistry();

            if (registry != null && registry.TryGetDefault(statsType, out stats))
            {
                return true;
            }

            stats = null;
            return false;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Replaces the active stats asset for the provided generic type.
        /// </summary>
        /// <typeparam name="T">The stats asset type to override.</typeparam>
        /// <param name="stats">The replacement stats asset.</param>
        public void SetOverride<T>(T stats) where T : FSMStatsAsset
        {
            SetOverride(typeof(T), stats);
        }

        /// <summary>
        /// Replaces the active stats asset using the concrete runtime type of the provided asset.
        /// </summary>
        /// <param name="stats">The replacement stats asset.</param>
        public void SetOverride(FSMStatsAsset stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            SetOverride(stats.GetType(), stats);
        }

        /// <summary>
        /// Replaces the active stats asset for the provided runtime type.
        /// </summary>
        /// <param name="statsType">The stats asset type to override.</param>
        /// <param name="stats">The replacement stats asset.</param>
        public void SetOverride(Type statsType, FSMStatsAsset stats)
        {
            ValidateStatsType(statsType);

            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            if (!statsType.IsInstanceOfType(stats))
            {
                throw new ArgumentException(
                    $"Stats asset of type '{stats.GetType().FullName}' cannot be assigned to '{statsType.FullName}'.",
                    nameof(stats));
            }

            _runtimeOverrides[statsType] = stats;
            NotifyStatsChanged(statsType);
        }

        /// <summary>
        /// Gets whether one override is currently active for the provided generic type.
        /// </summary>
        /// <typeparam name="T">The stats asset type to inspect.</typeparam>
        /// <returns>True when an override is active.</returns>
        public bool HasOverride<T>() where T : FSMStatsAsset
        {
            return _runtimeOverrides.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Gets whether one override is currently active for the provided runtime type.
        /// </summary>
        /// <param name="statsType">The stats asset type to inspect.</param>
        /// <returns>True when an override is active.</returns>
        public bool HasOverride(Type statsType)
        {
            ValidateStatsType(statsType);
            return _runtimeOverrides.ContainsKey(statsType);
        }

        /// <summary>
        /// Removes the active override for the provided generic type.
        /// </summary>
        /// <typeparam name="T">The stats asset type to reset.</typeparam>
        public void ClearOverride<T>() where T : FSMStatsAsset
        {
            ClearOverride(typeof(T));
        }

        /// <summary>
        /// Removes the active override for the provided runtime type.
        /// </summary>
        /// <param name="statsType">The stats asset type to reset.</param>
        public void ClearOverride(Type statsType)
        {
            ValidateStatsType(statsType);

            if (!_runtimeOverrides.Remove(statsType))
            {
                return;
            }

            NotifyStatsChanged(statsType);
        }

        /// <summary>
        /// Removes every runtime stats override owned by this brain.
        /// </summary>
        public void ClearAllOverrides()
        {
            if (_runtimeOverrides.Count == 0)
            {
                return;
            }

            List<Type> changedTypes = new(_runtimeOverrides.Keys);
            _runtimeOverrides.Clear();

            for (int index = 0; index < changedTypes.Count; index++)
            {
                NotifyStatsChanged(changedTypes[index]);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Resolves the first stats registry found under the brain branch.
        /// </summary>
        /// <returns>The resolved stats registry, or null when missing.</returns>
        private FSMStatsRegistry ResolveRegistry()
        {
            if (_registry != null)
            {
                return _registry;
            }

            if (_brain == null)
            {
                return null;
            }

            FSMStatsRegistry[] registries =
                _brain.GetComponentsInChildren<FSMStatsRegistry>(true);

            if (registries.Length == 0)
            {
                return null;
            }

            if (registries.Length > 1)
            {
                Debug.LogWarning(
                    "Multiple FSMStatsRegistry components were found under the same FSMBrain. "
                    + "Brain.Stats will use the first registry in the hierarchy.",
                    _brain);
            }

            _registry = registries[0];
            return _registry;
        }

        /// <summary>
        /// Raises the active-stats-changed event for one stats type.
        /// </summary>
        /// <param name="statsType">The stats type whose active asset changed.</param>
        private void NotifyStatsChanged(Type statsType)
        {
            StatsChanged?.Invoke(statsType, Get(statsType));
        }

        /// <summary>
        /// Validates that the provided type can be used as a stats asset key.
        /// </summary>
        /// <param name="statsType">The type to validate.</param>
        private static void ValidateStatsType(Type statsType)
        {
            if (statsType == null)
            {
                throw new ArgumentNullException(nameof(statsType));
            }

            if (!typeof(FSMStatsAsset).IsAssignableFrom(statsType))
            {
                throw new ArgumentException(
                    $"Type '{statsType.FullName}' does not derive from {nameof(FSMStatsAsset)}.",
                    nameof(statsType));
            }
        }

        #endregion
    }
}