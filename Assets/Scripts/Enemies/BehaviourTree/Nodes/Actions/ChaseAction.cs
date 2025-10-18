using UnityEngine;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Chase action node - moves towards the player at chase move speed
    /// Based on Legacy EnemyChaseAction
    /// </summary>
    public class ChaseAction : ActionNode
    {
        private Vector3 _targetPosition;
        private float _updateTimer = 0f;
        private bool _isActive = false;

        public override BTNodeStatus Execute()
        {
            // Check if can start chase
            if (!_isActive)
            {
                if (!CanStartChase())
                {
                    return BTNodeStatus.Failure;
                }
                StartChase();
            }

            // Update target position periodically
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= Controller.TargetUpdateInterval)
            {
                _updateTimer = 0f;
                UpdateTargetPosition();
            }

            // Move towards target
            MoveTowardsTarget();

            // Check finish conditions
            return CheckFinishConditions();
        }

        private bool CanStartChase()
        {
            return Controller.IsAlive &&
                   Controller.HasPlayerTarget &&
                   Controller.IsPlayerInDetectionRange() &&
                   !Controller.IsPlayerInAttackRange() &&
                   Controller.Stats.GetModifiedChaseMoveSpeed() > 0f;
        }

        private void StartChase()
        {
            _isActive = true;
            _updateTimer = 0f;

            // Set initial target to player's current position
            if (Controller.HasPlayerTarget)
            {
                _targetPosition = Controller.PlayerTarget.position;
            }
            else
            {
                _targetPosition = Controller.LastKnownPlayerPosition;
            }

            Debug.Log($"ChaseAction: Started chasing towards {_targetPosition}");
        }

        private void UpdateTargetPosition()
        {
            if (Controller.HasPlayerTarget)
            {
                _targetPosition = Controller.PlayerTarget.position;
            }
            else
            {
                _targetPosition = Controller.LastKnownPlayerPosition;
            }
        }

        private void MoveTowardsTarget()
        {
            Movement.SetTarget(_targetPosition);
        }

        private BTNodeStatus CheckFinishConditions()
        {
            // Finish if player enters attack range
            if (Controller.IsPlayerInAttackRange())
            {
                Debug.Log("ChaseAction: Player in attack range, finishing chase");
                FinishChase();
                return BTNodeStatus.Success;
            }

            // Finish if player is no longer detected
            if (!Controller.HasPlayerTarget || !Controller.IsPlayerInDetectionRange())
            {
                Debug.Log("ChaseAction: Player lost, finishing chase");
                FinishChase();
                return BTNodeStatus.Success;
            }

            // Finish if enemy can no longer move
            if (Controller.Stats.GetModifiedChaseMoveSpeed() <= 0f)
            {
                Debug.Log("ChaseAction: Cannot move, finishing chase");
                FinishChase();
                return BTNodeStatus.Failure;
            }

            return BTNodeStatus.Running;
        }

        private void FinishChase()
        {
            Movement?.Stop();
            _isActive = false;
        }
    }
}
