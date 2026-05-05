
using System;
using System.Collections.Generic;
using System.Threading;

namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Retries synchronous operations with a fixed interval between attempts.
    /// </summary>
    public static class Retry
    {
        /// <summary>
        /// Executes one action until it succeeds or the attempt limit is reached.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <param name="retryInterval">Delay applied between attempts.</param>
        /// <param name="maxAttemptCount">Maximum number of attempts.</param>
        public static void Do(Action action, TimeSpan retryInterval, int maxAttemptCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        /// <summary>
        /// Executes one function until it succeeds or the attempt limit is reached.
        /// </summary>
        /// <typeparam name="T">Return type of the function.</typeparam>
        /// <param name="action">Function to execute.</param>
        /// <param name="retryInterval">Delay applied between attempts.</param>
        /// <param name="maxAttemptCount">Maximum number of attempts.</param>
        /// <returns>The value returned by the first successful attempt.</returns>
        public static T Do<T>(Func<T> action, TimeSpan retryInterval, int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }

                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }

    }
}