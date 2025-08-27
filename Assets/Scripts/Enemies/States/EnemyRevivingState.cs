using UnityEngine;
using Resonance.Core;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.States
{
    /// <summary>
    /// Enemy复活状态，物理血量缓慢恢复
    /// 核心保持暴露，易受精神攻击
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
            Debug.Log("EnemyState: Entered Reviving state - physical health recovering");
            
            _revivalTimer = 0f;
            
            // Continue to disable movement and AI
            _enemyController.StopPatrol();
            _enemyController.LosePlayer();
            
            // TODO: Visual effects for revival process
            // TODO: Play revival audio
            
            Debug.Log("EnemyState: Revival in progress - core still exposed");
        }

        public void Update()
        {
            _revivalTimer += Time.deltaTime;
            
            // Check for revival interruption - if mental health reaches 0 during revival
            if (!_enemyController.IsMentallyAlive)
            {
                Debug.Log("EnemyRevivingState: Revival interrupted - mental health reached 0");
                // This will trigger true death transition handled by EnemyController
                return;
            }
            
            // Revival progress is handled in EnemyController.UpdateRevivalTimer()
            // The controller will call CompleteRevival() when physical health is full
            
            // Check if revival duration exceeded (safety check)
            if (_revivalTimer > _enemyController.Stats.revivalDuration * 2f)
            {
                Debug.LogWarning("EnemyRevivingState: Revival taking too long, forcing completion");
                _enemyController.Stats.RestorePhysicalHealth();
            }
        }

        public void Exit()
        {
            Debug.Log("EnemyState: Exited Reviving state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition to:
            // - Normal (when physical health is restored and mental health > 0)
            // - TrueDeath (when mental health reaches 0)
            return newState.Name == "Normal" || newState.Name == "TrueDeath";
        }

        /// <summary>
        /// Get revival progress (0-1)
        /// </summary>
        public float GetRevivalProgress()
        {
            if (_enemyController.Stats.maxPhysicalHealth <= 0f) return 0f;
            return _enemyController.Stats.currentPhysicalHealth / _enemyController.Stats.maxPhysicalHealth;
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
            
            float healthRemaining = _enemyController.Stats.maxPhysicalHealth - _enemyController.Stats.currentPhysicalHealth;
            return healthRemaining / _enemyController.Stats.revivalRate;
        }
    }
}
