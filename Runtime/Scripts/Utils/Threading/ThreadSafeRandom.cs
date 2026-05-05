using System;
using System.Threading;

namespace IndieGabo.HandyTools.Utils.Threading
{
    /// <summary>
    /// Exposes one Random instance per thread to avoid cross-thread contention.
    /// </summary>
    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random _local;

        /// <summary>
        /// Gets the Random instance associated with the current thread.
        /// </summary>
        public static Random ThisThreadsRandom
        {
            get { return _local ?? (_local = new Random(unchecked(System.Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }
}