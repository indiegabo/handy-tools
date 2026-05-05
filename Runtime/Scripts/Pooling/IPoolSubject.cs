using UnityEngine;

namespace IndieGabo.HandyTools.Pooling
{
    /// <summary>
    /// Defines the lifecycle contract for objects managed by one pool.
    /// </summary>
    /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
    public interface IPoolSubject<TBehaviour> where TBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Stores the pool instance that owns the subject.
        /// </summary>
        /// <param name="pool">Owning pool instance.</param>
        void SetPool(IHandyPool<TBehaviour> pool);

        /// <summary>
        /// Invoked after the subject is retrieved from the pool.
        /// </summary>
        void OnTakenFromPool();

        /// <summary>
        /// Requests the subject to return itself to the pool.
        /// </summary>
        void ReleaseToPool();

        /// <summary>
        /// Invoked after the subject has been returned to the pool.
        /// </summary>
        void OnReturnedToPool();
    }
}