using System;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Enemies.Core
{
    /// <summary>
    /// Blackboard keys using hashed integers for better performance
    /// </summary>
    public static class BlackboardKeys
    {
        public static readonly int HasTarget = "hasTarget".GetHashCode();
        public static readonly int InAttackRange = "inAttackRange".GetHashCode();
        public static readonly int IsAlive = "isAlive".GetHashCode();
        public static readonly int IsCoreAlive = "isCoreAlive".GetHashCode();
        public static readonly int CurrentState = "currentState".GetHashCode();
        public static readonly int Position = "position".GetHashCode();
        public static readonly int IsStunned = "isStunned".GetHashCode();
        public static readonly int IsReviving = "isReviving".GetHashCode();
        public static readonly int IsTrulyDead = "isTrulyDead".GetHashCode();
        public static readonly int TargetPosition = "targetPosition".GetHashCode();
    }

    public class EnemyBlackboard
    {
        // Use int keys for better performance (avoid string allocations and comparisons)
        private Dictionary<int, object> _data = new Dictionary<int, object>();
        private Dictionary<Type, object> _systems = new Dictionary<Type, object>();

        // Optimized methods using int keys
        public void SetValue<T>(int key, T value)
        {
            _data[key] = value;
        }

        public T GetValue<T>(int key)
        {
            if (_data.TryGetValue(key, out object value))
            {
                return (T)value;
            }
            return default;
        }

        // Legacy string-based methods for backward compatibility (will hash the key)
        public void SetValue<T>(string key, T value)
        {
            _data[key.GetHashCode()] = value;
        }

        public T GetValue<T>(string key)
        {
            if (_data.TryGetValue(key.GetHashCode(), out object value))
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
            SetValue("position", controller.CurrentPosition);
            
            // ★ 使用统一的状态数据系统
            var stateData = controller.StateData;
            SetValue("currentState", stateData.CurrentState);
            
            // 生命值状态（三个互斥的 bool）
            SetValue("isPhysicallyAlive", stateData.IsPhysicallyAlive);
            SetValue("isPhysicallyDead", stateData.IsPhysicallyDead);
            SetValue("isCoreDead", stateData.IsCoreDead);
            
            if (controller.HasPlayerTarget)
            {
                SetValue("targetPosition", controller.LastKnownPlayerPosition);
            }
        }
    }
}
