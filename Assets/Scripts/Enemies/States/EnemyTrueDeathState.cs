using UnityEngine;
using Resonance.Core;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.States
{
    /// <summary>
    /// Enemy真死亡状态，精神血量归零时进入
    /// 完全死亡，播放死亡动画，掉落物品，销毁对象
    /// </summary>
    public class EnemyTrueDeathState : IState
    {
        private EnemyController _enemyController;
        private float _deathTimer = 0f;
        private bool _deathEffectsTriggered = false;
        private bool _lootDropped = false;
        
        // Death sequence timing
        private const float DEATH_ANIMATION_DURATION = 2f;
        private const float LOOT_DROP_DELAY = 1f;
        private const float DESTRUCTION_DELAY = 3f;
        
        public string Name => "TrueDeath";

        public EnemyTrueDeathState(EnemyController enemyController)
        {
            _enemyController = enemyController;
        }

        public void Enter()
        {
            Debug.Log("EnemyState: Entered True Death state - enemy completely destroyed");
            
            _deathTimer = 0f;
            _deathEffectsTriggered = false;
            _lootDropped = false;
            
            // Stop all behaviors immediately
            _enemyController.StopPatrol();
            _enemyController.LosePlayer();
            
            // Trigger immediate death effects
            TriggerDeathEffects();
            
            Debug.Log("EnemyState: True death sequence initiated");
        }

        public void Update()
        {
            _deathTimer += Time.deltaTime;
            
            // Drop loot after delay
            if (!_lootDropped && _deathTimer >= LOOT_DROP_DELAY)
            {
                DropLoot();
            }
            
            // Destruction happens automatically via MonoBehaviour after DESTRUCTION_DELAY
        }

        public void Exit()
        {
            Debug.Log("EnemyState: Exited True Death state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // True death is terminal - no transitions allowed
            // Object should be destroyed before any transition attempts
            return false;
        }

        /// <summary>
        /// Trigger death visual and audio effects
        /// </summary>
        private void TriggerDeathEffects()
        {
            if (_deathEffectsTriggered) return;
            
            _deathEffectsTriggered = true;
            
            // TODO: Play death animation
            // TODO: Play death audio
            // TODO: Spawn death particles
            // TODO: Apply death material
            
            Debug.Log("EnemyTrueDeathState: Death effects triggered");
        }

        /// <summary>
        /// Drop loot items
        /// </summary>
        private void DropLoot()
        {
            if (_lootDropped) return;
            
            _lootDropped = true;
            
            var stats = _enemyController.Stats;
            
            // Check if loot should drop based on chance
            if (stats.deathLootPrefab == null)
            {
                Debug.Log("EnemyTrueDeathState: No loot prefab configured, skipping loot drop");
                return;
            }
            
            if (Random.Range(0f, 1f) > stats.lootDropChance)
            {
                Debug.Log($"EnemyTrueDeathState: Loot drop failed chance check ({stats.lootDropChance:P0} chance)");
                return;
            }
            
            // TODO: has some bug here, need to fix it. Get enemy position for loot spawn
            Vector3 enemyPosition = _enemyController.CurrentPosition;
            
            // Spawn loot items
            int itemsToSpawn = stats.lootCount;
            for (int i = 0; i < itemsToSpawn; i++)
            {
                // Calculate spawn position with random offset
                Vector2 randomCircle = Random.insideUnitCircle * stats.lootSpawnRadius;
                Vector3 spawnPosition = enemyPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
                
                // Spawn the loot item
                GameObject lootItem = Object.Instantiate(stats.deathLootPrefab, spawnPosition, Quaternion.identity);
                
                if (lootItem != null)
                {
                    Debug.Log($"EnemyTrueDeathState: Spawned loot item {i + 1}/{itemsToSpawn} at {spawnPosition}");
                    
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
                    Debug.LogError($"EnemyTrueDeathState: Failed to instantiate loot item {i + 1}");
                }
            }
            
            Debug.Log($"EnemyTrueDeathState: Successfully dropped {itemsToSpawn} loot items");
        }

        /// <summary>
        /// Get time since death
        /// </summary>
        public float GetDeathTime()
        {
            return _deathTimer;
        }

        /// <summary>
        /// Get time remaining until destruction
        /// </summary>
        public float GetDestructionTimeRemaining()
        {
            return Mathf.Max(0f, DESTRUCTION_DELAY - _deathTimer);
        }

        /// <summary>
        /// Check if death effects have been triggered
        /// </summary>
        public bool AreDeathEffectsTriggered()
        {
            return _deathEffectsTriggered;
        }

        /// <summary>
        /// Check if loot has been dropped
        /// </summary>
        public bool IsLootDropped()
        {
            return _lootDropped;
        }

        /// <summary>
        /// Check if ready for destruction
        /// </summary>
        public bool IsReadyForDestruction()
        {
            return _deathTimer >= DESTRUCTION_DELAY;
        }
    }
}
