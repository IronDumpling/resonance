using UnityEngine;
using Resonance.Interfaces.Objects;
using Resonance.Enemies;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.Actions
{
    /// <summary>
    /// Enemy patrol action - moves around the patrol area at normal speed
    /// Only executed in Normal state's Normal sub-state when no player is detected
    /// </summary>
    public class EnemyPatrolAction : IEnemyAction
    {
        private bool _isFinished = false;
        private Vector3 _currentPatrolTarget;
        private float _patrolTimer = 0f;
        private bool _hasReachedTarget = false;
        
        public string Name => "Patrol";
        public int Priority => 10; // Low priority - easily interrupted
        public bool CanInterrupt => true;
        public bool IsFinished => _isFinished;

        public bool CanStart(EnemyController enemy)
        {
            // Can patrol if:
            // 1. Enemy is alive and can move
            // 2. No player detected
            // 3. Not currently patrolling to avoid overlapping patrol actions
            // 4. Haven't exceeded patrol cycle limit (for Limited mode)
            return enemy.IsPhysicallyAlive && 
                   !enemy.HasPlayerTarget && 
                   !enemy.IsPatrolling &&
                   enemy.Stats.GetModifiedMoveSpeed() > 0f &&
                   !enemy.ShouldStopPatrol();
        }

        public void Start(EnemyController enemy)
        {
            _isFinished = false;
            _patrolTimer = 0f;
            _hasReachedTarget = false;
            
            // Reset patrol cycles if starting fresh
            if (enemy.CurrentPatrolCycles == 0)
            {
                enemy.ResetPatrolCycles();
            }
            
            // Generate a new patrol point (uses waypoints if available)
            _currentPatrolTarget = enemy.GeneratePatrolPoint();
            enemy.SetPatrolTarget(_currentPatrolTarget);
            
            Debug.Log($"EnemyPatrolAction: Started patrolling to {_currentPatrolTarget} (Mode: {enemy.EnemyPatrolMode}, Cycle: {enemy.CurrentPatrolCycles}/{enemy.MaxPatrolCycles})");
        }

        public void Update(EnemyController enemy, float deltaTime)
        {
            _patrolTimer += deltaTime;
            
            // Move towards patrol target
            if (!_hasReachedTarget)
            {
                MoveTowardsPatrolTarget(enemy, deltaTime);
                CheckArrival(enemy);
            }
            
            // Check finish conditions
            CheckFinishConditions(enemy);
        }

        public void Cancel(EnemyController enemy)
        {
            Debug.Log("EnemyPatrolAction: Patrol action cancelled");
            enemy.StopPatrol();
            _isFinished = true;
        }

        public void OnDamageTaken(EnemyController enemy)
        {
            // Patrol action is interrupted by damage (low priority)
            Debug.Log("EnemyPatrolAction: Interrupted by damage");
            Cancel(enemy);
        }

        /// <summary>
        /// Move the enemy towards the patrol target
        /// Speed is now managed centrally by EnemyMovement based on current state
        /// </summary>
        private void MoveTowardsPatrolTarget(EnemyController enemy, float deltaTime)
        {
            // Use the movement system to move towards patrol target
            // Speed will be automatically determined by EnemyMovement based on current substate
            var movement = enemy.Movement;
            
            // Set movement target - speed will be handled automatically
            movement.SetTarget(_currentPatrolTarget);
            
            // Debug.Log($"EnemyPatrolAction: Patrolling towards {_currentPatrolTarget}");
        }

        /// <summary>
        /// Check if the enemy has arrived at the patrol target
        /// </summary>
        private void CheckArrival(EnemyController enemy)
        {
            // Use movement system to check if we're close to target
            var movement = enemy.Movement;
            float distanceToTarget = movement.GetDistanceToTarget();
            
            // Use configured arrival threshold
            if (distanceToTarget <= enemy.ArrivalThreshold)
            {
                // Movement system stopped because we reached the target
                if (!_hasReachedTarget)
                {
                    _hasReachedTarget = true;
                    Debug.Log($"EnemyPatrolAction: Arrived at patrol point (distance: {distanceToTarget:F2})");
                    
                    // If using waypoint system, switch direction and set next target
                    if (enemy.HasPatrolWaypoints())
                    {
                        enemy.SwitchPatrolDirection();
                        // Don't immediately set new target, wait for the wait duration
                    }
                }
            }
        }

        /// <summary>
        /// Check if the action should finish
        /// </summary>
        private void CheckFinishConditions(EnemyController enemy)
        {
            // Finish if player is detected (higher priority action should take over)
            if (enemy.HasPlayerTarget)
            {
                Debug.Log("EnemyPatrolAction: Player detected, finishing patrol");
                enemy.StopPatrol();
                _isFinished = true;
                return;
            }
            
            // Finish if enemy can no longer move
            if (enemy.Stats.GetModifiedMoveSpeed() <= 0f)
            {
                Debug.Log("EnemyPatrolAction: Cannot move, finishing patrol");
                enemy.StopPatrol();
                _isFinished = true;
                return;
            }
            
            // Check if we should stop patrolling (Limited mode)
            if (enemy.ShouldStopPatrol())
            {
                Debug.Log($"EnemyPatrolAction: Reached maximum patrol cycles ({enemy.CurrentPatrolCycles}/{enemy.MaxPatrolCycles}), stopping");
                enemy.StopPatrol();
                _isFinished = true;
                return;
            }
            
            // Finish after wait duration if reached target and waited
            if (_hasReachedTarget && _patrolTimer >= enemy.WaitAtWaypointDuration)
            {
                if (enemy.HasPatrolWaypoints())
                {
                    // For waypoint patrol, start moving to next waypoint
                    _currentPatrolTarget = enemy.GeneratePatrolPoint();
                    enemy.SetPatrolTarget(_currentPatrolTarget);
                    _hasReachedTarget = false;
                    _patrolTimer = 0f; // Reset timer for next leg
                    Debug.Log($"EnemyPatrolAction: Moving to next waypoint: {_currentPatrolTarget}");
                }
                else
                {
                    // For random patrol, finish the action
                    Debug.Log("EnemyPatrolAction: Random patrol completed");
                    enemy.StopPatrol();
                    _isFinished = true;
                }
                return;
            }
            
            // Safety timeout - finish if taking too long (twice the single cycle duration)
            if (_patrolTimer >= enemy.SingleCycleDuration * 2f)
            {
                Debug.LogWarning($"EnemyPatrolAction: Patrol timeout after {_patrolTimer:F1}s (expected: {enemy.SingleCycleDuration:F1}s), finishing");
                enemy.StopPatrol();
                _isFinished = true;
                return;
            }
        }
    }
}
