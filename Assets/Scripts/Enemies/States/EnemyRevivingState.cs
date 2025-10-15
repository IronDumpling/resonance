using UnityEngine;
using Resonance.Core;
using Resonance.Enemies.Core;
using Resonance.Enemies.Actions;

namespace Resonance.Enemies.States
{
    /// <summary>
    /// Enemy复活状态, 物理血量缓慢恢复
    /// 核心保持暴露, 易受精神攻击
    /// </summary>
    public class EnemyRevivingState : IState
    {
        private EnemyController _enemyController;
        private float _revivalTimer = 0f;
        
        public string Name => "Reviving";

        public EnemyRevivingState(EnemyController enemyController)
        {
            _enemyController = enemyController;
        }

        public void Enter()
        {
            Debug.Log("EnemyState: Entered Reviving state - health health recovering");
            
            _revivalTimer = 0f;
            
            // Continue to disable movement and AI
            _enemyController.StopPatrol();
            _enemyController.LosePlayer();
            
            // Start the revive action
            var reviveAction = new EnemyReviveAction();
            _enemyController.ActionController.RegisterAction(reviveAction);
            _enemyController.ActionController.TryStartAction("Revive");
            
            // TODO: Play revival audio
            
            Debug.Log("EnemyState: Revival in progress - core still exposed");
        }

        public void Update()
        {
            _revivalTimer += Time.deltaTime;
            
            // Check for revival interruption - if core health reaches 0 during revival
            if (!_enemyController.IsCoreAlive)
            {
                Debug.Log("EnemyRevivingState: Revival interrupted - core health reached 0");
                // This will trigger Normal State or TrueDeath State transition handled by EnemyController
                return;
            }
            
            // Check if revival duration exceeded (safety check)
            if (_revivalTimer > _enemyController.Stats.revivalDuration * 2f)
            {
                Debug.LogWarning("EnemyRevivingState: Revival taking too long, forcing completion");
                _enemyController.Stats.FullRestore();
            }
        }

        public void Exit()
        {
            Debug.Log("EnemyState: Exited Reviving state");
            
            // Cleanup revive action
            _enemyController.ActionController.UnregisterAction("Revive");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition to:
            // - Normal (when health health is restored and core health > 0)
            // - TrueDeath (when core health reaches 0)
            return newState.Name == "Normal" || newState.Name == "TrueDeath";
        }

        /// <summary>
        /// Get revival progress (0-1)
        /// </summary>
        public float GetRevivalProgress()
        {
            if (_enemyController.Stats.maxHealth <= 0f) return 0f;
            return _enemyController.Stats.currentHealth / _enemyController.Stats.maxHealth;
        }

        /// <summary>
        /// Get time spent in revival
        /// </summary>
        public float GetRevivalTime()
        {
            return _revivalTimer;
        }

        /// <summary>
        /// Get estimated time remaining for revival
        /// </summary>
        public float GetEstimatedTimeRemaining()
        {
            if (_enemyController.Stats.revivalRate <= 0f) return float.MaxValue;
            
            float healthRemaining = _enemyController.Stats.maxHealth - _enemyController.Stats.currentHealth;
            return healthRemaining / _enemyController.Stats.revivalRate;
        }
    }
}
