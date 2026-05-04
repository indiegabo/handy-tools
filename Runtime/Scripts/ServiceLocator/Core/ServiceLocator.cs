using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.HandyServiceLocator
{
    public class ServiceLocator : MonoBehaviour
    {
        const string k_GlobalServiceLocatorName = "ServiceLocator [Global]";
        const string k_SceneServiceLocatorName = "ServiceLocator [Scene]";

        static ServiceLocator _global;
        static Dictionary<Scene, ServiceLocator> _scenesContainer = new();
        static List<GameObject> _tmpSceneGameObjects = new();

        readonly ServiceManager _services = new();

        internal void ConfigureAsScene()
        {
            Scene scene = gameObject.scene;

            if (_scenesContainer.ContainsKey(scene))
            {
                Debug.LogError("<color=#FFFFFF>[Handy Service Locator]</color> ServiceLocator already configured as scene");
                return;
            }

            _scenesContainer.Add(scene, this);
        }

        public static ServiceLocator Global => _global;

        /// <summary>
        /// Gets the <see cref="ServiceLocator"/> which is responsible for the scene that <paramref name="monoBehaviour"/> is in.
        /// If none is found, it tries to find one in the scene by looking for a
        /// <see cref="ServiceLocatorSceneBootstrapper"/> and calling <see cref="ServiceLocatorSceneBootstrapper.BootstrapOnDemand"/>.
        /// If still none is found, it returns the global <see cref="ServiceLocator"/>.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour to search from.</param>
        /// <returns>The <see cref="ServiceLocator"/> responsible for the scene, or the global one if none is found.</returns>
        public static ServiceLocator For(MonoBehaviour monoBehaviour)
        {
            var componentInParent = monoBehaviour.GetComponentInParent<ServiceLocator>();
            if (componentInParent != null)
            {
                return componentInParent;
            }

            var inTheScene = ForSceneOf(monoBehaviour);
            if (inTheScene != null)
            {
                return inTheScene;
            }

            return _global;
        }

        /// <summary>
        /// Gets the <see cref="ServiceLocator"/> which is responsible for the scene that <paramref name="monoBehaviour"/> is in.
        /// If none is found, it tries to find one in the scene by looking for a
        /// <see cref="ServiceLocatorSceneBootstrapper"/> and calling <see cref="ServiceLocatorSceneBootstrapper.BootstrapOnDemand"/>.
        /// If still none is found, it returns the global <see cref="ServiceLocator"/>.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour to search from.</param>
        /// <returns>The <see cref="ServiceLocator"/> responsible for the scene, or the global one if none is found.</returns>
        public static ServiceLocator ForSceneOf(MonoBehaviour monoBehaviour)
        {
            Scene scene = monoBehaviour.gameObject.scene;

            if (_scenesContainer.TryGetValue(scene, out ServiceLocator container) && container != monoBehaviour)
            {
                return container;
            }

            _tmpSceneGameObjects.Clear();

            scene.GetRootGameObjects(_tmpSceneGameObjects);

            foreach (GameObject go in _tmpSceneGameObjects.Where(go => go.GetComponent<ServiceLocator>() != null))
            {
                if (go.TryGetComponent(out ServiceLocatorSceneBootstrapper bootstrapper) && bootstrapper.Container != monoBehaviour)
                {
                    bootstrapper.BootstrapOnDemand();
                    return bootstrapper.Container;
                }
            }

            return _global;
        }

        /// <summary>
        /// Registers a service with the given instance and type in the current scope.
        /// </summary>
        /// <typeparam name="T">The type of the service to register</typeparam>
        /// <param name="service">The instance of the service to register</param>
        /// <returns>The current ServiceLocator, for chaining</returns>
        public ServiceLocator Register<T>(T service) where T : class
        {
            _services.Register(service);
            return this;
        }

        /// <summary>
        /// Registers a service with the given instance and type in the current scope and any parent scopes.
        /// </summary>
        /// <param name="type">The type of the service to register</param>
        /// <param name="service">The instance of the service to register</param>
        public ServiceLocator Register(Type type, object service)
        {
            _services.Register(type, service);
            return this;
        }

        /// <summary>
        /// Registers a service with the given instance and name in the current scope and any parent scopes.
        /// </summary>
        /// <param name="name">The name of the service to register</param>
        /// <param name="service">The instance of the service to register</param>
        /// <typeparam name="T">The type of the service to register</typeparam>
        public ServiceLocator Register<T>(string name, T service) where T : class
        {
            _services.Register(name, service);
            return this;
        }

        /// <summary>
        /// Deregisters a service with the given instance from the current scope and any parent scopes.
        /// </summary>
        /// <param name="service">The instance of the service to deregister</param>
        /// <returns>This ServiceLocator instance</returns>
        public ServiceLocator Deregister<T>(T service) where T : class
        {
            _services.Deregister(service);
            return this;
        }

        /// <summary>
        /// Deregisters a service with the given type from the current scope and any parent scopes.
        /// </summary>
        /// <param name="type">The type of the service to deregister</param>
        public ServiceLocator Deregister(Type type)
        {
            _services.Deregister(type);
            return this;
        }

        /// <summary>
        /// Deregisters a service with the given name from the current scope and any parent scopes.
        /// </summary>
        /// <param name="name">The name of the service to deregister</param>
        /// <returns>This ServiceLocator instance</returns>
        public ServiceLocator Deregister(string name)
        {
            _services.Deregister(name);
            return this;
        }

        /// <summary>
        /// Gets a service of type T from the current scope and any parent scopes.
        /// </summary>
        /// <param name="service">The retrieved service, or null if none was found</param>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>This ServiceLocator instance</returns>
        /// <exception cref="ArgumentException">If the service was not registered</exception>
        public ServiceLocator Get<T>(out T service) where T : class
        {
            if (TryGetFromRegistry(out service)) return this;

            if (TryGetNextInHierarchy(out ServiceLocator container))
            {
                container.Get(out service);
                return this;
            }

            throw new ArgumentException($"ServiceLocator.Get: Service of type {typeof(T).FullName} not registered");
        }

        /// <summary>
        /// Gets a service of type T with the given name from the current scope and any parent scopes.
        /// </summary>
        /// <param name="name">The name of the service to retrieve</param>
        /// <param name="service">The retrieved service, or null if none was found</param>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>This ServiceLocator instance</returns>
        /// <exception cref="ArgumentException">If the service was not registered</exception>
        public ServiceLocator Get<T>(string name, out T service) where T : class
        {
            if (TryGetFromRegistry(name, out service)) return this;

            if (TryGetNextInHierarchy(out ServiceLocator container))
            {
                container.Get(out service);
                return this;
            }

            throw new ArgumentException($"ServiceLocator.Get: Service of type {typeof(T).FullName} with name {name} not registered");
        }

        /// <summary>
        /// Attempts to retrieve a service of type T from the current scope and any parent scopes.
        /// </summary>
        /// <param name="service">The retrieved service, or null if none was found</param>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>True if the service was found, false otherwise</returns>
        public bool TryGet<T>(out T service) where T : class
        {
            if (TryGetFromRegistry(out service)) return true;

            if (TryGetNextInHierarchy(out ServiceLocator container))
            {
                return container.TryGet(out service);
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve a service of type T from the current scope with the given name.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <param name="name">The name of the service to retrieve</param>
        /// <param name="service">The retrieved service, or null if none was found</param>
        /// <returns>True if the service was found, false otherwise</returns>
        public bool TryGet<T>(string name, out T service) where T : class
        {
            if (TryGetFromRegistry(name, out service)) return true;

            if (TryGetNextInHierarchy(out ServiceLocator container))
            {
                return container.TryGet(out service);
            }

            return false;
        }

        private static void GenerateGlobal()
        {
            var container = new GameObject(k_GlobalServiceLocatorName, typeof(ServiceLocator));
            DontDestroyOnLoad(container);
            _global = container.GetComponent<ServiceLocator>();
        }

        private bool TryGetFromRegistry<T>(out T service) where T : class
        {
            return _services.TryGet(out service);
        }

        private bool TryGetFromRegistry<T>(string name, out T service) where T : class
        {
            return _services.TryGet(name, out service);
        }

        private bool TryGetNextInHierarchy(out ServiceLocator container)
        {
            if (this == _global)
            {
                container = null;
                return false;
            }

            if (transform.parent != null)
            {
                container = transform.parent.GetComponentInParent<ServiceLocator>();
                return container != null;
            }
            else
            {
                container = ForSceneOf(this);
                return container != null;
            }
        }

        private void OnDestroy()
        {
            if (_global == this)
            {
                _global = null;
            }
            else if (_scenesContainer.ContainsValue(this))
            {
                _scenesContainer.Remove(gameObject.scene);
            }
        }

        /// <summary>
        /// Rebuilds the global service locator container for a new runtime
        /// session.
        /// </summary>
        public static void BootstrapGlobal()
        {
            if (_global != null)
            {
                Destroy(_global.gameObject);
            }

            GenerateGlobal();
            _scenesContainer.Clear();
            _tmpSceneGameObjects.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            BootstrapGlobal();
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/Handy Service Locator/Add Scene", false, 100)]
        public static GameObject AddSceneServiceLocator()
        {
            var go = new GameObject(k_SceneServiceLocatorName, typeof(ServiceLocatorSceneBootstrapper));
            return go;
        }
#endif
    }
}
