namespace Resonance.Enemies.BehaviourTree.Base
{
    public abstract class DecoratorNode : BTNode
    {
        protected BTNode child;

        public void SetChild(BTNode node)
        {
            child = node;
            child.SetBlackboard(blackboard);
        }
    }
}
