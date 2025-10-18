using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Composites
{
    /// <summary>
    /// Selector node - executes children in order until one succeeds
    /// Returns Success if any child succeeds
    /// Returns Failure if all children fail
    /// Returns Running if a child is running
    /// </summary>
    public class SelectorNode : CompositeNode
    {
        private int _currentChildIndex = 0;

        public override BTNodeStatus Execute()
        {
            if (children == null || children.Count == 0)
            {
                return BTNodeStatus.Failure;
            }

            // Execute children from current index onwards
            while (_currentChildIndex < children.Count)
            {
                BTNodeStatus status = children[_currentChildIndex].Execute();

                switch (status)
                {
                    case BTNodeStatus.Success:
                        // Child succeeded, selector succeeds
                        _currentChildIndex = 0; // Reset for next execution
                        return BTNodeStatus.Success;

                    case BTNodeStatus.Running:
                        // Child is running, selector is running
                        return BTNodeStatus.Running;

                    case BTNodeStatus.Failure:
                        // Child failed, try next child
                        _currentChildIndex++;
                        break;
                }
            }

            // All children failed, selector fails
            _currentChildIndex = 0; // Reset for next execution
            return BTNodeStatus.Failure;
        }
    }
}
