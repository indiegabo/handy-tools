using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    /// <summary>
    /// Stores the default stats assets available to one FSM branch.
    /// </summary>
    [AddComponentMenu("HandyTools/FSM/FSMStatsRegistry")]
    [DisallowMultipleComponent]
    public sealed class FSMStatsRegistry : MonoBehaviour
    {
        #region Inspector

        [SerializeField]
        private List<FSMStatsAsset> _defaultStats = new();

        #endregion

        #region Fields

        private Dictionary<Type, FSMStatsAsset> _defaultsByType;

        #endregion

        #region Retrieval

        /// <summary>
        /// Tries to resolve one default stats asset by generic type.
        /// </summary>
        /// <typeparam name="T">The stats asset type to resolve.</typeparam>
        /// <param name="stats">The resolved stats asset when found.</param>
        /// <returns>True when one matching stats asset is registered.</returns>
        public bool TryGetDefault<T>(out T stats) where T : FSMStatsAsset
        {
            if (TryGetDefault(typeof(T), out FSMStatsAsset resolvedStats))
            {
                stats = resolvedStats as T;
                return stats != null;
            }

            stats = null;
            return false;
        }

        /// <summary>
        /// Tries to resolve one default stats asset by runtime type.
        /// </summary>
        /// <param name="statsType">The requested stats asset type.</param>
        /// <param name="stats">The resolved stats asset when found.</param>
        /// <returns>True when one matching stats asset is registered.</returns>
        public bool TryGetDefault(Type statsType, out FSMStatsAsset stats)
        {
            ValidateStatsType(statsType);
            EnsureCache();
            return _defaultsByType.TryGetValue(statsType, out stats);
        }

        #endregion

        #region Unity Messages

        /// <summary>
        /// Clears the cached lookup after inspector edits.
        /// </summary>
        private void OnValidate()
        {
            _defaultsByType = null;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Builds the runtime lookup dictionary from the serialized asset list.
        /// </summary>
        private void EnsureCache()
        {
            if (_defaultsByType != null)
            {
                return;
            }

            _defaultsByType = new Dictionary<Type, FSMStatsAsset>();

            for (int index = 0; index < _defaultStats.Count; index++)
            {
                FSMStatsAsset statsAsset = _defaultStats[index];

                if (statsAsset == null)
                {
                    continue;
                }

                Type statsType = statsAsset.GetType();

                if (_defaultsByType.ContainsKey(statsType))
                {
                    Debug.LogError(
                        $"FSMStatsRegistry contains multiple stats assets of type '{statsType.FullName}'. "
                        + "Register only one asset per type unless a build-id keyed workflow is introduced.",
                        this);
                    continue;
                }

                _defaultsByType.Add(statsType, statsAsset);
            }
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