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
            // Launch attack on first execution
            if (!_attackLaunched)
            {
                // Subscribe to events
                Controller.OnAttackSequenceFinished += HandleSequenceFinished;

                // Launch the attack
                if (Controller.LaunchAttack())
                {
                    _attackLaunched = true;
                }
                else
                {
                    return BTNodeStatus.Failure;
                }
            }

            // Wait for sequence to finish
            if (_sequenceFinished)
            {
                return BTNodeStatus.Success;
            }

            return BTNodeStatus.Running;
        }

        private void HandleSequenceFinished()
        {
            _sequenceFinished = true;
        }

        /// <summary>
        /// Reset attack state for next execution
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            // Clean up event subscriptions
            Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
            _attackLaunched = false;
            _sequenceFinished = false;
        }
    }
}
