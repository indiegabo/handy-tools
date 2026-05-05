using IndieGabo.HandyTools.HandyServiceLocator;
using UnityEngine;

namespace IndieGabo.HandyTools.Gameplay
{
    /// <summary>
    /// Creates and registers the global gameplay service before runtime logic
    /// starts consuming it.
    /// </summary>
    public static class GameplayServiceBootstrapper
    {
        /// <summary>
        /// Creates the GameplayService root object and registers its services
        /// into the global service locator.
        /// </summary>
        public static void Bootstrap()
        {
            if (ServiceLocator.TryGet(out GameplayService existingGameplayService))
            {
                _ = existingGameplayService;
                return;
            }

            GameObject go = new("GameplayService");
            GameplayService gameplayService = go.AddComponent<GameplayService>();
            ServiceLocator.Register(gameplayService);

            go.AddComponent<GameplayTimeRegisterer>();

            Object.DontDestroyOnLoad(go);
        }
    }
}