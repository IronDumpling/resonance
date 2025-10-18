using UnityEngine;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Chase action node - moves towards the player at chase move speed
    /// Simplified to follow BT design principles:
    /// - No internal condition checking (handled by ConditionNode)
    /// - Focuses only on executing the chase behavior
    /// </summary>
    public class ChaseAction : ActionNode
    {
        private float _updateTimer = 0f;

        public override BTNodeStatus Execute()
        {
            // Update target position periodically
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= Controller.TargetUpdateInterval)
            {
                _updateTimer = 0f;
            }

            // Get current target position
            Vector3 targetPosition = Controller.HasPlayerTarget 
                ? Controller.PlayerTarget.position 
                : Controller.LastKnownPlayerPosition;

            // Move towards target
            Movement.SetTarget(targetPosition);

            // Continue chasing (conditions are checked externally)
            return BTNodeStatus.Running;
        }

        /// <summary>
        /// Reset chase state for next execution
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Movement?.Stop();
            _updateTimer = 0f;
        }
    }
}
