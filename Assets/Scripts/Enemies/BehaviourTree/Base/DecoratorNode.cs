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

        /// <summary>
        /// Reset this node and its child
        /// </summary>
        public override void Reset()
        {
            child?.Reset();
        }
    }
}
