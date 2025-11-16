using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime.Tasks;
using Resonance.Gameplay.Enemies.Core;
using Resonance.Gameplay.Enemies.Movement;

namespace Resonance.Gameplay.Enemies.BTNodes
{
    /// <summary>
    /// Base class for all enemy behavior tree ACTION tasks
    /// Provides common functionality for accessing enemy components
    /// 
    /// Behavior Designer Best Practices:
    /// - Override OnAwake() to cache references
    /// - Override OnStart() for initialization when task starts
    /// - Override OnUpdate() for per-frame execution
    /// - Override OnEnd() for cleanup
    /// - Use SharedVariables for data sharing between tasks
    /// </summary>
    public abstract class EnemyActionBase : Action
    {
        // ===== Cached References =====
        protected EnemyMonoBehaviour enemyMono;
        protected EnemyController controller;
        protected EnemyMovement movement;
        protected NavMeshAgent navAgent;
        protected Animator animator;
        protected EnemyAnimator enemyAnimator;
        
        // ===== Unity Lifecycle =====
        
        /// <summary>
        /// Called once when the task is loaded
        /// Cache component references here for performance
        /// </summary>
        public override void OnAwake()
        {
            // Get EnemyMonoBehaviour from the GameObject running the behavior tree
            enemyMono = GetComponent<EnemyMonoBehaviour>();
            
            if (enemyMono == null)
            {
                Debug.LogError($"[BT Action] {GetType().Name}: No EnemyMonoBehaviour found on {gameObject.name}! " +
                              "Behavior tree must be attached to the enemy root GameObject.");
                return;
            }
            
            // Wait for enemy to initialize
            if (!enemyMono.IsInitialized)
            {
                Debug.LogWarning($"[BT Action] {GetType().Name}: Enemy not initialized yet, will retry in OnStart");
            }
            else
            {
                CacheReferences();
            }
        }
        
        /// <summary>
        /// Called every time the task starts (can be called multiple times)
        /// </summary>
        public override void OnStart()
        {
            // Ensure we have cached references (in case OnAwake ran before enemy initialized)
            if (controller == null && enemyMono != null && enemyMono.IsInitialized)
            {
                CacheReferences();
            }
        }
        
        // ===== Helper Methods =====
        
        /// <summary>
        /// Cache references to enemy components
        /// </summary>
        private void CacheReferences()
        {
            if (enemyMono == null || !enemyMono.IsInitialized)
            {
                return;
            }
            
            controller = enemyMono.Controller;
            movement = controller?.Movement;
            navAgent = movement?.GetNavAgent();
            animator = enemyMono.GetComponentInChildren<Animator>();
            enemyAnimator = enemyMono.GetComponentInChildren<EnemyAnimator>();
            
            if (controller == null)
            {
                Debug.LogError($"[BT Action] {GetType().Name}: EnemyController not found!");
            }
            
            if (movement == null)
            {
                Debug.LogWarning($"[BT Action] {GetType().Name}: EnemyMovement not found!");
            }
            
            if (navAgent == null)
            {
                Debug.LogWarning($"[BT Action] {GetType().Name}: NavMeshAgent not found!");
            }
            
            if (animator == null)
            {
                Debug.LogWarning($"[BT Action] {GetType().Name}: Animator not found!");
            }
            
            if (enemyAnimator == null)
            {
                Debug.LogWarning($"[BT Action] {GetType().Name}: EnemyAnimator not found!");
            }
        }
        
        /// <summary>
        /// Validate that all required components are available
        /// Call this at the start of OnUpdate in derived classes
        /// </summary>
        protected bool ValidateComponents()
        {
            if (enemyMono == null)
            {
                Debug.LogError($"[BT Action] {GetType().Name}: EnemyMonoBehaviour is null!");
                return false;
            }
            
            if (!enemyMono.IsInitialized)
            {
                // Enemy not ready yet, wait
                return false;
            }
            
            if (controller == null)
            {
                CacheReferences(); // Try to cache again
                
                if (controller == null)
                {
                    Debug.LogError($"[BT Action] {GetType().Name}: EnemyController is null!");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Shorthand properties for convenience
        /// </summary>
        protected EnemyController Controller => controller;
        protected EnemyMovement Movement => movement;
        protected NavMeshAgent NavAgent => navAgent;
        protected Animator Animator => animator;
        protected EnemyAnimator EnemyAnimator => enemyAnimator;
    }
}

