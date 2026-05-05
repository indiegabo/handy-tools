using System;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.Pooling
{
    /// <summary>
    /// Provides global lookup for active identified pools and spawn access by
    /// subject type.
    /// </summary>
    public static class PoolRegistry
    {
        private static Dictionary<PoolRegistryKey, object> _pools = new();

        #region Public API

        /// <summary>
        /// Rebuilds the runtime registry for a new play session.
        /// </summary>
        public static void Bootstrap()
        {
            _pools = new Dictionary<PoolRegistryKey, object>();
        }

        /// <summary>
        /// Attempts to retrieve one active identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier.</param>
        /// <param name="pool">Resolved active pool.</param>
        /// <returns>True when a matching pool is registered.</returns>
        public static bool TryGetPool<TBehaviour>(
            PoolIdentifier identifier,
            out IHandyPool<TBehaviour> pool
        ) where TBehaviour : MonoBehaviour
        {
            EnsureState();

            if (!identifier.IsValid)
            {
                pool = null;
                return false;
            }

            if (
                _pools.TryGetValue(
                    new PoolRegistryKey(typeof(TBehaviour), identifier),
                    out object storedPool
                )
                && storedPool is IHandyPool<TBehaviour> typedPool
            )
            {
                pool = typedPool;
                return true;
            }

            pool = null;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve one active identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier string.</param>
        /// <param name="pool">Resolved active pool.</param>
        /// <returns>True when a matching pool is registered.</returns>
        public static bool TryGetPool<TBehaviour>(
            string identifier,
            out IHandyPool<TBehaviour> pool
        ) where TBehaviour : MonoBehaviour
        {
            return TryGetPool((PoolIdentifier)identifier, out pool);
        }

        /// <summary>
        /// Attempts to retrieve one active identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier GUID.</param>
        /// <param name="pool">Resolved active pool.</param>
        /// <returns>True when a matching pool is registered.</returns>
        public static bool TryGetPool<TBehaviour>(
            Guid identifier,
            out IHandyPool<TBehaviour> pool
        ) where TBehaviour : MonoBehaviour
        {
            return TryGetPool((PoolIdentifier)identifier, out pool);
        }

        /// <summary>
        /// Resolves one active identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier.</param>
        /// <returns>Resolved active pool.</returns>
        public static IHandyPool<TBehaviour> GetRequiredPool<TBehaviour>(
            PoolIdentifier identifier
        ) where TBehaviour : MonoBehaviour
        {
            if (TryGetPool(identifier, out IHandyPool<TBehaviour> pool))
            {
                return pool;
            }

            throw new InvalidOperationException(
                $"No active pool of type {typeof(TBehaviour).FullName} is registered with identifier '{identifier}'."
            );
        }

        /// <summary>
        /// Resolves one active identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier string.</param>
        /// <returns>Resolved active pool.</returns>
        public static IHandyPool<TBehaviour> GetRequiredPool<TBehaviour>(
            string identifier
        ) where TBehaviour : MonoBehaviour
        {
            return GetRequiredPool<TBehaviour>((PoolIdentifier)identifier);
        }

        /// <summary>
        /// Resolves one active identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier GUID.</param>
        /// <returns>Resolved active pool.</returns>
        public static IHandyPool<TBehaviour> GetRequiredPool<TBehaviour>(
            Guid identifier
        ) where TBehaviour : MonoBehaviour
        {
            return GetRequiredPool<TBehaviour>((PoolIdentifier)identifier);
        }

        /// <summary>
        /// Attempts to retrieve one active subject from an identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier.</param>
        /// <param name="subject">Resolved active pooled subject.</param>
        /// <returns>True when the identified pool is active.</returns>
        public static bool TryGet<TBehaviour>(
            PoolIdentifier identifier,
            out TBehaviour subject
        ) where TBehaviour : MonoBehaviour
        {
            if (TryGetPool(identifier, out IHandyPool<TBehaviour> pool))
            {
                return pool.TryGet(out subject);
            }

            subject = null;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve one active subject from an identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier string.</param>
        /// <param name="subject">Resolved active pooled subject.</param>
        /// <returns>True when the identified pool is active.</returns>
        public static bool TryGet<TBehaviour>(
            string identifier,
            out TBehaviour subject
        ) where TBehaviour : MonoBehaviour
        {
            return TryGet((PoolIdentifier)identifier, out subject);
        }

        /// <summary>
        /// Attempts to retrieve one active subject from an identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier GUID.</param>
        /// <param name="subject">Resolved active pooled subject.</param>
        /// <returns>True when the identified pool is active.</returns>
        public static bool TryGet<TBehaviour>(
            Guid identifier,
            out TBehaviour subject
        ) where TBehaviour : MonoBehaviour
        {
            return TryGet((PoolIdentifier)identifier, out subject);
        }

        /// <summary>
        /// Resolves one active subject from an identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier.</param>
        /// <returns>Resolved active pooled subject.</returns>
        public static TBehaviour GetRequired<TBehaviour>(PoolIdentifier identifier)
            where TBehaviour : MonoBehaviour
        {
            return GetRequiredPool<TBehaviour>(identifier).Get();
        }

        /// <summary>
        /// Resolves one active subject from an identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier string.</param>
        /// <returns>Resolved active pooled subject.</returns>
        public static TBehaviour GetRequired<TBehaviour>(string identifier)
            where TBehaviour : MonoBehaviour
        {
            return GetRequired<TBehaviour>((PoolIdentifier)identifier);
        }

        /// <summary>
        /// Resolves one active subject from an identified pool.
        /// </summary>
        /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
        /// <param name="identifier">Pool identifier GUID.</param>
        /// <returns>Resolved active pooled subject.</returns>
        public static TBehaviour GetRequired<TBehaviour>(Guid identifier)
            where TBehaviour : MonoBehaviour
        {
            return GetRequired<TBehaviour>((PoolIdentifier)identifier);
        }

        #endregion

        #region Internal API

        internal static void Register<TBehaviour>(
            PoolIdentifier identifier,
            IHandyPool<TBehaviour> pool
        ) where TBehaviour : MonoBehaviour
        {
            EnsureState();
            ValidateIdentifier(identifier);

            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            PoolRegistryKey key = new(typeof(TBehaviour), identifier);
            if (_pools.TryGetValue(key, out object existingPool))
            {
                if (!ReferenceEquals(existingPool, pool))
                {
                    throw new InvalidOperationException(
                        $"Pool identifier '{identifier}' is already registered for type {typeof(TBehaviour).FullName}."
                    );
                }

                return;
            }

            _pools.Add(key, pool);
        }

        internal static void Deregister<TBehaviour>(
            PoolIdentifier identifier,
            IHandyPool<TBehaviour> pool
        ) where TBehaviour : MonoBehaviour
        {
            EnsureState();

            if (!identifier.IsValid)
            {
                return;
            }

            PoolRegistryKey key = new(typeof(TBehaviour), identifier);
            if (
                _pools.TryGetValue(key, out object existingPool)
                && ReferenceEquals(existingPool, pool)
            )
            {
                _pools.Remove(key);
            }
        }

        #endregion

        #region Unity Lifecycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Bootstrap();
        }

        #endregion

        #region Helpers

        private static void EnsureState()
        {
            _pools ??= new Dictionary<PoolRegistryKey, object>();
        }

        private static void ValidateIdentifier(PoolIdentifier identifier)
        {
            if (!identifier.IsValid)
            {
                throw new ArgumentException(
                    "Pool identifier must be valid.",
                    nameof(identifier)
                );
            }
        }

        private readonly struct PoolRegistryKey : IEquatable<PoolRegistryKey>
        {
            private readonly Type _subjectType;
            private readonly PoolIdentifier _identifier;
            private readonly int _hashCode;

            public PoolRegistryKey(Type subjectType, PoolIdentifier identifier)
            {
                _subjectType = subjectType ?? throw new ArgumentNullException(nameof(subjectType));
                _identifier = identifier;
                _hashCode = HashCode.Combine(subjectType, identifier);
            }

            public bool Equals(PoolRegistryKey other)
            {
                return _subjectType == other._subjectType
                    && _identifier == other._identifier;
            }

            public override bool Equals(object obj)
            {
                return obj is PoolRegistryKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }

        #endregion
    }
}