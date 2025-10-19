using UnityEngine;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Normal attack action node - triggers attack animation and manages attack flow
    /// Simplified to follow BT design principles:
    /// - No internal condition checking (handled by ConditionNode)
    /// - Focuses only on executing the attack behavior
    /// Damage is dealt through hitbox during animation window
    /// </summary>
    public class NormalAttackAction : ActionNode
    {
        private bool _sequenceFinished = false;
        private bool _attackLaunched = false;

        public override BTNodeStatus Execute()
        {
            // ===== Phase 1: Launch Attack =====
            if (!_attackLaunched)
            {
                // 1. Subscribe to completion event
                Controller.OnAttackSequenceFinished += HandleSequenceFinished;

                // 2. Launch business logic (set cooldown)
                if (!Controller.LaunchNormalAttack())
                {
                    Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
                    return BTNodeStatus.Failure;
                }
                
                // 3. Set Animator parameters
                var animator = GetAnimator();
                if (animator != null && animator.isActiveAndEnabled)
                {
                    // ★ KEY: Set InAttackRange to allow Animator to enter AttackSM
                    animator.SetBool("InAttackRange", true);
                    
                    // ★ Trigger attack transition
                    animator.SetTrigger("NormalAttackStart");
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
                return BTNodeStatus.Success;
            }

            // Continue waiting for animation to complete
            return BTNodeStatus.Running;
        }

        private void HandleSequenceFinished()
        {
            _sequenceFinished = true;
        }

        /// <summary>
        /// Reset attack state for next execution
        /// ★ CRITICAL: Reset InAttackRange to allow Animator to exit AttackSM
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            
            // ★ Reset Animator parameters - let Animator exit AttackSM
            var animator = GetAnimator();
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetBool("InAttackRange", false);
            }
            
            // Clean up event subscriptions
            Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
            
            _attackLaunched = false;
            _sequenceFinished = false;
        }
    }
}
