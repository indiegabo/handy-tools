using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace IndieGabo.HandyTools.Pooling
{
    /// <summary>
    /// Base ScriptableObject pool that manages one ObjectPool per prefab.
    /// </summary>
    public abstract class HandyPool<TBehaviour> : ScriptableObject where TBehaviour : MonoBehaviour, IPoolSubject<ObjectPool<TBehaviour>>
    {
        #region Inspector

        [Header("HandyTools")]
        [SerializeField]
        [Min(0)]
        private int _defaultSize;

        [SerializeField]
        [Min(1)]
        private int _maxSize = 10000;

        [SerializeField]
        private List<TBehaviour> _prefabs = new();

        [Header("Events")]
        [SerializeField]
        private UnityEvent _initialized;

        [SerializeField]
        private UnityEvent _dismissed;

        #endregion

        #region Fields

        private Dictionary<TBehaviour, ObjectPool<TBehaviour>> _pools;
        private HashSet<TBehaviour> _createdSubjects;
        private List<TBehaviour> _prewarmedSubjects;
        private Transform _container;
        private bool _isAvailable;

        #endregion

        #region Getters

        public bool IsAvailable => _isAvailable;

        #endregion

        #region Initializing

        /// <summary>
        /// Creates all declared pools and optionally prewarms them.
        /// </summary>
        /// <param name="container">Optional parent for instantiated subjects.</param>
        /// <param name="initialAmount">
        /// Prewarm amount per prefab. Fractional values are rounded up.
        /// </param>
        public void Initialize(Transform container = null, float initialAmount = 0)
        {
            if (_isAvailable)
            {
                Dismiss();
            }

            EnsureRuntimeState();
            _container = container;
            _pools.Clear();
            _createdSubjects.Clear();
            _prewarmedSubjects.Clear();
            _prefabs.ForEach(prefab => RequestPoolCreation(prefab, initialAmount));
            _isAvailable = true;
            _initialized?.Invoke();
        }

        /// <summary>
        /// Clears all created pools and destroys pooled instances.
        /// </summary>
        public void Dismiss()
        {
            EnsureRuntimeState();

            foreach (ObjectPool<TBehaviour> pool in _pools.Values)
            {
                pool.Clear();
            }

            foreach (TBehaviour subject in _createdSubjects)
            {
                if (subject != null)
                {
                    Destroy(subject.gameObject);
                }
            }

            _pools.Clear();
            _createdSubjects.Clear();
            _prewarmedSubjects.Clear();
            _container = null;
            _isAvailable = false;
            _dismissed?.Invoke();
        }

        #endregion

        #region Creating

        /// <summary>
        /// Creates a pool for the provided prefab when missing and optionally
        /// prewarms a number of instances.
        /// </summary>
        /// <param name="prefab">Prefab used to create pooled instances.</param>
        /// <param name="initialAmount">
        /// Prewarm amount for the new pool. Fractional values are rounded up.
        /// </param>
        public void RequestPoolCreation(TBehaviour prefab, float initialAmount = 0)
        {
            EnsureRuntimeState();

            if (prefab == null) return;
            if (_pools.ContainsKey(prefab)) return;

            ObjectPool<TBehaviour> pool = new ObjectPool<TBehaviour>(
                () => Create(prefab),
                OnTakenFromPool,
                OnReturnedToPool,
                OnDestroyInPool,
                true,
                _defaultSize,
                _maxSize
            );

            _pools.Add(prefab, pool);

            int prewarmCount = Mathf.Max(0, Mathf.CeilToInt(initialAmount));
            if (prewarmCount <= 0)
            {
                return;
            }

            _prewarmedSubjects.Clear();
            for (int index = 0; index < prewarmCount; index++)
            {
                _prewarmedSubjects.Add(pool.Get());
            }

            for (int index = 0; index < _prewarmedSubjects.Count; index++)
            {
                _prewarmedSubjects[index].ReleaseToPool();
            }

            _prewarmedSubjects.Clear();
        }

        #endregion

        #region Pooling Callbacks

        private TBehaviour Create(TBehaviour prefab)
        {
            TBehaviour subject = Instantiate(prefab, _container);
            subject.gameObject.SetActive(false);
            subject.SetPool(_pools[prefab]);
            _createdSubjects.Add(subject);
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
            Destroy(subject.gameObject);
        }

        #endregion

        #region Getting

        /// <summary>
        /// Gets an instance from the pool for the provided prefab.
        /// </summary>
        /// <param name="prefab">Prefab pool to use.</param>
        /// <returns>An active pooled instance.</returns>
        public TBehaviour Get(TBehaviour prefab)
        {
            EnsureRuntimeState();
            if (prefab == null) return null;

            if (!_pools.TryGetValue(prefab, out ObjectPool<TBehaviour> pool))
            {
                RequestPoolCreation(prefab);
                pool = _pools[prefab];
            }

            return pool.Get();
        }

        /// <summary>
        /// Attempts to get an instance from an already-created prefab pool.
        /// </summary>
        /// <param name="prefab">Prefab pool to query.</param>
        /// <param name="subject">Retrieved active pooled instance.</param>
        /// <returns>True when the prefab pool already exists.</returns>
        public bool TryGet(TBehaviour prefab, out TBehaviour subject)
        {
            subject = null;
            EnsureRuntimeState();

            if (prefab == null) return false;

            if (!_pools.TryGetValue(prefab, out ObjectPool<TBehaviour> pool)) return false;

            subject = pool.Get();

            return true;
        }

        private void EnsureRuntimeState()
        {
            _pools ??= new Dictionary<TBehaviour, ObjectPool<TBehaviour>>(
                _prefabs != null ? _prefabs.Count : 0
            );
            _createdSubjects ??= new HashSet<TBehaviour>();
            _prewarmedSubjects ??= new List<TBehaviour>();
        }

        #endregion
    }
}