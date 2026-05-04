using UnityEngine;
using UnityEngine.Pool;

namespace IndieGabo.HandyTools.Pooling
{
    public abstract class HandyPoolInitializer<TBehaviour> : HandyBehaviour, IPoolInitializer where TBehaviour : MonoBehaviour, IPoolSubject<ObjectPool<TBehaviour>>
    {
        #region Inspector

        [SerializeField]
        private float _initialAmount = 0;

        [SerializeField]
        private bool _initializeOnEnable = true;

        [SerializeField]
        private Transform _container;

        [SerializeField]
        private HandyPool<TBehaviour> _pool;

        #endregion

        #region Behaviour

        protected virtual void OnEnable()
        {
            if (!_initializeOnEnable) return;
            InitializePool();
        }

        protected virtual void OnDisable()
        {
            if (!_initializeOnEnable) return;
            DismissPool();
        }

        #endregion

        #region Initializing

        public void InitializePool()
        {
            _pool.Initialize(_container, _initialAmount);
        }

        public void DismissPool()
        {
            _pool.Dismiss();
        }

        #endregion
    }
}