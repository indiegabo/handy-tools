
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{
    public static class Coroutines
    {
        #region Coroutines

        private static Dictionary<float, WaitForSeconds> _forSecondsWaiters = new();

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