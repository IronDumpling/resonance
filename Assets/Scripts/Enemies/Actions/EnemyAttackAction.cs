using UnityEngine;
using Resonance.Interfaces.Objects;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.Actions
{
    /// <summary>
    /// Enemy attack action - triggers attack animation and manages attack flow
    /// Only executed in Normal state's Attack sub-state
    /// Damage is dealt through hitbox during animation window
    /// </summary>
    public class EnemyAttackAction : IEnemyAction
    {
        private float _actionTimer = 0f;
        private bool _isFinished = false;
        private bool _hasTriggeredAnimation = false;
        
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
            _hasTriggeredAnimation = false;
            
            Debug.Log("EnemyAttackAction: Started attack action - will trigger animation");
        }

        public void Update(EnemyController enemy, float deltaTime)
        {
            _actionTimer += deltaTime;
            
            // Trigger animation and start attack process on first frame
            if (!_hasTriggeredAnimation)
            {
                TriggerAttackAnimation(enemy);
                _hasTriggeredAnimation = true;
            }
            
            // Check if action should finish based on attack duration
            if (_actionTimer >= enemy.AttackDuration)
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
        /// Trigger attack animation and start attack process
        /// Actual damage will be dealt through hitbox during animation window
        /// </summary>
        private void TriggerAttackAnimation(EnemyController enemy)
        {
            // Start the attack process (sets cooldown, triggers events)
            bool attackStarted = enemy.LaunchAttack();
            
            if (attackStarted)
            {
                Debug.Log("EnemyAttackAction: Attack process started - animation should be triggered");
                // The animation trigger will be handled by the MonoBehaviour bridge
                // through the OnAttackLaunched event
            }
            else
            {
                Debug.LogWarning("EnemyAttackAction: Attack failed to start");
                _isFinished = true; // End action if attack couldn't start
            }
        }
    }
}
