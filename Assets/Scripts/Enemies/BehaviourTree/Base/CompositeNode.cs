using System.Collections.Generic;

namespace Resonance.Enemies.BehaviourTree.Base
{
    public abstract class CompositeNode : BTNode
    {
        protected List<BTNode> children = new List<BTNode>();
        
        /// <summary>
        /// Current child index being executed (managed by base class)
        /// </summary>
        protected int currentChild = 0;

        public void AddChild(BTNode child)
        {
            child.SetBlackboard(blackboard);
            children.Add(child);
        }

        /// <summary>
        /// Reset this node and all children recursively
        /// </summary>
        public override void Reset()
        {
            currentChild = 0;
            foreach (var child in children)
            {
                child.Reset();
            }
        }
    }
}
