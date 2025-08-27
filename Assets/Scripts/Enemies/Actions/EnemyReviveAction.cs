using UnityEngine;
using Resonance.Interfaces.Objects;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.Actions
{
    /// <summary>
    /// Enemy revive action - handles the revival process when physical health reaches 0
    /// Only executed in Reviving state, restores physical health over time
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
            // 2. Mental health > 0 (still has consciousness)
            // 3. Currently in reviving state
            return !enemy.IsPhysicallyAlive && 
                   enemy.IsMentallyAlive && 
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
            
            // Check if mental health dropped to 0 during revival (interruption)
            if (!enemy.IsMentallyAlive)
            {
                Debug.Log("EnemyReviveAction: Revival interrupted - mental health reached 0");
                _isFinished = true;
                return;
            }
            
            // Revival is handled by EnemyController.UpdateRevivalTimer()
            // This action mainly serves as a state indicator and behavior controller
            
            // Check if revival is complete (physical health restored)
            if (enemy.IsPhysicallyAlive)
            {
                Debug.Log("EnemyReviveAction: Revival completed - physical health restored");
                _isFinished = true;
                return;
            }
            
            // Safety timeout - if revival takes too long
            if (_reviveTimer > enemy.Stats.revivalDuration * 3f)
            {
                Debug.LogWarning("EnemyReviveAction: Revival timeout - forcing completion");
                enemy.Stats.RestorePhysicalHealth();
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
            if (enemy.Stats.maxPhysicalHealth <= 0f) return 0f;
            return enemy.Stats.currentPhysicalHealth / enemy.Stats.maxPhysicalHealth;
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
            
            float healthRemaining = enemy.Stats.maxPhysicalHealth - enemy.Stats.currentPhysicalHealth;
            return healthRemaining / enemy.Stats.revivalRate;
        }
    }
}
