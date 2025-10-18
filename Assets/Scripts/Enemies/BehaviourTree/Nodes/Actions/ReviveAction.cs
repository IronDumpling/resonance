using UnityEngine;
using Resonance.Enemies.Core;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Revive action node - handles the revival process when physical health reaches 0
    /// Simplified to follow BT design principles:
    /// - No internal condition checking (handled by ConditionNode)
    /// - Focuses only on executing the revival behavior
    /// Restores physical health over time
    /// </summary>
    public class ReviveAction : ActionNode
    {
        private float _reviveTimer = 0f;
        private float _maxReviveTime;
        private bool _initialized = false;

        public override BTNodeStatus Execute()
        {
            // Initialize on first execution
            if (!_initialized)
            {
                _initialized = true;
                _reviveTimer = 0f;
                _maxReviveTime = 3f * Controller.Stats.maxHealth / Controller.Stats.revivalRate;

                // Stop all movement and behaviors
                Controller.StopPatrol();
                Controller.LosePlayer();
            }

            _reviveTimer += Time.deltaTime;

            // Check if revival is complete (physical health restored)
            if (Controller.IsAlive)
            {
                return BTNodeStatus.Success;
            }

            // Safety timeout
            if (_reviveTimer > _maxReviveTime)
            {
                Controller.Stats.FullRestore();
                return BTNodeStatus.Success;
            }

            return BTNodeStatus.Running;
        }

        /// <summary>
        /// Reset revival state for next execution
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _initialized = false;
            _reviveTimer = 0f;
        }
    }
}
