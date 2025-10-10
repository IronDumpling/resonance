using UnityEngine;
using Resonance.Enemies.Core;
using Resonance.Interfaces.Operations;

namespace Resonance.Enemies.Actions
{
    /// <summary>
    /// Enemy revive action - handles the revival process when health health reaches 0
    /// Only executed in Reviving state, restores health health over time
    /// </summary>
    public class EnemyReviveAction : IEnemyAction
    {
        private bool _isFinished = false;
        private float _reviveTimer = 0f;
        
        public string Name => "Revive";
        public int Priority => 100; // Highest priority - cannot be interrupted
        public bool CanInterrupt => false; // Revival cannot be interrupted by other actions
        public bool IsFinished => _isFinished;

        public bool CanStart(EnemyController enemy)
        {
            // Can only start revival if:
            // 1. Physical health is 0 (dead)
            // 2. Core health > 0 (still has consciousness)
            // 3. Currently in reviving state
            return !enemy.IsAlive && 
                   enemy.IsCoreAlive && 
                   enemy.StateMachine.IsInState("Reviving");
        }

        public void Start(EnemyController enemy)
        {
            _isFinished = false;
            _reviveTimer = 0f;
            
            Debug.Log("EnemyReviveAction: Started revival process");
            
            // Ensure enemy stops all movement and other behaviors
            enemy.StopPatrol();
            enemy.LosePlayer();
        }

        public void Update(EnemyController enemy, float deltaTime)
        {
            _reviveTimer += deltaTime;
            
            // Check if core health dropped to 0 during revival (interruption)
            if (!enemy.IsCoreAlive)
            {
                Debug.Log("EnemyReviveAction: Revival interrupted - core health reached 0");
                _isFinished = true;
                return;
            }
            
            // Check if revival is complete (health health restored)
            if (enemy.IsAlive)
            {
                Debug.Log("EnemyReviveAction: Revival completed - health health restored");
                _isFinished = true;
                return;
            }
            
            // Safety timeout - if revival takes too long
            if (_reviveTimer > enemy.Stats.revivalDuration * 3f)
            {
                Debug.LogWarning("EnemyReviveAction: Revival timeout - forcing completion");
                enemy.Stats.FullRestore();
                _isFinished = true;
                return;
            }
        }

        public void Cancel(EnemyController enemy)
        {
            Debug.Log("EnemyReviveAction: Revival action cancelled");
            _isFinished = true;
        }

        public void OnDamageTaken(EnemyController enemy)
        {
            // Revival continues even when taking damage
            // The damage will be processed by the controller
            Debug.Log("EnemyReviveAction: Taking damage during revival");
        }

        /// <summary>
        /// Get revival progress (0-1)
        /// </summary>
        public float GetRevivalProgress(EnemyController enemy)
        {
            if (enemy.Stats.maxHealth <= 0f) return 0f;
            return enemy.Stats.currentHealth / enemy.Stats.maxHealth;
        }

        /// <summary>
        /// Get time spent in revival
        /// </summary>
        public float GetRevivalTime()
        {
            return _reviveTimer;
        }

        /// <summary>
        /// Get estimated time remaining for revival
        /// </summary>
        public float GetEstimatedTimeRemaining(EnemyController enemy)
        {
            if (enemy.Stats.revivalRate <= 0f) return float.MaxValue;
            
            float healthRemaining = enemy.Stats.maxHealth - enemy.Stats.currentHealth;
            return healthRemaining / enemy.Stats.revivalRate;
        }
    }
}
