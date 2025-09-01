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
        private bool _hasActivatedHitbox = false;
        
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
            bool canAttack = enemy.CanAttack;
            bool hasTarget = enemy.HasPlayerTarget;
            bool inRange = enemy.IsPlayerInAttackRange();
            
            // Break down CanAttack for detailed debugging
            bool isMentallyAlive = enemy.IsMentallyAlive;
            bool hasPlayerTargetForAttack = enemy.HasPlayerTarget;
            float currentTime = Time.time;
            float lastAttackTime = enemy.LastAttackTime;
            float attackCooldown = enemy.AttackCooldownValue;
            bool cooldownPassed = currentTime >= (lastAttackTime + attackCooldown);
            
            bool result = canAttack && hasTarget && inRange;
            
            // Optional: Keep minimal logging for important events
            if (!result && !cooldownPassed)
            {
                // Debug.Log($"EnemyAttackAction: Attack on cooldown - {(lastAttackTime + attackCooldown - currentTime):F1}s remaining");
            }
            
            return result;
        }

        public void Start(EnemyController enemy)
        {
            _actionTimer = 0f;
            _isFinished = false;
            _hasTriggeredAnimation = false;
            _hasActivatedHitbox = false;
            
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
            
            // NOTE: Hitbox activation is now handled by EnemyController.EnableHitbox()
            // which properly activates the DamageHitbox GameObject
            // For now, we'll keep the programmatic activation as a fallback
            // until animation events are properly configured
            float hitboxActivationTime = 0.2f; // Activate hitbox 0.2s into attack
            float hitboxDeactivationTime = 0.4f; // Deactivate hitbox 0.4s into attack
            
            if (!_hasActivatedHitbox && _actionTimer >= hitboxActivationTime)
            {
                enemy.EnableHitbox();
                _hasActivatedHitbox = true;
                Debug.Log("EnemyAttackAction: Programmatically enabled hitbox (fallback method)");
            }
            
            if (_hasActivatedHitbox && _actionTimer >= hitboxDeactivationTime)
            {
                enemy.DisableHitbox();
                Debug.Log("EnemyAttackAction: Programmatically disabled hitbox");
            }
            
            // Check if action should finish based on attack duration
            if (_actionTimer >= enemy.AttackDuration)
            {
                // Ensure hitbox is disabled when action finishes
                if (_hasActivatedHitbox)
                {
                    enemy.DisableHitbox();
                }
                _isFinished = true;
            }
            
            // Also finish if player is no longer in range or enemy can't attack
            if (!enemy.HasPlayerTarget || !enemy.IsPlayerInAttackRange())
            {
                // Ensure hitbox is disabled when action finishes
                if (_hasActivatedHitbox)
                {
                    enemy.DisableHitbox();
                }
                _isFinished = true;
            }
        }

        public void Cancel(EnemyController enemy)
        {
            Debug.Log("EnemyAttackAction: Attack action cancelled");
            
            // Ensure hitbox is disabled when action is cancelled
            if (_hasActivatedHitbox)
            {
                enemy.DisableHitbox();
            }
            
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
            }
            else
            {
                Debug.LogWarning("EnemyAttackAction: Attack failed to start");
                _isFinished = true; // End action if attack couldn't start
            }
        }
    }
}
