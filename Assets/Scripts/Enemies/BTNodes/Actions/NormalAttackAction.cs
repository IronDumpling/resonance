using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Normal attack action node - triggers attack animation and manages attack flow
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyTaskBase for component access
    /// - No internal condition checking (handled by Conditional nodes)
    /// - Focuses only on executing the attack behavior
    /// - Returns Running while attacking, Success when complete
    /// - Damage is dealt through hitbox during animation window
    /// </summary>
    [TaskCategory("Resonance/Enemy/Actions")]
    [TaskDescription("Executes a normal physical attack")]
    public class NormalAttackAction : EnemyTaskBase
    {
        private bool _sequenceFinished = false;
        private bool _attackLaunched = false;

        public override void OnStart()
        {
            base.OnStart();
            _attackLaunched = false;
            _sequenceFinished = false;
        }

        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            // ===== Phase 1: Launch Attack =====
            if (!_attackLaunched)
            {
                // 1. Subscribe to completion event
                Controller.OnAttackSequenceFinished += HandleSequenceFinished;

                // 2. Launch business logic (set cooldown)
                if (!Controller.LaunchNormalAttack())
                {
                    Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
                    return TaskStatus.Failure;
                }
                
                // 3. Set Animator parameters
                if (Animator != null && Animator.isActiveAndEnabled)
                {
                    // Set InAttackRange to allow Animator to enter AttackSM
                    Animator.SetBool("InAttackRange", true);
                    
                    // Trigger attack transition
                    Animator.SetTrigger("NormalAttackStart");
                }
                else
                {
                    Debug.LogWarning($"[BT Action] NormalAttackAction: Animator not available! Will rely on event callback.");
                }
                
                _attackLaunched = true;
                
                // 4. Stop movement during attack
                Movement?.Stop();
            }

            // ===== Phase 2: Wait for Animation Event =====
            if (_sequenceFinished)
            {
                return TaskStatus.Success;
            }

            // Continue waiting for animation to complete
            return TaskStatus.Running;
        }

        private void HandleSequenceFinished()
        {
            _sequenceFinished = true;
        }

        /// <summary>
        /// Cleanup when task ends
        /// CRITICAL: Reset InAttackRange to allow Animator to exit AttackSM
        /// </summary>
        public override void OnEnd()
        {
            // Reset Animator parameters - let Animator exit AttackSM
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("InAttackRange", false);
            }
            
            // Clean up event subscriptions
            if (Controller != null)
            {
                Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
            }
            
            _attackLaunched = false;
            _sequenceFinished = false;
        }
    }
}
