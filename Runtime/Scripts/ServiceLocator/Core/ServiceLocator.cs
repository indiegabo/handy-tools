using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.HandyServiceLocatorModule
{
    /// <summary>
    /// Provides access to the global HandyTools service registry.
    /// </summary>
    public static class ServiceLocator
    {
        private static ServiceManager _global = new();

        /// <summary>
        /// Registers the default service for its generic type.
        /// </summary>
        /// <typeparam name="T">Service type to register.</typeparam>
        /// <param name="service">Service instance to register.</param>
        public static void Register<T>(T service) where T : class
        {
            _global.Register(service);
        }

        /// <summary>
        /// Registers a service using an explicit identifier and type.
        /// </summary>
        /// <typeparam name="T">Service type to register.</typeparam>
        /// <param name="identifier">Registration identifier.</param>
        /// <param name="service">Service instance to register.</param>
        public static void Register<T>(ServiceIdentifier identifier, T service) where T : class
        {
            _global.Register(identifier, service);
        }

        /// <summary>
        /// Registers a service using an explicit string key and type.
        /// </summary>
        /// <typeparam name="T">Service type to register.</typeparam>
        /// <param name="name">Registration name.</param>
        /// <param name="service">Service instance to register.</param>
        public static void Register<T>(string name, T service) where T : class
        {
            _global.Register(name, service);
        }

        /// <summary>
        /// Registers a service using a GUID identifier and type.
        /// </summary>
        /// <typeparam name="T">Service type to register.</typeparam>
        /// <param name="identifier">GUID identifier.</param>
        /// <param name="service">Service instance to register.</param>
        public static void Register<T>(System.Guid identifier, T service) where T : class
        {
            _global.Register(identifier, service);
        }

        /// <summary>
        /// Removes a typed service registration when the stored instance matches.
        /// </summary>
        /// <typeparam name="T">Service type to remove.</typeparam>
        /// <param name="service">Service instance being removed.</param>
        /// <returns>True when the registration was removed.</returns>
        public static bool Deregister<T>(T service) where T : class
        {
            return _global.Deregister(service);
        }

        /// <summary>
        /// Removes a named service registration.
        /// </summary>
        /// <typeparam name="T">Service type associated with the identifier.</typeparam>
        /// <param name="identifier">Registration identifier to remove.</param>
        /// <returns>True when the registration was removed.</returns>
        public static bool Deregister<T>(ServiceIdentifier identifier) where T : class
        {
            return _global.Deregister<T>(identifier);
        }

        /// <summary>
        /// Removes a named service registration.
        /// </summary>
        /// <typeparam name="T">Service type associated with the name.</typeparam>
        /// <param name="name">Registration name to remove.</param>
        /// <returns>True when the registration was removed.</returns>
        public static bool Deregister<T>(string name) where T : class
        {
            return _global.Deregister<T>(name);
        }

        /// <summary>
        /// Removes a GUID-keyed service registration.
        /// </summary>
        /// <typeparam name="T">Service type associated with the identifier.</typeparam>
        /// <param name="identifier">GUID identifier to remove.</param>
        /// <returns>True when the registration was removed.</returns>
        public static bool Deregister<T>(System.Guid identifier) where T : class
        {
            return _global.Deregister<T>(identifier);
        }

        /// <summary>
        /// Attempts to retrieve the default globally registered service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="service">Resolved service instance when found.</param>
        /// <returns>True when a default service exists for the type.</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            return _global.TryGet(out service);
        }

        /// <summary>
        /// Attempts to retrieve the default globally registered service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="service">Default service instance when found.</param>
        /// <returns>True when a default service exists for the type.</returns>
        public static bool TryGetFirst<T>(out T service) where T : class
        {
            return _global.TryGetFirst(out service);
        }

        /// <summary>
        /// Copies the default and identified services of the requested type
        /// into the provided list.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="services">Destination list that receives the services.</param>
        /// <returns>The number of copied services.</returns>
        public static int GetAll<T>(List<T> services) where T : class
        {
            return _global.GetAll(services);
        }

        /// <summary>
        /// Attempts to retrieve a globally registered named service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="identifier">Registration identifier.</param>
        /// <param name="service">Resolved service instance when found.</param>
        /// <returns>True when a matching service exists.</returns>
        public static bool TryGet<T>(ServiceIdentifier identifier, out T service) where T : class
        {
            return _global.TryGet(identifier, out service);
        }

        /// <summary>
        /// Attempts to retrieve a globally registered named service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="name">Registration name.</param>
        /// <param name="service">Resolved service instance when found.</param>
        /// <returns>True when a matching service exists.</returns>
        public static bool TryGet<T>(string name, out T service) where T : class
        {
            return _global.TryGet(name, out service);
        }

        /// <summary>
        /// Attempts to retrieve a GUID-keyed service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="identifier">GUID identifier.</param>
        /// <param name="service">Resolved service instance when found.</param>
        /// <returns>True when a matching service exists.</returns>
        public static bool TryGet<T>(System.Guid identifier, out T service) where T : class
        {
            return _global.TryGet(identifier, out service);
        }

        /// <summary>
        /// Resolves the required default globally registered service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <returns>The registered service instance.</returns>
        public static T GetRequired<T>() where T : class
        {
            return _global.GetRequired<T>();
        }

        /// <summary>
        /// Resolves the required default globally registered service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <returns>The default registered service instance.</returns>
        public static T GetFirstRequired<T>() where T : class
        {
            return _global.GetFirstRequired<T>();
        }

        /// <summary>
        /// Resolves a required globally registered named service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="identifier">Registration identifier.</param>
        /// <returns>The registered named service instance.</returns>
        public static T GetRequired<T>(ServiceIdentifier identifier) where T : class
        {
            return _global.GetRequired<T>(identifier);
        }

        /// <summary>
        /// Resolves a required globally registered named service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="name">Registration name.</param>
        /// <returns>The registered named service instance.</returns>
        public static T GetRequired<T>(string name) where T : class
        {
            return _global.GetRequired<T>(name);
        }

        /// <summary>
        /// Resolves a required GUID-keyed service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="identifier">GUID identifier.</param>
        /// <returns>The registered named service instance.</returns>
        public static T GetRequired<T>(System.Guid identifier) where T : class
        {
            return _global.GetRequired<T>(identifier);
        }

        /// <summary>
        /// Rebuilds the global service registry for a new runtime session.
        /// </summary>
        public static void BootstrapGlobal()
        {
            _global = new ServiceManager();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            BootstrapGlobal();
        }
    }
}
