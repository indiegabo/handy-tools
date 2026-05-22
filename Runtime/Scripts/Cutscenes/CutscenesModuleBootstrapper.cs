using System.Collections.Generic;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.Services;
using IndieGabo.HandyTools.CutscenesModule.Triggers;
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
            CutsceneGraphFamily.Register();

            if (ServiceLocator.TryGet<ICutsceneService>(out ICutsceneService existingService)
                && existingService != null)
            {
                return;
            }

            bool preserveRuntimeState = HasActiveRun();
            CutsceneService service = ResolveOrCreateService(!preserveRuntimeState);

            if (!preserveRuntimeState)
            {
                ResetPersistedRuntimeState();
            }

            ServiceLocator.Register<ICutsceneService>(service);
            ServiceLocator.Register(service);

            if (service != null && Application.isPlaying)
            {
                Object.DontDestroyOnLoad(service.gameObject);
            }
        }

        private static bool HasActiveRun()
        {
            foreach (CutsceneDirector director in Object.FindObjectsByType<CutsceneDirector>(
                FindObjectsInactive.Include))
            {
                if (director != null && director.TryGetActiveRun(out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ResetPersistedRuntimeState()
        {
            foreach (CutsceneDirector director in Object.FindObjectsByType<CutsceneDirector>(
                FindObjectsInactive.Include))
            {
                director?.ResetRuntimeState();
            }

            foreach (CutsceneTrigger trigger in Object.FindObjectsByType<CutsceneTrigger>(
                FindObjectsInactive.Include))
            {
                trigger?.ResetRuntimeState();
            }
        }

        private static CutsceneService ResolveOrCreateService(bool resetRuntimeState)
        {
            CutsceneService primaryService = null;

            foreach (CutsceneService candidate in Object.FindObjectsByType<CutsceneService>(
                FindObjectsInactive.Include))
            {
                if (candidate == null)
                {
                    continue;
                }

                if (primaryService == null)
                {
                    primaryService = candidate;
                    continue;
                }

                DestroyServiceObject(candidate.gameObject);
            }

            if (primaryService == null)
            {
                GameObject serviceObject = new("CutsceneService");
                primaryService = serviceObject.AddComponent<CutsceneService>();
            }

            if (resetRuntimeState)
            {
                primaryService.ResetRuntimeState();
            }

            return primaryService;
        }

        private static void DestroyServiceObject(GameObject serviceObject)
        {
            if (serviceObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(serviceObject);
                return;
            }

            Object.DestroyImmediate(serviceObject);
        }
    }
}