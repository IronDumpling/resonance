using System.Collections.Generic;

namespace Resonance.Enemies.BehaviourTree.Base
{
    public abstract class CompositeNode : BTNode
    {
        protected List<BTNode> children = new List<BTNode>();

        public void AddChild(BTNode child)
        {
            child.SetBlackboard(blackboard);
            children.Add(child);
        }
    }
}
