using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Patrol action node - moves around the patrol area using NavMeshAgent
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyActionBase for component access
    /// - No internal condition checking (handled by Conditional nodes)
    /// - Focuses only on executing the patrol behavior
    /// - Returns Running while patrolling
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Patrols between waypoints or random positions using NavMeshAgent")]
    public class PatrolAction : EnemyActionBase
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
            
            // Set NavMesh destination
            if (NavAgent != null && NavAgent.isOnNavMesh)
            {
                NavAgent.isStopped = false;
                NavAgent.SetDestination(_currentPatrolTarget);
            }
            
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
                float speed = NavAgent != null ? NavAgent.velocity.magnitude : 0f;
                Animator.SetFloat("Speed", speed);
            }

            // Move towards patrol target
            if (!_hasReachedTarget)
            {
                // Check if NavAgent is ready and has reached destination
                if (NavAgent != null && NavAgent.isOnNavMesh)
                {
                    // Check if arrived at destination
                    if (!NavAgent.pathPending && NavAgent.remainingDistance <= NavAgent.stoppingDistance)
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
                        
                        // Set new destination
                        if (NavAgent != null && NavAgent.isOnNavMesh)
                        {
                            NavAgent.SetDestination(_currentPatrolTarget);
                        }
                        
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
            
            // Stop NavMeshAgent
            if (NavAgent != null && NavAgent.isOnNavMesh)
            {
                NavAgent.isStopped = true;
                NavAgent.ResetPath();
            }
            
            _hasReachedTarget = false;
            _waitTimer = 0f;
        }
    }
}
