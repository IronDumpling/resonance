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
            // Launch wave attack on first execution
            if (!_attackLaunched)
            {
                // Subscribe to events
                Controller.OnAttackSequenceFinished += HandleSequenceFinished;

                // Launch the wave attack
                if (Controller.LaunchWaveAttack())
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
        /// Reset wave attack state for next execution
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
