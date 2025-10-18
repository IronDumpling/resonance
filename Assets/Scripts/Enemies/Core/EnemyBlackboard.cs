using System;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Enemies.Core
{
    public class EnemyBlackboard
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();
        private Dictionary<Type, object> _systems = new Dictionary<Type, object>();

        public void SetValue<T>(string key, T value)
        {
            _data[key] = value;
        }

        public T GetValue<T>(string key)
        {
            if (_data.TryGetValue(key, out object value))
            {
                return (T)value;
            }
            return default;
        }

        public void RegisterSystem<T>(T system) where T : class
        {
            _systems[typeof(T)] = system;
        }

        public T GetSystem<T>() where T : class
        {
            if (_systems.TryGetValue(typeof(T), out object system))
            {
                return (T)system;
            }
            return null;
        }

        public void Update()
        {
            var controller = GetSystem<EnemyController>();
            if (controller == null) return;

            // Update common data
            SetValue("hasTarget", controller.HasPlayerTarget);
            SetValue("inAttackRange", controller.IsPlayerInAttackRange());
            SetValue("isAlive", controller.IsAlive);
            SetValue("isCoreAlive", controller.IsCoreAlive);
            SetValue("currentState", controller.CurrentState);
            SetValue("position", controller.CurrentPosition);
            
            // Legacy boolean flags (for backward compatibility if needed)
            SetValue("isStunned", controller.IsStunned);
            SetValue("isReviving", controller.IsReviving);
            SetValue("isTrulyDead", controller.IsTrulyDead);
            
            if (controller.HasPlayerTarget)
            {
                SetValue("targetPosition", controller.LastKnownPlayerPosition);
            }
        }
    }
}
