using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace IndieGabo.HandyTools.Modules
{
    /// <summary>
    /// Discovers and loads HandyTools modules according to the kernel module
    /// settings.
    /// </summary>
    public static class HandyModuleRuntimeLoader
    {
        private readonly struct MandatoryBootstrapCall
        {
            public MandatoryBootstrapCall(string displayName, string typeName, string methodName)
            {
                DisplayName = displayName;
                TypeName = typeName;
                MethodName = methodName;
            }

            public string DisplayName { get; }
            public string TypeName { get; }
            public string MethodName { get; }
        }

        private static readonly MandatoryBootstrapCall[] _mandatoryBootstrapCalls =
        {
            new(
                "HandyBus",
                "IndieGabo.HandyTools.HandyBus.EventBusUtil",
                "Initialize"
            ),
            new(
                "ServiceLocator",
                "IndieGabo.HandyTools.HandyServiceLocator.ServiceLocator",
                "BootstrapGlobal"
            ),
        };

        private static readonly List<IHandyModuleBootstrapper> _activeBootstrappers = new();
        private static bool _prepared;
        private static bool _bootstrapped;

        /// <summary>
        /// Resets loader state before a new runtime session starts.
        /// </summary>
        public static void ResetState()
        {
            _activeBootstrappers.Clear();
            _prepared = false;
            _bootstrapped = false;
        }

        /// <summary>
        /// Loads mandatory infrastructure and discovers active optional modules.
        /// </summary>
        public static void PrepareActiveModules()
        {
            if (_prepared)
            {
                return;
            }

            _prepared = true;
            BootstrapMandatoryInfrastructure();
            DiscoverActiveBootstrappers();
        }

        /// <summary>
        /// Bootstraps every active optional module before the first scene loads.
        /// </summary>
        public static void BootstrapActiveModules()
        {
            if (_bootstrapped)
            {
                return;
            }

            PrepareActiveModules();
            _bootstrapped = true;

            for (int index = 0; index < _activeBootstrappers.Count; index++)
            {
                _activeBootstrappers[index].Bootstrap();
            }
        }

        private static void BootstrapMandatoryInfrastructure()
        {
            for (int index = 0; index < _mandatoryBootstrapCalls.Length; index++)
            {
                MandatoryBootstrapCall bootstrapCall = _mandatoryBootstrapCalls[index];
                TryInvokeStaticBootstrap(bootstrapCall);
            }
        }

        private static void TryInvokeStaticBootstrap(MandatoryBootstrapCall bootstrapCall)
        {
            Type type = FindType(bootstrapCall.TypeName);
            if (type == null)
            {
                Debug.LogWarning(
                    $"[HandyTools Kernel] Mandatory module {bootstrapCall.DisplayName} was not found."
                );
                return;
            }

            MethodInfo method = type.GetMethod(
                bootstrapCall.MethodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );

            if (method == null)
            {
                Debug.LogWarning(
                    $"[HandyTools Kernel] Mandatory module {bootstrapCall.DisplayName} does not expose {bootstrapCall.MethodName}."
                );
                return;
            }

            method.Invoke(null, null);
        }

        private static void DiscoverActiveBootstrappers()
        {
            _activeBootstrappers.Clear();

            HandyModuleSettings settings = HandyModuleSettings.Instance;
            foreach (IHandyModuleBootstrapper bootstrapper in CreateBootstrappers())
            {
                HandyModuleDescriptor descriptor = bootstrapper.Descriptor;
                if (!settings.IsModuleActive(descriptor))
                {
                    continue;
                }

                if (!AreDependenciesSatisfied(bootstrapper.Dependencies))
                {
                    continue;
                }

                _activeBootstrappers.Add(bootstrapper);
            }

            _activeBootstrappers.Sort(
                (left, right) => left.Descriptor.LoadOrder.CompareTo(right.Descriptor.LoadOrder)
            );
        }

        private static IEnumerable<IHandyModuleBootstrapper> CreateBootstrappers()
        {
            Type contractType = typeof(IHandyModuleBootstrapper);
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Type[] types = GetLoadableTypes(assemblies[assemblyIndex]);
                for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                {
                    Type candidateType = types[typeIndex];
                    if (!contractType.IsAssignableFrom(candidateType) ||
                        candidateType.IsAbstract ||
                        candidateType.IsInterface)
                    {
                        continue;
                    }

                    if (Activator.CreateInstance(candidateType) is IHandyModuleBootstrapper bootstrapper)
                    {
                        yield return bootstrapper;
                    }
                }
            }
        }

        private static bool AreDependenciesSatisfied(
            IReadOnlyList<HandyModuleDependencyStatus> dependencies
        )
        {
            if (dependencies == null)
            {
                return true;
            }

            for (int index = 0; index < dependencies.Count; index++)
            {
                if (!dependencies[index].IsSatisfied)
                {
                    return false;
                }
            }

            return true;
        }

        private static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Type type = assemblies[index].GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
        }
    }
}