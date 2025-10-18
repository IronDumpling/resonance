using UnityEngine;
using System.Collections.Generic;
using Resonance.Core;
using Resonance.Enemies.Core;
using Resonance.Interfaces.Operations;
using Resonance.Utilities;

namespace Resonance.Enemies.Actions
{
    /// <summary>
    /// Enemy wave attack action - attacks player's core health
    /// Only executed when player is stunned
    /// Deals CoreHealth damage to break player's crystal core
    /// </summary>
    public class EnemyWaveAttackAction : IEnemyAction
    {
        private float _actionTimer = 0f;
        private bool _isFinished = false;
        private bool _hasTriggeredAnimation = false;
        private bool _hasActivatedHitbox = false;
        private bool _windowOpened = false;

        private EnemyController _enemy;

        public string Name => "WaveAttack";
        public int Priority => 95; // Higher priority than normal attack
        public bool CanInterrupt => true;
        public bool IsFinished => _isFinished;

        public bool CanStart(EnemyController enemy)
        {
            // Can only wave attack if:
            // 1. Enemy has cooldown ready, is alive, has core alive, and target is in range
            // 2. Triggered by: player in chaos state OR every 3rd normal attack
            bool canWaveAttack = enemy.CanWaveAttack;
            bool inRange = enemy.IsPlayerInAttackRange();
            
            bool result = canWaveAttack && inRange;
            
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
                if(_enemy.LaunchWaveAttack())
                {
                    _hasTriggeredAnimation = true;
                }
                else
                {
                    Finish();
                }
            }
            
            Debug.Log("EnemyWaveAttackAction: Started wave attack action - targeting player's core");
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
            Debug.Log("EnemyWaveAttackAction: Wave attack action cancelled (e.g. by stun)");
            
            // Ensure hitbox is disabled when action is cancelled
            if (_hasActivatedHitbox)
            {
                enemy.DisableHitbox();
            }
            
            // Clean up event subscriptions
            if (_enemy != null)
            {
                _enemy.OnAttackSequenceFinished -= () => HandleSequenceFinished();
                _enemy.OnAttackWindowOpened -= () => HandleWindowOpened();
                _enemy.OnAttackWindowClosed -= () => HandleWindowClosed();
            }
            
            _isFinished = true;
        }

        public void OnDamageTaken(EnemyController enemy)
        {
            // Wave attack continues even when taking damage
        }

        private void HandleWindowOpened()
        {
            Debug.Log("EnemyWaveAttackAction: Wave attack window opened");
            _windowOpened = true;
        }

        private void HandleWindowClosed()
        {
            Debug.Log("EnemyWaveAttackAction: Wave attack window closed");
            _windowOpened = false;
        }

        private void HandleSequenceFinished()
        {
            Debug.Log("EnemyWaveAttackAction: Wave attack sequence finished");
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

