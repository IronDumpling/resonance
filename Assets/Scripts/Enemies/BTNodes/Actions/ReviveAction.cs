using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Revive action node - handles the revival process when physical health reaches 0
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyActionBase for component access
    /// - No internal condition checking (handled by Conditional nodes)
    /// - Focuses only on executing the revival behavior
    /// - Returns Running while reviving, Success when complete
    /// - Restores physical health over time
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Revives the enemy when physical health depletes but core is intact")]
    public class ReviveAction : EnemyActionBase
    {
        private float _reviveTimer = 0f;
        private float _maxReviveTime;

        public override void OnStart()
        {
            base.OnStart();
            
            if (!ValidateComponents())
            {
                return;
            }
            
            _reviveTimer = 0f;
            _maxReviveTime = 3f * Controller.Stats.maxHealth / Controller.Stats.revivalRate;

            // Stop all movement and behaviors
            Controller.StopPatrol();
            Controller.LosePlayer();
            Movement?.Stop();
            
            // Set revival animation state
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                // Trigger PhysicalDeath animation first (if needed by Animator)
                // This transition: Locomotion → PhysicalDeath → Revival
                // If Animator doesn't need PhysicalDeath trigger, can remove this line
                Animator.SetTrigger("PhysicalDeath");
                
                // Then set revival state parameters
                Animator.SetBool("IsReviving", true);      // Enter revival state
                Animator.SetFloat("Speed", 0f);            // No movement during revival
                Animator.SetBool("HasTarget", false);      // No target during revival
                Animator.SetBool("InAttackRange", false);  // Cannot attack during revival
            }
            else
            {
                Debug.LogWarning("[BT Action] ReviveAction: Animator not available!");
            }
        }

        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            _reviveTimer += Time.deltaTime;

            // Check if revival is complete (physical health restored)
            if (Controller.IsPhysicallyAlive)
            {
                // Complete revival animation
                if (Animator != null && Animator.isActiveAndEnabled)
                {
                    Animator.SetBool("IsReviving", false);
                    Animator.SetTrigger("ReviveComplete");
                }
                
                return TaskStatus.Success;
            }

            // Safety timeout
            if (_reviveTimer > _maxReviveTime)
            {
                Controller.Stats.FullRestore();
                
                // Complete revival animation
                if (Animator != null && Animator.isActiveAndEnabled)
                {
                    Animator.SetBool("IsReviving", false);
                    Animator.SetTrigger("ReviveComplete");
                }
                
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            // Clean up animation state - allow Animator to return to normal
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("IsReviving", false);
            }
            
            _reviveTimer = 0f;
        }
    }
}
