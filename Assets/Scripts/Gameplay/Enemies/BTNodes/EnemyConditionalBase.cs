using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Resonance.Gameplay.Enemies.Core;
using Resonance.Gameplay.Enemies.Movement;

namespace Resonance.Gameplay.Enemies.BTNodes
{
    /// <summary>
    /// Base class for all enemy behavior tree CONDITIONAL tasks
    /// Provides common functionality for accessing enemy components
    /// 
    /// Behavior Designer Best Practices:
    /// - Override OnAwake() to cache references
    /// - Override OnStart() for initialization when task starts
    /// - Override OnUpdate() for per-frame execution (return Success/Failure based on condition)
    /// - Override OnEnd() for cleanup
    /// - Use SharedVariables for data sharing between tasks
    /// </summary>
    public abstract class EnemyConditionalBase : Conditional
    {
        // ===== Cached References =====
        protected EnemyMonoBehaviour enemyMono;
        protected EnemyController controller;
        protected EnemyMovement movement;
        protected Animator animator;
        
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
                Debug.LogError($"[BT Conditional] {GetType().Name}: No EnemyMonoBehaviour found on {gameObject.name}! " +
                              "Behavior tree must be attached to the enemy root GameObject.");
                return;
            }
            
            // Wait for enemy to initialize
            if (!enemyMono.IsInitialized)
            {
                Debug.LogWarning($"[BT Conditional] {GetType().Name}: Enemy not initialized yet, will retry in OnStart");
            }
            else
            {
                CacheReferences();
            }

            Debug.Log($"[BT Conditional] {GetType().Name}: OnAwake called on {gameObject.name}");
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
                Debug.LogWarning($"[BT Conditional] {GetType().Name}: Cannot cache - enemyMono null or not initialized");
                return;
            }
            
            controller = enemyMono.Controller;
            movement = controller?.Movement;
            animator = enemyMono.GetComponentInChildren<Animator>();
            
            Debug.Log($"[BT Conditional] {GetType().Name}: Cached references on {gameObject.name} - Controller: {controller != null}, Movement: {movement != null}, Animator: {animator != null}");
            
            if (controller == null)
            {
                Debug.LogError($"[BT Conditional] {GetType().Name}: EnemyController not found on {gameObject.name}!");
            }
            
            if (movement == null)
            {
                Debug.LogWarning($"[BT Conditional] {GetType().Name}: EnemyMovement not found on {gameObject.name}!");
            }
            
            if (animator == null)
            {
                Debug.LogWarning($"[BT Conditional] {GetType().Name}: Animator not found on {gameObject.name}!");
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
                Debug.LogError($"[BT Conditional] {GetType().Name}: EnemyMonoBehaviour is null on {gameObject?.name ?? "unknown"}!");
                return false;
            }
            
            if (!enemyMono.IsInitialized)
            {
                // Enemy not ready yet, wait
                Debug.LogWarning($"[BT Conditional] {GetType().Name}: EnemyMonoBehaviour not initialized yet on {gameObject.name}");
                return false;
            }
            
            if (controller == null)
            {
                Debug.LogWarning($"[BT Conditional] {GetType().Name}: Controller is null, attempting to cache references again on {gameObject.name}");
                CacheReferences(); // Try to cache again
                
                if (controller == null)
                {
                    Debug.LogError($"[BT Conditional] {GetType().Name}: EnemyController is still null after cache attempt on {gameObject.name}!");
                    return false;
                }
                else
                {
                    Debug.Log($"[BT Conditional] {GetType().Name}: Successfully cached controller on retry on {gameObject.name}");
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Shorthand properties for convenience
        /// </summary>
        protected EnemyController Controller => controller;
        protected EnemyMovement Movement => movement;
        protected Animator Animator => animator;
    }
}

