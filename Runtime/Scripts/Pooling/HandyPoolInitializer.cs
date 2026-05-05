using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.Pooling
{
    /// <summary>
    /// Connects a MonoBehaviour lifecycle to a HandyPool so the pool can be
    /// initialized and dismissed from one host component.
    /// </summary>
    /// <typeparam name="TBehaviour">
    /// Pool subject behaviour managed by the initializer.
    /// </typeparam>
    public abstract class HandyPoolInitializer<TBehaviour> : HandyBehaviour, IPoolInitializer
        where TBehaviour : MonoBehaviour, IPoolSubject<TBehaviour>
    {
        #region Inspector

        [BoxGroup("Pooling")]
        [SerializeField]
        private float _initialAmount = 0;

        [BoxGroup("Pooling")]
        [SerializeField]
        private bool _initializeOnEnable = true;

        [BoxGroup("Pooling")]
        [SerializeField]
        private Transform _container;

        [BoxGroup("Pooling")]
        [SerializeField]
        private HandyPool<TBehaviour> _pool;

        #endregion

        #region Fields

        private HandyPoolRuntime<TBehaviour> _runtime;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the runtime instance owned by this initializer.
        /// </summary>
        public HandyPoolRuntime<TBehaviour> Runtime => _runtime;

        #endregion

        #region Behaviour

        /// <summary>
        /// Initializes the pool automatically when enabled if configured to do
        /// so.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (!_initializeOnEnable) return;
            InitializePool();
        }

        /// <summary>
        /// Dismisses the pool automatically when disabled if configured to do
        /// so.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (!_initializeOnEnable) return;
            DismissPool();
        }

        #endregion

        #region Initializing

        /// <summary>
        /// Initializes the configured pool and prewarms it when requested.
        /// </summary>
        public void InitializePool()
        {
            if (_pool == null)
            {
                return;
            }

            _runtime ??= _pool.CreateRuntime();
            _runtime.Initialize(_container, _initialAmount);
        }

        /// <summary>
        /// Releases every pooled instance and clears the configured pool.
        /// </summary>
        public void DismissPool()
        {
            _runtime?.Dismiss();
        }

        #endregion
    }
}