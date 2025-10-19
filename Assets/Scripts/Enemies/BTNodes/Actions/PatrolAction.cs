using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Patrol action node - moves around the patrol area at normal speed
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyTaskBase for component access
    /// - No internal condition checking (handled by Conditional nodes)
    /// - Focuses only on executing the patrol behavior
    /// - Returns Running while patrolling
    /// </summary>
    [TaskCategory("Resonance/Enemy/Actions")]
    [TaskDescription("Patrols between waypoints or random positions")]
    public class PatrolAction : EnemyTaskBase
    {
        private Vector3 _currentPatrolTarget;
        private float _waitTimer = 0f;
        private bool _hasReachedTarget = false;

        public override void OnStart()
        {
            base.OnStart();
            
            if (!ValidateComponents())
            {
                return;
            }
            
            // Initialize patrol
            _waitTimer = 0f;
            _hasReachedTarget = false;

            // Generate patrol point
            _currentPatrolTarget = Controller.GeneratePatrolPoint();
            Controller.SetPatrolTarget(_currentPatrolTarget);
            
            // Set animation parameters for patrol state
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("HasTarget", false);
                Animator.SetBool("InAttackRange", false);
            }
        }

        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            // Update animation speed parameter
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                float speed = Movement?.Velocity.magnitude ?? 0f;
                Animator.SetFloat("Speed", speed);
            }

            // Move towards patrol target
            if (!_hasReachedTarget)
            {
                Movement?.SetTarget(_currentPatrolTarget);
                
                // Check if arrived
                float distanceToTarget = Movement.GetDistanceToTarget();
                if (distanceToTarget <= Controller.Stats.arrivalThreshold)
                {
                    _hasReachedTarget = true;
                    _waitTimer = 0f;
                    
                    // Switch direction if using waypoint system
                    if (Controller.HasPatrolWaypoints())
                    {
                        Controller.SwitchPatrolDirection();
                    }
                }
            }
            else
            {
                // Wait at waypoint
                _waitTimer += Time.deltaTime;
                
                if (_waitTimer >= Controller.WaitAtWaypointDuration)
                {
                    if (Controller.HasPatrolWaypoints())
                    {
                        // Move to next waypoint
                        _currentPatrolTarget = Controller.GeneratePatrolPoint();
                        Controller.SetPatrolTarget(_currentPatrolTarget);
                        _hasReachedTarget = false;
                        _waitTimer = 0f;
                    }
                    else
                    {
                        // Random patrol completed
                        return TaskStatus.Success;
                    }
                }
            }

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            // Cleanup when task ends
            if (Controller != null)
            {
                Controller.StopPatrol();
            }
            Movement?.Stop();
            _hasReachedTarget = false;
            _waitTimer = 0f;
        }
    }
}
