using System;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Utilities
{
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private static readonly object _lock = new object();

        public static void Register<T>(T service) where T : class
        {
            lock (_lock)
            {
                Type serviceType = typeof(T);
                if (_services.ContainsKey(serviceType))
                {
                    Debug.LogWarning($"Service of type {serviceType.Name} is already registered. Overwriting.");
                }
                _services[serviceType] = service;
            }
        }

        public static T Get<T>() where T : class
        {
            lock (_lock)
            {
                Type serviceType = typeof(T);
                if (_services.TryGetValue(serviceType, out object service))
                {
                    return service as T;
                }
                return null;
            }
        }

        public static bool IsRegistered<T>() where T : class
        {
            lock (_lock)
            {
                return _services.ContainsKey(typeof(T));
            }
        }

        public static void Unregister<T>() where T : class
        {
            lock (_lock)
            {
                Type serviceType = typeof(T);
                if (_services.ContainsKey(serviceType))
                {
                    _services.Remove(serviceType);
                }
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }
    }
}
