using System;
using System.Threading;

namespace IndieGabo.HandyTools.Utils.Threading
{
    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(System.Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }
}