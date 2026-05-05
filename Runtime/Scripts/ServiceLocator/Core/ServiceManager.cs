using System;
using System.Collections.Generic;

namespace IndieGabo.HandyTools.HandyServiceLocator
{
    /// <summary>
    /// Stores globally registered services for the HandyTools runtime.
    /// </summary>
    public sealed class ServiceManager
    {
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<Type, Dictionary<ServiceIdentifier, object>>
            _identifiedServices = new();

        /// <summary>
        /// Clears all registered typed and named services.
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            _identifiedServices.Clear();
        }

        /// <summary>
        /// Attempts to resolve a service by its runtime type.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="service">Resolved service instance when found.</param>
        /// <returns>True when a matching service exists.</returns>
        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object instance)
                && instance is T typedService)
            {
                service = typedService;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Attempts to resolve the first registered service for the requested type.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="service">First registered service instance when found.</param>
        /// <returns>True when at least one matching service exists.</returns>
        public bool TryGetFirst<T>(out T service) where T : class
        {
            return TryGet(out service);
        }

        /// <summary>
        /// Copies all registered services of the requested type into the provided list.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="services">Destination list that receives the services.</param>
        /// <returns>The number of copied services.</returns>
        public int GetAll<T>(List<T> services) where T : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Clear();

            int count = 0;

            if (_services.TryGetValue(typeof(T), out object defaultInstance)
                && defaultInstance is T defaultService)
            {
                services.Add(defaultService);
                count++;
            }

            if (!_identifiedServices.TryGetValue(
                typeof(T),
                out Dictionary<ServiceIdentifier, object> identifiedServices
            ))
            {
                return count;
            }

            foreach (object instance in identifiedServices.Values)
            {
                if (instance is not T typedService)
                {
                    continue;
                }

                services.Add(typedService);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Attempts to resolve a service by explicit registration identifier.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="identifier">Explicit registration identifier.</param>
        /// <param name="service">Resolved service instance when found.</param>
        /// <returns>True when a matching named service exists.</returns>
        public bool TryGet<T>(ServiceIdentifier identifier, out T service) where T : class
        {
            if (!identifier.IsValid)
            {
                service = null;
                return false;
            }

            if (_identifiedServices.TryGetValue(
                    typeof(T),
                    out Dictionary<ServiceIdentifier, object> identifiedServices
                )
                && identifiedServices.TryGetValue(identifier, out object instance)
                && instance is T typedService)
            {
                service = typedService;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Attempts to resolve a service by explicit registration name.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="name">Explicit registration name.</param>
        /// <param name="service">Resolved service instance when found.</param>
        /// <returns>True when a matching named service exists.</returns>
        public bool TryGet<T>(string name, out T service) where T : class
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                service = null;
                return false;
            }

            return TryGet(new ServiceIdentifier(name), out service);
        }

        /// <summary>
        /// Attempts to resolve a service by GUID identifier.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="identifier">GUID identifier.</param>
        /// <param name="service">Resolved service instance when found.</param>
        /// <returns>True when a matching named service exists.</returns>
        public bool TryGet<T>(Guid identifier, out T service) where T : class
        {
            if (identifier == Guid.Empty)
            {
                service = null;
                return false;
            }

            return TryGet(new ServiceIdentifier(identifier), out service);
        }

        /// <summary>
        /// Resolves a required service by its runtime type.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <returns>The registered service instance.</returns>
        public T GetRequired<T>() where T : class
        {
            if (TryGet(out T service))
            {
                return service;
            }

            throw new InvalidOperationException(
                $"Service of type {typeof(T).FullName} is not registered."
            );
        }

        /// <summary>
        /// Resolves the first registered service for the requested type.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <returns>The first registered service instance.</returns>
        public T GetFirstRequired<T>() where T : class
        {
            return GetRequired<T>();
        }

        /// <summary>
        /// Resolves a required named service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="identifier">Explicit registration identifier.</param>
        /// <returns>The registered named service instance.</returns>
        public T GetRequired<T>(ServiceIdentifier identifier) where T : class
        {
            ValidateIdentifier(identifier);

            if (TryGet(identifier, out T service))
            {
                return service;
            }

            throw new InvalidOperationException(
                $"Service of type {typeof(T).FullName} with identifier {identifier} is not registered."
            );
        }

        /// <summary>
        /// Resolves a required named service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="name">Explicit registration name.</param>
        /// <returns>The registered named service instance.</returns>
        public T GetRequired<T>(string name) where T : class
        {
            return GetRequired<T>(new ServiceIdentifier(name));
        }

        /// <summary>
        /// Resolves a required GUID-keyed service.
        /// </summary>
        /// <typeparam name="T">Expected service type.</typeparam>
        /// <param name="identifier">GUID identifier.</param>
        /// <returns>The registered named service instance.</returns>
        public T GetRequired<T>(Guid identifier) where T : class
        {
            return GetRequired<T>(new ServiceIdentifier(identifier));
        }

        /// <summary>
        /// Registers a service using its generic type as the lookup key.
        /// </summary>
        /// <typeparam name="T">Service type to register.</typeparam>
        /// <param name="service">Service instance to store.</param>
        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Type type = typeof(T);
            if (_services.TryGetValue(type, out object currentService))
            {
                if (ReferenceEquals(currentService, service))
                {
                    throw new InvalidOperationException(
                        $"Default service of type {type.FullName} is already registered."
                    );
                }

                throw new InvalidOperationException(
                    $"Default service of type {type.FullName} is already registered. "
                    + $"Additional services of this type must use {nameof(ServiceIdentifier)}."
                );
            }

            _services.Add(type, service);
        }

        /// <summary>
        /// Registers a service using an explicit identifier and type.
        /// </summary>
        /// <typeparam name="T">Service type to register.</typeparam>
        /// <param name="identifier">Registration identifier.</param>
        /// <param name="service">Service instance to store.</param>
        public void Register<T>(ServiceIdentifier identifier, T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            ValidateIdentifier(identifier);

            Dictionary<ServiceIdentifier, object> identifiedServices
                = GetOrCreateIdentifiedServices(typeof(T));

            if (!identifiedServices.TryAdd(identifier, service))
            {
                throw new InvalidOperationException(
                    $"Service of type {typeof(T).FullName} with identifier {identifier} is already registered."
                );
            }
        }

        /// <summary>
        /// Registers a service using an explicit string key and type.
        /// </summary>
        /// <typeparam name="T">Service type to register.</typeparam>
        /// <param name="name">Registration name.</param>
        /// <param name="service">Service instance to store.</param>
        public void Register<T>(string name, T service) where T : class
        {
            Register(new ServiceIdentifier(name), service);
        }

        /// <summary>
        /// Registers a service using a GUID identifier and type.
        /// </summary>
        /// <typeparam name="T">Service type to register.</typeparam>
        /// <param name="identifier">GUID identifier.</param>
        /// <param name="service">Service instance to store.</param>
        public void Register<T>(Guid identifier, T service) where T : class
        {
            Register(new ServiceIdentifier(identifier), service);
        }

        /// <summary>
        /// Removes a typed service registration when the stored instance matches.
        /// </summary>
        /// <typeparam name="T">Service type to remove.</typeparam>
        /// <param name="service">Service instance being removed.</param>
        /// <returns>True when the registration was removed.</returns>
        public bool Deregister<T>(T service) where T : class
        {
            if (service == null)
            {
                return false;
            }

            Type type = typeof(T);
            if (!_services.TryGetValue(type, out object currentService))
            {
                return false;
            }

            if (!ReferenceEquals(currentService, service))
            {
                return false;
            }

            return _services.Remove(type);
        }

        /// <summary>
        /// Removes a named service registration.
        /// </summary>
        /// <param name="identifier">Registration identifier to remove.</param>
        /// <typeparam name="T">Service type associated with the identifier.</typeparam>
        /// <returns>True when the registration was removed.</returns>
        public bool Deregister<T>(ServiceIdentifier identifier) where T : class
        {
            if (!identifier.IsValid)
            {
                return false;
            }

            Type type = typeof(T);
            if (!_identifiedServices.TryGetValue(
                type,
                out Dictionary<ServiceIdentifier, object> identifiedServices
            ))
            {
                return false;
            }

            bool removed = identifiedServices.Remove(identifier);
            if (identifiedServices.Count == 0)
            {
                _identifiedServices.Remove(type);
            }

            return removed;
        }

        private static void ValidateIdentifier(ServiceIdentifier identifier)
        {
            if (!identifier.IsValid)
            {
                throw new ArgumentException(
                    "Service identifier must be created from a non-empty string or GUID.",
                    nameof(identifier)
                );
            }
        }

        /// <summary>
        /// Removes a named service registration.
        /// </summary>
        /// <param name="name">Registration name to remove.</param>
        /// <typeparam name="T">Service type associated with the name.</typeparam>
        /// <returns>True when the registration was removed.</returns>
        public bool Deregister<T>(string name) where T : class
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return Deregister<T>(new ServiceIdentifier(name));
        }

        /// <summary>
        /// Removes a GUID-keyed service registration.
        /// </summary>
        /// <param name="identifier">GUID identifier to remove.</param>
        /// <typeparam name="T">Service type associated with the identifier.</typeparam>
        /// <returns>True when the registration was removed.</returns>
        public bool Deregister<T>(Guid identifier) where T : class
        {
            if (identifier == Guid.Empty)
            {
                return false;
            }

            return Deregister<T>(new ServiceIdentifier(identifier));
        }

        private Dictionary<ServiceIdentifier, object> GetOrCreateIdentifiedServices(
            Type type
        )
        {
            if (_identifiedServices.TryGetValue(
                type,
                out Dictionary<ServiceIdentifier, object> identifiedServices
            ))
            {
                return identifiedServices;
            }

            identifiedServices = new Dictionary<ServiceIdentifier, object>();
            _identifiedServices.Add(type, identifiedServices);
            return identifiedServices;
        }
    }
}
