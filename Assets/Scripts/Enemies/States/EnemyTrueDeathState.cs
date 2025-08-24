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
            
            // TODO: Implement loot dropping system
            // For now, just log the event
            Debug.Log("EnemyTrueDeathState: Loot dropped (placeholder)");
            
            // Example loot drops:
            // - Ammo pickups
            // - Health items
            // - Experience points
            // - Special items based on enemy type
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
