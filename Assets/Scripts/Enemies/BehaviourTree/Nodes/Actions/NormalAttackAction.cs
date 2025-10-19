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
                Debug.Log("[BT Action] NormalAttackAction: Launching attack...");
                
                // 1. Subscribe to completion event
                Controller.OnAttackSequenceFinished += HandleSequenceFinished;

                // 2. Launch business logic (set cooldown)
                if (!Controller.LaunchNormalAttack())
                {
                    Debug.LogWarning("[BT Action] NormalAttackAction: LaunchNormalAttack failed! Returning Failure.");
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
                    
                    Debug.Log($"[BT Action] NormalAttackAction: Animation triggered. InAttackRange=true, NormalAttackStart=triggered");
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
                Debug.Log("[BT Action] NormalAttackAction: Sequence finished! Returning Success.");
                return BTNodeStatus.Success;
            }

            // Continue waiting for animation to complete
            return BTNodeStatus.Running;
        }

        private void HandleSequenceFinished()
        {
            Debug.Log("[BT Action] NormalAttackAction: OnAttackSequenceFinished event received!");
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
                Debug.Log("[BT Action] NormalAttackAction: Reset - InAttackRange=false");
            }
            
            // Clean up event subscriptions
            Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
            
            _attackLaunched = false;
            _sequenceFinished = false;
        }
    }
}
