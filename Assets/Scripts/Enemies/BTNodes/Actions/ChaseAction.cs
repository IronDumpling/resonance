using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Chase action node - moves towards the player at chase move speed
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyActionBase for component access
    /// - No internal condition checking (handled by Conditional nodes)
    /// - Focuses only on executing the chase behavior
    /// - Returns Running while chasing, Success/Failure never (external conditions control)
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Chases the player target, updating position periodically")]
    public class ChaseAction : EnemyActionBase
    {
        private float _updateTimer = 0f;

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
                float speed = Movement?.Velocity.magnitude ?? 0f;
                Animator.SetFloat("Speed", speed);
            }

            // Update target position periodically
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= Controller.TargetUpdateInterval)
            {
                _updateTimer = 0f;
            }

            // Get current target position
            Vector3 targetPosition = Controller.HasPlayerTarget 
                ? Controller.PlayerTarget.position 
                : Controller.LastKnownPlayerPosition;
            
            // Move towards target
            Movement?.SetTarget(targetPosition);

            // Continue chasing (conditions are checked externally)
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            // Cleanup when task ends
            Movement?.Stop();
            _updateTimer = 0f;
        }
    }
}
