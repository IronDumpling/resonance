using UnityEngine;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.BehaviourTree.Base
{
    public enum BTNodeStatus
    {
        Success,
        Failure,
        Running
    }

    public abstract class BTNode
    {
        protected EnemyBlackboard blackboard;

        /// <summary>
        /// Optional priority for sorting nodes (higher = more important)
        /// Used by PrioritySelector if implemented
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Optional name for debugging
        /// </summary>
        public string Name { get; set; } = "Node";

        public void SetBlackboard(EnemyBlackboard bb) => blackboard = bb;
        
        public abstract BTNodeStatus Execute();

        /// <summary>
        /// Reset node state for next execution cycle
        /// Override this in derived classes to reset node-specific state
        /// </summary>
        public virtual void Reset()
        {
            // Base implementation - override in derived classes
        }
    }
}
