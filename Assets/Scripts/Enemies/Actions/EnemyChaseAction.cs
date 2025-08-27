using UnityEngine;
using Resonance.Interfaces.Objects;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.Actions
{
    /// <summary>
    /// Enemy chase action - moves towards the player at alert move speed
    /// Only executed in Normal state's Alert sub-state when player is detected
    /// </summary>
    public class EnemyChaseAction : IEnemyAction
    {
        private bool _isFinished = false;
        private Vector3 _targetPosition;
        private float _updateTimer = 0f;
        
        public string Name => "Chase";
        public int Priority => 70; // Medium-high priority
        public bool CanInterrupt => true;
        public bool IsFinished => _isFinished;

        public bool CanStart(EnemyController enemy)
        {
            // Can chase if:
            // 1. Enemy is alive and can move
            // 2. Has player target
            // 3. Player is in detection range but not in attack range
            return enemy.IsPhysicallyAlive && 
                   enemy.HasPlayerTarget && 
                   enemy.IsPlayerInDetectionRange() &&
                   !enemy.IsPlayerInAttackRange() &&
                   enemy.Stats.GetModifiedAlertMoveSpeed() > 0f;
        }

        public void Start(EnemyController enemy)
        {
            _isFinished = false;
            _updateTimer = 0f;
            
            // Set initial target to player's current position
            if (enemy.HasPlayerTarget)
            {
                _targetPosition = enemy.PlayerTarget.position;
            }
            else
            {
                _targetPosition = enemy.LastKnownPlayerPosition;
            }
            
            Debug.Log($"EnemyChaseAction: Started chasing towards {_targetPosition}");
        }

        public void Update(EnemyController enemy, float deltaTime)
        {
            _updateTimer += deltaTime;
            
            // Update target position periodically
            if (_updateTimer >= enemy.TargetUpdateInterval)
            {
                _updateTimer = 0f;
                UpdateTargetPosition(enemy);
            }
            
            // Move towards target
            MoveTowardsTarget(enemy, deltaTime);
            
            // Check finish conditions
            CheckFinishConditions(enemy);
        }

        public void Cancel(EnemyController enemy)
        {
            Debug.Log("EnemyChaseAction: Chase action cancelled");
            _isFinished = true;
        }

        public void OnDamageTaken(EnemyController enemy)
        {
            // Continue chasing even when taking damage
            // Could add evasive behavior here if needed
        }

        /// <summary>
        /// Update the target position based on player location
        /// </summary>
        private void UpdateTargetPosition(EnemyController enemy)
        {
            if (enemy.HasPlayerTarget)
            {
                _targetPosition = enemy.PlayerTarget.position;
            }
            else
            {
                // Use last known position if player is no longer detected
                _targetPosition = enemy.LastKnownPlayerPosition;
            }
        }

        /// <summary>
        /// Move the enemy towards the target position
        /// </summary>
        private void MoveTowardsTarget(EnemyController enemy, float deltaTime)
        {
            // Use the movement system to chase the player
            var movement = enemy.Movement;
            float moveSpeed = enemy.Stats.GetModifiedAlertMoveSpeed();
            
            // Set movement target and move towards player
            movement.SetTarget(_targetPosition);
            movement.MoveToTarget(moveSpeed, deltaTime);
            
            // Debug.Log($"EnemyChaseAction: Chasing player at {_targetPosition}, speed: {moveSpeed:F1}");
        }

        /// <summary>
        /// Check if the action should finish
        /// </summary>
        private void CheckFinishConditions(EnemyController enemy)
        {
            // Finish if player enters attack range (switch to attack)
            if (enemy.IsPlayerInAttackRange())
            {
                Debug.Log("EnemyChaseAction: Player in attack range, finishing chase");
                _isFinished = true;
                return;
            }
            
            // Finish if player is no longer detected
            if (!enemy.HasPlayerTarget || !enemy.IsPlayerInDetectionRange())
            {
                Debug.Log("EnemyChaseAction: Player lost, finishing chase");
                _isFinished = true;
                return;
            }
            
            // Finish if enemy can no longer move (wounded/dead)
            if (enemy.Stats.GetModifiedAlertMoveSpeed() <= 0f)
            {
                Debug.Log("EnemyChaseAction: Cannot move, finishing chase");
                _isFinished = true;
                return;
            }
            
            // Check if arrived at target position
            var movement = enemy.Movement;
            float distanceToTarget = movement.GetDistanceToTarget();
            
            if (distanceToTarget <= enemy.ChaseArrivalThreshold)
            {
                Debug.Log("EnemyChaseAction: Arrived at target position");
                // Don't finish immediately - keep chasing as target updates
            }
        }
    }
}
