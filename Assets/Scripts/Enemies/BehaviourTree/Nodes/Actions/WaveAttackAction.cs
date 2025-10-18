using UnityEngine;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Wave attack action node - attacks player's core health
    /// Based on Legacy EnemyWaveAttackAction
    /// Deals CoreHealth damage to break player's crystal core
    /// </summary>
    public class WaveAttackAction : ActionNode
    {
        private bool _windowOpened = false;
        private bool _isActive = false;
        private bool _sequenceFinished = false;

        public override BTNodeStatus Execute()
        {
            // Check if can start wave attack
            if (!_isActive)
            {
                if (!CanStartWaveAttack())
                {
                    return BTNodeStatus.Failure;
                }
                StartWaveAttack();
            }

            // If sequence finished, complete the action
            if (_sequenceFinished)
            {
                FinishWaveAttack();
                return BTNodeStatus.Success;
            }

            // Cancel if player left range before window opened
            if (!_windowOpened && (!Controller.HasPlayerTarget || !Controller.IsPlayerInAttackRange()))
            {
                FinishWaveAttack();
                return BTNodeStatus.Failure;
            }

            return BTNodeStatus.Running;
        }

        private bool CanStartWaveAttack()
        {
            return Controller.CanWaveAttack && Controller.IsPlayerInAttackRange();
        }

        private void StartWaveAttack()
        {
            _isActive = true;
            _windowOpened = false;
            _sequenceFinished = false;

            // Subscribe to events
            Controller.OnAttackSequenceFinished += HandleSequenceFinished;
            Controller.OnAttackWindowOpened += HandleWindowOpened;
            Controller.OnAttackWindowClosed += HandleWindowClosed;

            // Launch the wave attack
            if (Controller.LaunchWaveAttack())
            {
                Debug.Log("WaveAttackAction: Started wave attack action");
            }
            else
            {
                FinishWaveAttack();
            }
        }

        private void HandleWindowOpened()
        {
            _windowOpened = true;
        }

        private void HandleWindowClosed()
        {
            _windowOpened = false;
        }

        private void HandleSequenceFinished()
        {
            _sequenceFinished = true;
        }

        private void FinishWaveAttack()
        {
            // Clean up event subscriptions
            Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
            Controller.OnAttackWindowOpened -= HandleWindowOpened;
            Controller.OnAttackWindowClosed -= HandleWindowClosed;

            _isActive = false;
        }
    }
}
