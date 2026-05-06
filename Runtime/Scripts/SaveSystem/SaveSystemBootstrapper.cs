using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine;

namespace IndieGabo.HandyTools.SaveSystemModule
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

            GameObject go = new("SaveSystem");
            SlotManager slotManager = go.AddComponent<SlotManager>();
            LoadedSlotService loadedSlotService = go.AddComponent<LoadedSlotService>();

            Object.DontDestroyOnLoad(go);

            ServiceLocator.Register(slotManager);
            ServiceLocator.Register(loadedSlotService);

            if (SaveSystemConfig.Instance.EnsureIndexedSlots)
            {
                slotManager.EnsureIndexedSlots();
            }
        }
    }
}