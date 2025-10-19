using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Wave attack action node - attacks player's core health
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyTaskBase for component access
    /// - No internal condition checking (handled by Conditional nodes)
    /// - Focuses only on executing the wave attack behavior
    /// - Returns Running while attacking, Success when complete
    /// - Deals CoreHealth damage to break player's crystal core
    /// </summary>
    [TaskCategory("Resonance/Enemy/Actions")]
    [TaskDescription("Executes a wave attack that damages player's core health")]
    public class WaveAttackAction : EnemyTaskBase
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

            // ===== Phase 1: Launch Wave Attack =====
            if (!_attackLaunched)
            {
                // 1. Subscribe to completion event
                Controller.OnAttackSequenceFinished += HandleSequenceFinished;

                // 2. Consume energy for wave attack
                if (!Controller.Stats.crystalCore.ConsumeEnergySlot())
                {
                    Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
                    return TaskStatus.Failure;
                }

                // 3. Launch business logic (set cooldown)
                if (!Controller.LaunchWaveAttack())
                {
                    Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
                    return TaskStatus.Failure;
                }
                
                // 4. Set Animator parameters
                if (Animator != null && Animator.isActiveAndEnabled)
                {
                    // Set InAttackRange to allow Animator to enter WaveAttackSM
                    Animator.SetBool("InAttackRange", true);
                    
                    // Trigger wave attack transition
                    Animator.SetTrigger("WaveAttackStart");
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
                return TaskStatus.Success;
            }

            // Continue waiting for animation to complete
            return TaskStatus.Running;
        }

        private void HandleSequenceFinished()
        {
            _sequenceFinished = true;
        }

        public override void OnEnd()
        {
            // Reset Animator parameters - let Animator exit WaveAttackSM
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
