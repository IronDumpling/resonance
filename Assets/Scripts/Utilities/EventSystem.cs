using System;
using System.Collections.Generic;
using UnityEngine;
using Resonance.Core;

namespace Resonance.Utilities
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<object>> _eventHandlers = new Dictionary<Type, List<object>>();
        private static readonly object _lock = new object();

        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            lock (_lock)
            {
                Type eventType = typeof(T);
                if (!_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType] = new List<object>();
                }
                _eventHandlers[eventType].Add(handler);
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            lock (_lock)
            {
                Type eventType = typeof(T);
                if (_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType].Remove(handler);
                    if (_eventHandlers[eventType].Count == 0)
                    {
                        _eventHandlers.Remove(eventType);
                    }
                }
            }
        }

        public static void Publish<T>(T eventData) where T : IGameEvent
        {
            List<object> handlers = null;
            
            lock (_lock)
            {
                Type eventType = typeof(T);
                if (_eventHandlers.ContainsKey(eventType))
                {
                    handlers = new List<object>(_eventHandlers[eventType]);
                }
            }

            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        ((Action<T>)handler)?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error handling event {typeof(T).Name}: {ex.Message}");
                    }
                }
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _eventHandlers.Clear();
            }
        }
    }
}
