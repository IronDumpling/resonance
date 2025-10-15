using UnityEngine;
using Resonance.Enemies.Core;
using Resonance.Interfaces.Operations;
using Resonance.Utilities;
using System.Collections.Generic;

namespace Resonance.Enemies.Actions
{
    /// <summary>
    /// Enemy core attack action - attacks player's core health
    /// Only executed when player is stunned
    /// Deals CoreHealth damage to break player's crystal core
    /// </summary>
    public class EnemyCoreAttackAction : IEnemyAction
    {
        private float _actionTimer = 0f;
        private bool _isFinished = false;
        private bool _hasTriggeredAnimation = false;
        private bool _hasActivatedHitbox = false;
        private bool _windowOpened = false;

        private EnemyController _enemy;

        public string Name => "CoreAttack";
        public int Priority => 95; // Higher priority than normal attack
        public bool CanInterrupt => true;
        public bool IsFinished => _isFinished;

        public bool CanStart(EnemyController enemy)
        {
            // Can only core attack if:
            // 1. Enemy is alive and can attack
            // 2. Has player target in attack range
            // 3. Player is stunned
            // 4. Not on attack cooldown
            bool canAttack = enemy.CanAttack;
            bool hasTarget = enemy.HasPlayerTarget;
            bool inRange = enemy.IsPlayerInAttackRange();
            
            // Check if player is stunned (requires player service)
            bool playerStunned = IsPlayerStunned(enemy);
            
            bool result = canAttack && hasTarget && inRange && playerStunned;
            
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
            
            Debug.Log("EnemyCoreAttackAction: Started core attack action - targeting player's core");
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
            Debug.Log("EnemyCoreAttackAction: Core attack action cancelled");
            
            // Ensure hitbox is disabled when action is cancelled
            if (_hasActivatedHitbox)
            {
                enemy.DisableHitbox();
            }
            
            _isFinished = true;
        }

        public void OnDamageTaken(EnemyController enemy)
        {
            // Core attack continues even when taking damage
        }

        private void HandleWindowOpened()
        {
            Debug.Log("EnemyCoreAttackAction: Core attack window opened");
            _windowOpened = true;
        }

        private void HandleWindowClosed()
        {
            Debug.Log("EnemyCoreAttackAction: Core attack window closed");
            _windowOpened = false;
        }

        private void HandleSequenceFinished()
        {
            Debug.Log("EnemyCoreAttackAction: Core attack sequence finished");
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

        /// <summary>
        /// Check if player is currently stunned
        /// </summary>
        private bool IsPlayerStunned(EnemyController enemy)
        {
            // Get player service to access player controller
            var playerService = ServiceRegistry.Get<Interfaces.Services.IPlayerService>();
            if (playerService == null || !playerService.HasPlayer) return false;
            
            var playerMonoBehaviour = playerService.CurrentPlayer;
            if (playerMonoBehaviour == null || !playerMonoBehaviour.IsInitialized) return false;
            
            return playerMonoBehaviour.Controller.IsStunned;
        }
    }
}

