using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Composites
{
    /// <summary>
    /// Sequence node - executes children in order until one fails
    /// Returns Success if all children succeed
    /// Returns Failure if any child fails
    /// Returns Running if a child is running
    /// </summary>
    public class SequenceNode : CompositeNode
    {
        private int _currentChildIndex = 0;

        public override BTNodeStatus Execute()
        {
            if (children == null || children.Count == 0)
            {
                return BTNodeStatus.Success;
            }

            // Execute children from current index onwards
            while (_currentChildIndex < children.Count)
            {
                BTNodeStatus status = children[_currentChildIndex].Execute();

                switch (status)
                {
                    case BTNodeStatus.Failure:
                        // Child failed, sequence fails
                        _currentChildIndex = 0; // Reset for next execution
                        return BTNodeStatus.Failure;

                    case BTNodeStatus.Running:
                        // Child is running, sequence is running
                        return BTNodeStatus.Running;

                    case BTNodeStatus.Success:
                        // Child succeeded, move to next child
                        _currentChildIndex++;
                        break;
                }
            }

            // All children succeeded, sequence succeeds
            _currentChildIndex = 0; // Reset for next execution
            return BTNodeStatus.Success;
        }
    }
}
