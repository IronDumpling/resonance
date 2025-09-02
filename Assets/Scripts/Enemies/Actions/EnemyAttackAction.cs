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
        private bool _windowOpened = false;

        private EnemyController _enemy;

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
            // bool isMentallyAlive = enemy.IsMentallyAlive;
            // bool hasPlayerTargetForAttack = enemy.HasPlayerTarget;
            // float currentTime = Time.time;
            // float lastAttackTime = enemy.LastAttackTime;
            // float attackCooldown = enemy.AttackCooldownValue;
            // bool cooldownPassed = currentTime >= (lastAttackTime + attackCooldown);
            
            bool result = canAttack && hasTarget && inRange;
            
            // Optional: Keep minimal logging for important events
            // if (!result && !cooldownPassed)
            // {
                // Debug.Log($"EnemyAttackAction: Attack on cooldown - {(lastAttackTime + attackCooldown - currentTime):F1}s remaining");
            // }
            
            return result;
        }

        public void Start(EnemyController enemy)
        {
            _enemy = enemy;
            _actionTimer = 0f;
            _isFinished = false;
            _windowOpened = false;
            _hasTriggeredAnimation = false;
            _hasActivatedHitbox = false;

            _enemy.OnAttackSequenceFinished += () => HandleSequenceFinished();
            _enemy.OnAttackWindowOpened += () => HandleWindowOpened();
            _enemy.OnAttackWindowClosed += () => HandleWindowClosed();

            if(!_hasTriggeredAnimation)
            {
                if(_enemy.LaunchAttack())
                {
                    _hasTriggeredAnimation = true;
                }
                else
                {
                    Finish();
                }
            }
            
            Debug.Log("EnemyAttackAction: Started attack action - will trigger animation");
        }

        public void Update(EnemyController enemy, float deltaTime)
        {
            _actionTimer += deltaTime;

            if(!_windowOpened && (!enemy.HasPlayerTarget || !enemy.IsPlayerInAttackRange()))
            {
                Finish();
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

        private void HandleWindowOpened()
        {
            Debug.Log("EnemyAttackAction: Attack window opened");
            _windowOpened = true;
        }

        private void HandleWindowClosed()
        {
            Debug.Log("EnemyAttackAction: Attack window closed");
            _windowOpened = false;
        }

        private void HandleSequenceFinished()
        {
            Debug.Log("EnemyAttackAction: Attack sequence finished");
            Finish();
        }

        private void Finish()
        {
            if (_isFinished) return;
            _isFinished = true;
            if(_enemy != null)
            {
                _enemy.OnAttackSequenceFinished -= () => HandleSequenceFinished();
                _enemy.OnAttackWindowOpened -= () => HandleWindowOpened();
                _enemy.OnAttackWindowClosed -= () => HandleWindowClosed();
            }
        }
    }
}
