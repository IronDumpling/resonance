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
                // 1. Subscribe to completion event
                Controller.OnAttackSequenceFinished += HandleSequenceFinished;

                // 2. Consume energy for wave attack
                if (!Controller.Stats.crystalCore.ConsumeEnergySlot())
                {
                    Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
                    return BTNodeStatus.Failure;
                }

                // 3. Launch business logic (set cooldown)
                if (!Controller.LaunchWaveAttack())
                {
                    Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
                    return BTNodeStatus.Failure;
                }
                
                // 4. Set Animator parameters
                var animator = GetAnimator();
                if (animator != null && animator.isActiveAndEnabled)
                {
                    // ★ KEY: Set InAttackRange to allow Animator to enter WaveAttackSM
                    animator.SetBool("InAttackRange", true);
                    
                    // ★ Trigger wave attack transition
                    animator.SetTrigger("WaveAttackStart");
                }
                else
                {
                    Debug.LogWarning($"[BT Action] WaveAttackAction: Animator not available! Will rely on event callback.");
                }
                
                _attackLaunched = true;
                
                // 5. Stop movement during attack
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
            }
            
            // Clean up event subscriptions
            Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
            
            _attackLaunched = false;
            _sequenceFinished = false;
        }
    }
}
