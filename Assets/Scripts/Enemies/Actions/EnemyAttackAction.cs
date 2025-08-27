using UnityEngine;
using Resonance.Interfaces.Objects;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.Actions
{
    /// <summary>
    /// Enemy attack action - handles attacking the player when in range
    /// Only executed in Normal state's Attack sub-state
    /// </summary>
    public class EnemyAttackAction : IEnemyAction
    {
        private float _actionTimer = 0f;
        private bool _isFinished = false;
        private bool _hasAttacked = false;
        
        // Action configuration
        private const float ATTACK_DURATION = 0.5f; // Time action stays active after attack
        
        public string Name => "Attack";
        public int Priority => 90; // High priority - interrupts most other actions
        public bool CanInterrupt => true; // Can be interrupted by damage or state changes
        public bool IsFinished => _isFinished;

        public bool CanStart(EnemyController enemy)
        {
            // Can only attack if:
            // 1. Enemy is alive and can attack
            // 2. Has player target in attack range
            // 3. Not on attack cooldown
            return enemy.CanAttack && 
                   enemy.HasPlayerTarget && 
                   enemy.IsPlayerInAttackRange();
        }

        public void Start(EnemyController enemy)
        {
            _actionTimer = 0f;
            _isFinished = false;
            _hasAttacked = false;
            
            Debug.Log("EnemyAttackAction: Started attack action");
        }

        public void Update(EnemyController enemy, float deltaTime)
        {
            _actionTimer += deltaTime;
            
            // Perform attack immediately on start
            if (!_hasAttacked)
            {
                PerformAttack(enemy);
                _hasAttacked = true;
            }
            
            // Check if action should finish
            if (_actionTimer >= ATTACK_DURATION)
            {
                _isFinished = true;
            }
            
            // Also finish if player is no longer in range or enemy can't attack
            if (!enemy.HasPlayerTarget || !enemy.IsPlayerInAttackRange())
            {
                _isFinished = true;
            }
        }

        public void Cancel(EnemyController enemy)
        {
            Debug.Log("EnemyAttackAction: Attack action cancelled");
            _isFinished = true;
        }

        public void OnDamageTaken(EnemyController enemy)
        {
            // Attack action continues even when taking damage
            // Could add flinch behavior here if needed
        }

        /// <summary>
        /// Perform the actual attack
        /// </summary>
        private void PerformAttack(EnemyController enemy)
        {
            bool attackSuccessful = enemy.LaunchAttack();
            
            if (attackSuccessful)
            {
                Debug.Log("EnemyAttackAction: Attack launched successfully");
            }
            else
            {
                Debug.LogWarning("EnemyAttackAction: Attack failed to launch");
            }
        }
    }
}
