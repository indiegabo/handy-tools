using UnityEngine;

namespace IndieGabo.HandyTools.PoolingModule
{
    /// <summary>
    /// Exposes the runtime contract of one active subpool.
    /// </summary>
    /// <typeparam name="TBehaviour">Pooled behaviour type.</typeparam>
    public interface IHandyPool<TBehaviour> where TBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Gets the stable pool identifier when one is registered.
        /// </summary>
        PoolIdentifier Identifier { get; }

        /// <summary>
        /// Gets the prefab used to instantiate pooled subjects.
        /// </summary>
        TBehaviour Prefab { get; }

        /// <summary>
        /// Gets a value indicating whether the subpool is active.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Retrieves an active instance from the subpool.
        /// </summary>
        /// <returns>Active pooled subject.</returns>
        TBehaviour Get();

        /// <summary>
        /// Attempts to retrieve an instance from an already-created subpool.
        /// </summary>
        /// <param name="subject">Retrieved active subject.</param>
        /// <returns>True when the subpool is already active.</returns>
        bool TryGet(out TBehaviour subject);

        /// <summary>
        /// Returns a subject to the subpool.
        /// </summary>
        /// <param name="subject">Subject to return.</param>
        void Release(TBehaviour subject);

        /// <summary>
        /// Attempts to return a subject to the subpool.
        /// </summary>
        /// <param name="subject">Subject to return.</param>
        /// <returns>True when the subject belongs to this subpool.</returns>
        bool TryRelease(TBehaviour subject);

        /// <summary>
        /// Captures the current runtime counters for the subpool.
        /// </summary>
        /// <returns>Current subpool statistics snapshot.</returns>
        PoolStatistics GetStatistics();
    }
}