using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Creates and registers the global command service.
    /// </summary>
    public static class CommandServiceBootstrapper
    {
        #region Public API

        /// <summary>
        /// Creates the runtime command service and registers it into the
        /// global service locator.
        /// </summary>
        public static void Bootstrap()
        {
            CommandService.ResetStaticState();

            if (ServiceLocator.TryGet<ICommandService>(
                out ICommandService existingService)
                && existingService != null)
            {
                return;
            }

            GameObject serviceObject = new("CommandService");
            CommandService service = serviceObject.AddComponent<CommandService>();

            ServiceLocator.Register<ICommandService>(service);
            ServiceLocator.Register(service);

            Object.DontDestroyOnLoad(serviceObject);
        }

        #endregion
    }
}