using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Resonance.Utilities;
using Resonance.Core;

namespace Resonance.Core
{
    public class Services
    {
        private GlobalServices.InputService _inputService;
        private List<IGameSystem> _systems;
        private bool _isInitialized = false;
        private GameObject _gameManagerObject;

        public Services(GameObject gameManagerObject, ServiceConfiguration configuration)
        {
            _gameManagerObject = gameManagerObject;
            _systems = new List<IGameSystem>();
            DiscoverServices(configuration);
        }

        public T GetService<T>() where T : class, IGameSystem
        {
            return ServiceRegistry.Get<T>();
        }

        private void DiscoverServices(ServiceConfiguration configuration)
        {
            if (_inputService == null)
            {
                _inputService = new GlobalServices.InputService(configuration);
            }

            // Add services to list
            AddService(_inputService);
        }

        private void AddService(IGameSystem system)
        {
            if (system != null && !_systems.Contains(system))
            {
                _systems.Add(system);
            }
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("Services already initialized");
                return;
            }

            Debug.Log("Services: Initializing global services");

            // Sort systems by priority
            _systems = _systems.OrderBy(s => s.Priority).ToList();

            // Initialize all systems
            foreach (var system in _systems)
            {
                try
                {
                    Debug.Log($"Services: Initializing {system.GetType().Name}");
                    system.Initialize();
                    
                    // Register in service registry
                    RegisterSystemInRegistry(system);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to initialize {system.GetType().Name}: {ex.Message}");
                }
            }

            _isInitialized = true;
            Debug.Log("Services: All global services initialized");
        }

        private void RegisterSystemInRegistry(IGameSystem system)
        {
            // Register by interface type
            var interfaces = system.GetType().GetInterfaces()
                .Where(i => i != typeof(IGameSystem) && typeof(IGameSystem).IsAssignableFrom(i));

            foreach (var interfaceType in interfaces)
            {
                var registerMethod = typeof(ServiceRegistry).GetMethod("Register").MakeGenericMethod(interfaceType);
                registerMethod.Invoke(null, new object[] { system });
            }

            // Also register by concrete type
            var concreteRegisterMethod = typeof(ServiceRegistry).GetMethod("Register").MakeGenericMethod(system.GetType());
            concreteRegisterMethod.Invoke(null, new object[] { system });
        }

        public void Shutdown()
        {
            if (!_isInitialized)
                return;

            Debug.Log("Services: Shutting down global services");

            // Shutdown in reverse order
            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                try
                {
                    _systems[i].Shutdown();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error shutting down {_systems[i].GetType().Name}: {ex.Message}");
                }
            }

            ServiceRegistry.Clear();
            _isInitialized = false;
        }

        public bool IsServiceAvailable<T>() where T : class, IGameSystem
        {
            return ServiceRegistry.IsRegistered<T>();
        }

        public int GetSystemCount()
        {
            return _systems.Count;
        }
    }
}
