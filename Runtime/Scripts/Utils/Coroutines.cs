
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Provides cached coroutine yield instructions used across the package.
    /// </summary>
    public static class Coroutines
    {
        #region Coroutines

        private static Dictionary<float, WaitForSeconds> _forSecondsWaiters = new();

        /// <summary>
        /// Gets one cached WaitForSeconds instance for the requested duration.
        /// </summary>
        /// <param name="seconds">Requested wait duration.</param>
        /// <returns>A cached yield instruction for that duration.</returns>
        public static WaitForSeconds GetWaitForSeconds(float seconds)
        {
            if (_forSecondsWaiters.TryGetValue(seconds, out WaitForSeconds waitForSeconds)) return waitForSeconds;

            WaitForSeconds newWaitForSeconds = new(seconds);
            _forSecondsWaiters.Add(seconds, newWaitForSeconds);
            return newWaitForSeconds;
        }

        #endregion
    }
}