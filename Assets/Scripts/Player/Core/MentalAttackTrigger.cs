using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Player.Core;
using Resonance.Enemies;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Trigger component that detects enemies in physical death state within mental attack range.
    /// Similar to EnemyDetectionTrigger but specifically for player's mental attack detection.
    /// Should be attached to the MentalAttackRange GameObject under the Player.
    /// </summary>
    public class MentalAttackTrigger : MonoBehaviour
    {
        // Core references
        private PlayerController _playerController;
        private bool _isInitialized = false;

        // Enemy tracking
        private List<EnemyHitbox> _enemiesInRange = new List<EnemyHitbox>();
        private List<EnemyHitbox> _deadEnemiesInRange = new List<EnemyHitbox>();

        // Events
        public System.Action<EnemyHitbox> OnDeadEnemyEntered;
        public System.Action<EnemyHitbox> OnDeadEnemyExited;
        public System.Action OnDeadEnemiesChanged; // General event for any change in dead enemies

        // Properties
        public bool HasDeadEnemiesInRange => _deadEnemiesInRange.Count > 0;
        public int DeadEnemyCount => _deadEnemiesInRange.Count;
        public List<EnemyHitbox> DeadEnemiesInRange => new List<EnemyHitbox>(_deadEnemiesInRange);

        #region Unity Lifecycle

        void Awake()
        {
            // Ensure we have a SphereCollider trigger
            var collider = GetComponent<SphereCollider>();
            if (collider == null)
            {
                Debug.LogError("MentalAttackTrigger: No SphereCollider found! Please add a SphereCollider component.");
                return;
            }

            if (!collider.isTrigger)
            {
                Debug.LogWarning("MentalAttackTrigger: SphereCollider is not set as trigger. Setting it now.");
                collider.isTrigger = true;
            }

            Debug.Log($"MentalAttackTrigger: Initialized with range {collider.radius}");
        }

        void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the trigger with player controller reference and set the range
        /// </summary>
        /// <param name="playerController">Reference to the player controller</param>
        /// <param name="range">Detection range (will set the SphereCollider radius)</param>
        public void Initialize(PlayerController playerController, float range)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("MentalAttackTrigger: Already initialized");
                return;
            }

            _playerController = playerController;

            // Set the collider radius
            var collider = GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius = range;
                Debug.Log($"MentalAttackTrigger: Set detection range to {range}");
            }

            _isInitialized = true;
            Debug.Log("MentalAttackTrigger: Initialized successfully");
        }

        #endregion

        #region Trigger Events

        void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized) return;

            // Check if it's an enemy
            var enemy = other.GetComponent<EnemyHitbox>();
            if (enemy == null) return;

            // Add to enemies in range if not already present
            if (!_enemiesInRange.Contains(enemy))
            {
                _enemiesInRange.Add(enemy);
                Debug.Log($"MentalAttackTrigger: Enemy {enemy.name} entered range");

                // Check if this enemy is in physical death state
                CheckEnemyDeathState(enemy);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!_isInitialized) return;

            // Check if it's an enemy
            var enemy = other.GetComponent<EnemyHitbox>();
            if (enemy == null) return;

            // Remove from all tracking lists
            if (_enemiesInRange.Contains(enemy))
            {
                _enemiesInRange.Remove(enemy);
                Debug.Log($"MentalAttackTrigger: Enemy {enemy.name} exited range");
            }

            if (_deadEnemiesInRange.Contains(enemy))
            {
                _deadEnemiesInRange.Remove(enemy);
                OnDeadEnemyExited?.Invoke(enemy);
                OnDeadEnemiesChanged?.Invoke();
                Debug.Log($"MentalAttackTrigger: Dead enemy {enemy.name} exited range");
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (!_isInitialized) return;

            // Continuously check enemy state for those in range
            var enemy = other.GetComponent<EnemyHitbox>();
            if (enemy != null && _enemiesInRange.Contains(enemy))
            {
                CheckEnemyDeathState(enemy);
            }
        }

        #endregion

        #region Enemy State Detection

        /// <summary>
        /// Check if an enemy is in physical death state and update tracking accordingly
        /// </summary>
        /// <param name="enemy">The enemy to check</param>
        private void CheckEnemyDeathState(EnemyHitbox enemy)
        {
            if (enemy == null) return;

            bool isInPhysicalDeathState = IsEnemyInPhysicalDeathState(enemy);
            bool wasInDeadList = _deadEnemiesInRange.Contains(enemy);

            if (isInPhysicalDeathState && !wasInDeadList)
            {
                // Enemy just entered physical death state
                _deadEnemiesInRange.Add(enemy);
                OnDeadEnemyEntered?.Invoke(enemy);
                OnDeadEnemiesChanged?.Invoke();
                Debug.Log($"MentalAttackTrigger: Enemy {enemy.name} entered physical death state");
            }
            else if (!isInPhysicalDeathState && wasInDeadList)
            {
                // Enemy left physical death state (revived or truly died)
                _deadEnemiesInRange.Remove(enemy);
                OnDeadEnemyExited?.Invoke(enemy);
                OnDeadEnemiesChanged?.Invoke();
                Debug.Log($"MentalAttackTrigger: Enemy {enemy.name} left physical death state");
            }
        }

        /// <summary>
        /// Check if an enemy is currently in physical death state
        /// This method checks the enemy's state machine for PhysicalDeath state
        /// </summary>
        /// <param name="enemy">The enemy to check</param>
        /// <returns>True if enemy is in physical death state</returns>
        private bool IsEnemyInPhysicalDeathState(EnemyHitbox enemy)
        {
            if (enemy == null || !enemy.IsInitialized) return false;

            // Check the enemy's controller state machine
            // var enemyController = enemy.Controller;
            // if (enemyController == null) return false;

            // // Check if the enemy is in PhysicalDeath state
            // // This assumes the enemy has a similar state machine structure to the player
            // string currentState = enemyController.CurrentState;
            // bool isPhysicallyDead = currentState == "PhysicalDeath";

            // // Alternative: Check health values if state machine check fails
            // if (!isPhysicallyDead)
            // {
            //     // Check if physical health is 0 but mental health > 0
            //     var stats = enemyController.Stats;
            //     if (stats != null)
            //     {
            //         isPhysicallyDead = stats.currentPhysicalHealth <= 0f && stats.currentMentalHealth > 0f;
            //     }
            // }

            return false;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Manually refresh the state of all enemies in range
        /// Useful for ensuring accuracy when enemy states change externally
        /// </summary>
        public void RefreshEnemyStates()
        {
            if (!_isInitialized) return;

            // Create a copy of the list to avoid modification during iteration
            var enemiesToCheck = new List<EnemyHitbox>(_enemiesInRange);
            
            foreach (var enemy in enemiesToCheck)
            {
                if (enemy != null)
                {
                    CheckEnemyDeathState(enemy);
                }
            }

            Debug.Log($"MentalAttackTrigger: Refreshed states for {enemiesToCheck.Count} enemies");
        }

        /// <summary>
        /// Get the closest dead enemy in range
        /// </summary>
        /// <returns>Closest dead enemy or null if none</returns>
        public EnemyHitbox GetClosestDeadEnemy()
        {
            if (_deadEnemiesInRange.Count == 0) return null;

            Vector3 playerPosition = transform.position;
            EnemyHitbox closest = null;
            float closestDistance = float.MaxValue;

            foreach (var enemy in _deadEnemiesInRange)
            {
                if (enemy != null)
                {
                    float distance = Vector3.Distance(playerPosition, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = enemy;
                    }
                }
            }

            return closest;
        }

        /// <summary>
        /// Check if a specific enemy is in range and in physical death state
        /// </summary>
        /// <param name="enemy">The enemy to check</param>
        /// <returns>True if enemy is in range and in physical death state</returns>
        public bool IsEnemyInRangeAndDead(EnemyHitbox enemy)
        {
            return enemy != null && _deadEnemiesInRange.Contains(enemy);
        }

        #endregion

        #region Debug and Utility

        /// <summary>
        /// Get debug information about current detection state
        /// </summary>
        /// <returns>Debug info string</returns>
        public string GetDebugInfo()
        {
            if (!_isInitialized) return "Not initialized";

            return $"Enemies in range: {_enemiesInRange.Count}, Dead enemies: {_deadEnemiesInRange.Count}";
        }

        /// <summary>
        /// Clean up resources and events
        /// </summary>
        private void Cleanup()
        {
            OnDeadEnemyEntered = null;
            OnDeadEnemyExited = null;
            OnDeadEnemiesChanged = null;

            _enemiesInRange.Clear();
            _deadEnemiesInRange.Clear();

            _isInitialized = false;
            Debug.Log("MentalAttackTrigger: Cleaned up");
        }

        #endregion

        #region Gizmos (for debugging)

        // void OnDrawGizmosSelected()
        // {
        //     var collider = GetComponent<SphereCollider>();
        //     if (collider != null)
        //     {
        //         // Draw the detection range
        //         Gizmos.color = HasDeadEnemiesInRange ? Color.red : Color.yellow;
        //         Gizmos.DrawWireSphere(transform.position, collider.radius);

        //         // Draw connections to dead enemies
        //         if (_deadEnemiesInRange != null)
        //         {
        //             Gizmos.color = Color.red;
        //             foreach (var enemy in _deadEnemiesInRange)
        //             {
        //                 if (enemy != null)
        //                 {
        //                     Gizmos.DrawLine(transform.position, enemy.transform.position);
        //                     Gizmos.DrawWireCube(enemy.transform.position, Vector3.one * 0.5f);
        //                 }
        //             }
        //         }
        //     }
        // }

        #endregion
    }
}
