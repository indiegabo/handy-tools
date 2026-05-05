using IndieGabo.HandyTools.HandyServiceLocator;
using UnityEngine;

namespace IndieGabo.HandyTools.SaveSystem
{
    /// <summary>
    /// Creates the runtime SaveSystem services when automatic bootstrapping is
    /// enabled in the SaveSystem configuration.
    /// </summary>
    public static class SaveSystemBootstrapper
    {
        /// <summary>
        /// Creates and registers the SaveSystem runtime services.
        /// </summary>
        public static void Bootstrap()
        {
            if (!SaveSystemModuleDefinition.IsActive) return;
            if (!SaveSystemConfig.Instance.ShouldAutoBoot) return;

            var go = new GameObject("SaveSystem");
            var slotManager = go.AddComponent<SlotManager>();
            var lodedSloteService = go.AddComponent<LoadedSlotService>();

            Object.DontDestroyOnLoad(go);

            ServiceLocator.Register(slotManager);
            ServiceLocator.Register(lodedSloteService);

            if (SaveSystemConfig.Instance.EnsureIndexedSlots)
            {
                slotManager.EnsureIndexedSlots();
            }
        }
    }
}