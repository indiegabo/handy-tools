using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Services;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using IndieGabo.HandyTools.Modules;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule
{
    public sealed class CutscenesModuleBootstrapper : IHandyModuleBootstrapper
    {
        public HandyModuleDescriptor Descriptor => CutscenesModuleDefinition.Descriptor;

        public IReadOnlyList<HandyModuleDependencyStatus> Dependencies =>
            CutscenesModuleDefinition.Dependencies;

        public void Bootstrap()
        {
            if (ServiceLocator.TryGet<ICutsceneService>(out ICutsceneService existingService)
                && existingService != null)
            {
                return;
            }

            GameObject serviceObject = new("CutsceneService");
            CutsceneService service = serviceObject.AddComponent<CutsceneService>();

            ServiceLocator.Register<ICutsceneService>(service);
            ServiceLocator.Register(service);

            Object.DontDestroyOnLoad(serviceObject);
        }
    }
}