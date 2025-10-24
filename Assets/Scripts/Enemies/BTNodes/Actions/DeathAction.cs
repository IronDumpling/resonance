using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// True death action - triggers death animation and handles object destruction
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyActionBase for component access
    /// - This is triggered when crystal core is destroyed
    /// - Returns Running during death animation, Success when ready for destruction
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Handles true death sequence when core is destroyed")]
    public class DeathAction : EnemyActionBase
    {
        private bool _deathTriggered = false;
        private bool _lootDropped = false;
        private float _destructionTimer = 0f;
        private const float DESTRUCTION_DELAY = 2f;
        private GameObject _deathFXPrefab;

        public override void OnStart()
        {
            base.OnStart();
            _deathTriggered = false;
            _lootDropped = false;
            _destructionTimer = 0f;
            _deathFXPrefab = Resources.Load<GameObject>("Prefabs/FX/Explosion_Small_FX");
        }

        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            if (!_deathTriggered)
            {
                // Stop all movement
                Movement?.Stop();
                Controller.StopPatrol();
                Controller.LosePlayer();

                // Trigger death animation
                if (Animator != null && Animator.isActiveAndEnabled)
                {
                    Animator.SetTrigger("TrueDeath");
                }
                
                _deathTriggered = true;
            }

            // Wait for destruction delay
            _destructionTimer += Time.deltaTime;
            
            if (_destructionTimer >= DESTRUCTION_DELAY)
            {
                if (_deathFXPrefab != null)
                {
                    GameObject deathFX = Object.Instantiate(_deathFXPrefab, transform.position, Quaternion.identity);
                }

                // Drop loot on death
                DropLoot();

                // Play death audio
                enemyMono.PlayDeathAudio();

                // Destroy the enemy GameObject after delay
                Object.Destroy(gameObject, 0.5f);
                
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            _deathTriggered = false;
            _lootDropped = false;
            _destructionTimer = 0f;
        }

        /// <summary>
        /// Drop loot items based on enemy stats configuration
        /// </summary>
        private void DropLoot()
        {
            if (_lootDropped)
            {
                Debug.Log("DeathAction: Loot already dropped, skipping");
                return;
            }
            
            _lootDropped = true;
            
            var stats = Controller.Stats;
            
            // Check if loot prefab is configured
            if (stats.deathLootPrefab == null)
            {
                Debug.Log("DeathAction: No loot prefab configured, skipping loot drop");
                return;
            }
            
            // Check loot drop chance
            if (Random.Range(0f, 1f) > stats.lootDropChance)
            {
                Debug.Log($"DeathAction: Loot drop failed chance check ({stats.lootDropChance:P0} chance)");
                return;
            }
            
            // Get enemy position for loot spawn
            Vector3 enemyPosition = Controller.CurrentPosition;
            
            // Spawn loot items
            int itemsToSpawn = stats.lootCount;
            for (int i = 0; i < itemsToSpawn; i++)
            {
                // Calculate spawn position with random offset within spawn radius
                Vector2 randomCircle = Random.insideUnitCircle * stats.lootSpawnRadius;
                Vector3 spawnPosition = enemyPosition + new Vector3(randomCircle.x, 0.5f, randomCircle.y);
                
                // Spawn the loot item
                GameObject lootItem = Object.Instantiate(stats.deathLootPrefab, spawnPosition, Quaternion.identity);
                
                if (lootItem != null)
                {
                    Debug.Log($"DeathAction: Spawned loot item {i + 1}/{itemsToSpawn} at {spawnPosition}");
                    
                    // Add a small upward velocity for visual effect
                    Rigidbody lootRb = lootItem.GetComponent<Rigidbody>();
                    if (lootRb != null)
                    {
                        Vector3 randomForce = new Vector3(
                            Random.Range(-2f, 2f),
                            Random.Range(3f, 5f),
                            Random.Range(-2f, 2f)
                        );
                        lootRb.AddForce(randomForce, ForceMode.Impulse);
                    }
                }
                else
                {
                    Debug.LogError($"DeathAction: Failed to instantiate loot item {i + 1}");
                }
            }
            
            Debug.Log($"DeathAction: Successfully dropped {itemsToSpawn} loot items");
        }
    }
}

