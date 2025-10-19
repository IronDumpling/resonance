using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Idle action node - enemy stays still and idles
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyActionBase for component access
    /// - Simple passive behavior
    /// - Returns Running indefinitely (until interrupted by conditions)
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Enemy idles in place")]
    public class IdleAction : EnemyActionBase
    {
        public override void OnStart()
        {
            base.OnStart();
            
            if (!ValidateComponents())
            {
                return;
            }
            
            // Stop all movement
            Movement?.Stop();
            
            // Set animation parameters for idle state
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetFloat("Speed", 0f);
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

            // Just idle - return Running indefinitely
            // External conditions will interrupt this task
            return TaskStatus.Running;
        }
    }
}

