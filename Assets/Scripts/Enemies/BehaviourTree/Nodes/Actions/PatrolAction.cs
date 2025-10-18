using UnityEngine;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Patrol action node - moves around the patrol area at normal speed
    /// Based on Legacy EnemyPatrolAction
    /// </summary>
    public class PatrolAction : ActionNode
    {
        private Vector3 _currentPatrolTarget;
        private float _patrolTimer = 0f;
        private bool _hasReachedTarget = false;
        private bool _isActive = false;

        public override BTNodeStatus Execute()
        {
            // Check if can start patrol
            if (!_isActive)
            {
                if (!CanStartPatrol())
                {
                    return BTNodeStatus.Failure;
                }
                StartPatrol();
            }

            // Update patrol
            _patrolTimer += Time.deltaTime;

            // Move towards patrol target
            if (!_hasReachedTarget)
            {
                MoveTowardsPatrolTarget();
                CheckArrival();
            }

            // Check finish conditions
            return CheckFinishConditions();
        }

        private bool CanStartPatrol()
        {
            return Controller.IsAlive &&
                   !Controller.HasPlayerTarget &&
                   !Controller.IsPatrolling &&
                   Controller.Stats.GetModifiedMoveSpeed() > 0f &&
                   !Controller.ShouldStopPatrol();
        }

        private void StartPatrol()
        {
            _isActive = true;
            _patrolTimer = 0f;
            _hasReachedTarget = false;

            // Reset patrol cycles if starting fresh
            if (Controller.CurrentPatrolCycles == 0)
            {
                Controller.ResetPatrolCycles();
            }

            // Generate a new patrol point (uses waypoints if available)
            _currentPatrolTarget = Controller.GeneratePatrolPoint();
            Controller.SetPatrolTarget(_currentPatrolTarget);

            Debug.Log($"PatrolAction: Started patrolling to {_currentPatrolTarget}");
        }

        private void MoveTowardsPatrolTarget()
        {
            Movement.SetTarget(_currentPatrolTarget);
        }

        private void CheckArrival()
        {
            float distanceToTarget = Movement.GetDistanceToTarget();

            if (distanceToTarget <= Controller.Stats.arrivalThreshold)
            {
                if (!_hasReachedTarget)
                {
                    _hasReachedTarget = true;
                    Debug.Log($"PatrolAction: Arrived at patrol point");

                    // If using waypoint system, switch direction
                    if (Controller.HasPatrolWaypoints())
                    {
                        Controller.SwitchPatrolDirection();
                    }
                }
            }
        }

        private BTNodeStatus CheckFinishConditions()
        {
            // Finish if player is detected
            if (Controller.HasPlayerTarget)
            {
                Debug.Log("PatrolAction: Player detected, finishing patrol");
                FinishPatrol();
                return BTNodeStatus.Success;
            }

            // Finish if enemy can no longer move
            if (Controller.Stats.GetModifiedMoveSpeed() <= 0f)
            {
                Debug.Log("PatrolAction: Cannot move, finishing patrol");
                FinishPatrol();
                return BTNodeStatus.Failure;
            }

            // Check if we should stop patrolling (Limited mode)
            if (Controller.ShouldStopPatrol())
            {
                Debug.Log($"PatrolAction: Reached maximum patrol cycles");
                FinishPatrol();
                return BTNodeStatus.Success;
            }

            // Continue patrol after wait duration
            if (_hasReachedTarget && _patrolTimer >= Controller.WaitAtWaypointDuration)
            {
                if (Controller.HasPatrolWaypoints())
                {
                    // Move to next waypoint
                    _currentPatrolTarget = Controller.GeneratePatrolPoint();
                    Controller.SetPatrolTarget(_currentPatrolTarget);
                    _hasReachedTarget = false;
                    _patrolTimer = 0f;
                    Debug.Log($"PatrolAction: Moving to next waypoint: {_currentPatrolTarget}");
                }
                else
                {
                    // Random patrol completed
                    FinishPatrol();
                    return BTNodeStatus.Success;
                }
            }

            // Safety timeout
            if (_patrolTimer >= Controller.SingleCycleDuration * 2f)
            {
                Debug.LogWarning($"PatrolAction: Patrol timeout");
                FinishPatrol();
                return BTNodeStatus.Success;
            }

            return BTNodeStatus.Running;
        }

        private void FinishPatrol()
        {
            Controller.StopPatrol();
            Movement?.Stop();
            _isActive = false;
        }
    }
}
