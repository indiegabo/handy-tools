using IndieGabo.HandyTools.Modules;
using UnityEngine;

namespace IndieGabo.HandyTools.Core
{
    /// <summary>
    /// Coordinates all runtime bootstrappers that must run before the first
    /// scene loads so their dependency order stays explicit and stable.
    /// </summary>
    public static class RuntimeBootstrapCoordinator
    {
        #region State

        private static bool _bootstrapped;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Resets the coordinator state before a new play session starts.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _bootstrapped = false;
            HandyModuleRuntimeLoader.ResetState();
        }

        /// <summary>
        /// Detects active modules before any splash screen work starts.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void PrepareModules()
        {
            HandyModuleRuntimeLoader.PrepareActiveModules();
        }

        /// <summary>
        /// Executes the runtime bootstrap sequence in a deterministic order.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Bootstrap()
        {
            if (_bootstrapped)
            {
                return;
            }

            _bootstrapped = true;
            HandyModuleRuntimeLoader.BootstrapActiveModules();
        }

        #endregion
    }
}