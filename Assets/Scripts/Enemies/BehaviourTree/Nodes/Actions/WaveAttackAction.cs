using UnityEngine;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Wave attack action node - attacks player's core health
    /// Simplified to follow BT design principles:
    /// - No internal condition checking (handled by ConditionNode)
    /// - Focuses only on executing the wave attack behavior
    /// Deals CoreHealth damage to break player's crystal core
    /// </summary>
    public class WaveAttackAction : ActionNode
    {
        private bool _sequenceFinished = false;
        private bool _attackLaunched = false;

        public override BTNodeStatus Execute()
        {
            // ===== Phase 1: Launch Wave Attack =====
            if (!_attackLaunched)
            {
                Debug.Log("[BT Action] WaveAttackAction: Launching wave attack...");
                
                // 1. Subscribe to completion event
                Controller.OnAttackSequenceFinished += HandleSequenceFinished;

                // 2. Launch business logic (set cooldown)
                if (!Controller.LaunchWaveAttack())
                {
                    Debug.LogWarning("[BT Action] WaveAttackAction: LaunchWaveAttack failed! Returning Failure.");
                    Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
                    return BTNodeStatus.Failure;
                }
                
                // 3. Set Animator parameters
                var animator = GetAnimator();
                if (animator != null && animator.isActiveAndEnabled)
                {
                    // ★ KEY: Set InAttackRange to allow Animator to enter WaveAttackSM
                    animator.SetBool("InAttackRange", true);
                    
                    // ★ Trigger wave attack transition
                    animator.SetTrigger("WaveAttackStart");
                    
                    Debug.Log($"[BT Action] WaveAttackAction: Animation triggered. InAttackRange=true, WaveAttackStart=triggered");
                }
                else
                {
                    Debug.LogWarning($"[BT Action] WaveAttackAction: Animator not available! Will rely on event callback.");
                }
                
                _attackLaunched = true;
                
                // 4. Stop movement during attack
                Movement?.Stop();
            }

            // ===== Phase 2: Wait for Animation Event =====
            if (_sequenceFinished)
            {
                Debug.Log("[BT Action] WaveAttackAction: Sequence finished! Returning Success.");
                return BTNodeStatus.Success;
            }

            // Continue waiting for animation to complete
            return BTNodeStatus.Running;
        }

        private void HandleSequenceFinished()
        {
            Debug.Log("[BT Action] WaveAttackAction: OnAttackSequenceFinished event received!");
            _sequenceFinished = true;
        }

        /// <summary>
        /// Reset wave attack state for next execution
        /// ★ CRITICAL: Reset InAttackRange to allow Animator to exit WaveAttackSM
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            
            // ★ Reset Animator parameters - let Animator exit WaveAttackSM
            var animator = GetAnimator();
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetBool("InAttackRange", false);
                Debug.Log("[BT Action] WaveAttackAction: Reset - InAttackRange=false");
            }
            
            // Clean up event subscriptions
            Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
            
            _attackLaunched = false;
            _sequenceFinished = false;
        }
    }
}
