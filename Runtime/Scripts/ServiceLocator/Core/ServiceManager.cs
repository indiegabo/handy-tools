using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IndieGabo.HandyTools.HandyServiceLocator
{
    public class ServiceManager
    {
        readonly Dictionary<Type, object> _services = new();
        readonly Dictionary<string, object> _namedServices = new();
        public IEnumerable<object> RegisteredServices => _services.Values;
        public IEnumerable<object> RegisteredNamedServices => _namedServices.Values;

        public bool TryGet<T>(out T service) where T : class
        {
            Type type = typeof(T);
            if (_services.TryGetValue(type, out object obj))
            {
                service = obj as T;
                return true;
            }

            service = null;
            return false;
        }

        public bool TryGet<T>(string name, out T service) where T : class
        {
            if (_namedServices.TryGetValue(name, out object obj))
            {
                service = obj as T;
                return true;
            }

            service = null;
            return false;
        }


        public T Get<T>() where T : class
        {
            Type type = typeof(T);

            if (_services.TryGetValue(type, out object obj))
            {
                return obj as T;
            }

            throw new ArgumentException($"Service of type {type.FullName} not registered");
        }

        public T Get<T>(string name) where T : class
        {
            if (_namedServices.TryGetValue(name, out object obj))
            {
                return obj as T;
            }

            throw new ArgumentException($"Service of type {typeof(T).FullName} with name {name} not registered");
        }

        public ServiceManager Register<T>(T service)
        {
            Type type = typeof(T);

            if (!_services.TryAdd(type, service))
            {
                Debug.LogError($"<color=#FFFFFF>[Handy Service Locator]</color> Service of type {type} already registered");
            }

            return this;
        }

        public ServiceManager Register(Type type, object service)
        {
            if (!type.IsInstanceOfType(service))
            {
                throw new ArgumentException($"Service must be of type {type}");
            }

            if (!_services.TryAdd(type, service))
            {
                Debug.LogError($"<color=#FFFFFF>[Handy Service Locator]</color> Service of type {type} already registered");
            }

            return this;
        }

        public ServiceManager Register(string name, object service)
        {
            if (!_namedServices.TryAdd(name, service))
            {
                Debug.LogError($"<color=#FFFFFF>[Handy Service Locator]</color> Service of type {service.GetType().FullName} with name {name} already registered");
            }
            return this;
        }

        public ServiceManager Deregister<T>(T service)
        {
            Type type = typeof(T);
            _services.Remove(type);
            return this;
        }

        public ServiceManager Deregister(Type type)
        {
            _services.Remove(type);
            return this;
        }

        public ServiceManager Deregister(string name)
        {
            _namedServices.Remove(name);
            return this;
        }
    }
}
