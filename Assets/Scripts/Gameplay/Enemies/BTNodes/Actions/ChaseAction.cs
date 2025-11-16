using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Chase action node - moves towards the player using NavMeshAgent
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyActionBase for component access
    /// - No internal condition checking (handled by Conditional nodes)
    /// - Focuses only on executing the chase behavior
    /// - Returns Running while chasing, Success/Failure never (external conditions control)
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Chases the player target using NavMeshAgent")]
    public class ChaseAction : EnemyActionBase
    {
        private float _updateTimer = 0f;
        private const float PATH_UPDATE_INTERVAL = 0.5f; // Update path every 0.5 seconds

        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            // Update animation parameters for chase state
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("HasTarget", true);         // Has player target
                Animator.SetBool("InAttackRange", false);    // NOT in attack range (chasing)
                
                // Update speed parameter based on actual movement
                float speed = NavAgent != null ? NavAgent.velocity.magnitude : 0f;
                Animator.SetFloat("Speed", speed);
            }

            // Update target position periodically
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= PATH_UPDATE_INTERVAL)
            {
                _updateTimer = 0f;
                
                // Get target position from vision system's last known position
                // This allows the enemy to continue chasing even if line of sight is temporarily lost
                Vector3 targetPosition = Controller.LastKnownPlayerPosition;
                
                // Update NavMeshAgent destination
                if (NavAgent != null && NavAgent.isOnNavMesh)
                {
                    NavAgent.isStopped = false;
                    NavAgent.SetDestination(targetPosition);
                }
            }

            // Continue chasing (conditions are checked externally)
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            // Cleanup when task ends
            if (NavAgent != null && NavAgent.isOnNavMesh)
            {
                NavAgent.isStopped = true;
                NavAgent.ResetPath();
            }
            
            _updateTimer = 0f;
        }
    }
}
