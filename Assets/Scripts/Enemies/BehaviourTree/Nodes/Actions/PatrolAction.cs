using UnityEngine;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Patrol action node - moves around the patrol area at normal speed
    /// Simplified to follow BT design principles:
    /// - No internal condition checking (handled by ConditionNode)
    /// - Focuses only on executing the patrol behavior
    /// </summary>
    public class PatrolAction : ActionNode
    {
        private Vector3 _currentPatrolTarget;
        private float _waitTimer = 0f;
        private bool _hasReachedTarget = false;
        private bool _initialized = false;

        public override BTNodeStatus Execute()
        {
            // Initialize on first execution
            if (!_initialized)
            {
                Initialize();
            }

            // Update animation speed parameter
            var animator = GetAnimator();
            if (animator != null && animator.isActiveAndEnabled)
            {
                float speed = Movement?.Velocity.magnitude ?? 0f;
                animator.SetFloat("Speed", speed);
            }

            // Move towards patrol target
            if (!_hasReachedTarget)
            {
                Movement.SetTarget(_currentPatrolTarget);
                
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
                        return BTNodeStatus.Success;
                    }
                }
            }

            return BTNodeStatus.Running;
        }

        private void Initialize()
        {
            _initialized = true;
            _waitTimer = 0f;
            _hasReachedTarget = false;

            // Generate patrol point
            _currentPatrolTarget = Controller.GeneratePatrolPoint();
            Controller.SetPatrolTarget(_currentPatrolTarget);
            
            // Set animation parameters for patrol state
            var animator = GetAnimator();
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetBool("HasTarget", false);
                animator.SetBool("InAttackRange", false);
            }
        }

        /// <summary>
        /// Reset patrol state for next execution
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Controller.StopPatrol();
            Movement?.Stop();
            _initialized = false;
            _hasReachedTarget = false;
            _waitTimer = 0f;
        }
    }
}
