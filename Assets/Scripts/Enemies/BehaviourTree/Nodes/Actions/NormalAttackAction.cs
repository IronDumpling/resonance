using UnityEngine;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Normal attack action node - triggers attack animation and manages attack flow
    /// Based on Legacy EnemyNormalAttackAction
    /// Damage is dealt through hitbox during animation window
    /// </summary>
    public class NormalAttackAction : ActionNode
    {
        private bool _windowOpened = false;
        private bool _isActive = false;
        private bool _sequenceFinished = false;

        public override BTNodeStatus Execute()
        {
            // Check if can start attack
            if (!_isActive)
            {
                if (!CanStartAttack())
                {
                    return BTNodeStatus.Failure;
                }
                StartAttack();
            }

            // If sequence finished, complete the action
            if (_sequenceFinished)
            {
                FinishAttack();
                return BTNodeStatus.Success;
            }

            // Cancel if player left range before window opened
            if (!_windowOpened && (!Controller.HasPlayerTarget || !Controller.IsPlayerInAttackRange()))
            {
                FinishAttack();
                return BTNodeStatus.Failure;
            }

            return BTNodeStatus.Running;
        }

        private bool CanStartAttack()
        {
            return Controller.CanNormalAttack && Controller.IsPlayerInAttackRange();
        }

        private void StartAttack()
        {
            _isActive = true;
            _windowOpened = false;
            _sequenceFinished = false;

            // Subscribe to events
            Controller.OnAttackSequenceFinished += HandleSequenceFinished;
            Controller.OnAttackWindowOpened += HandleWindowOpened;
            Controller.OnAttackWindowClosed += HandleWindowClosed;

            // Launch the attack
            if (Controller.LaunchAttack())
            {
                Debug.Log("NormalAttackAction: Started attack action");
            }
            else
            {
                FinishAttack();
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

        private void FinishAttack()
        {
            // Clean up event subscriptions
            Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
            Controller.OnAttackWindowOpened -= HandleWindowOpened;
            Controller.OnAttackWindowClosed -= HandleWindowClosed;

            _isActive = false;
        }
    }
}
