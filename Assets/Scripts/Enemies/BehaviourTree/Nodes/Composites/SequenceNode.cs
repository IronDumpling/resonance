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
        public override BTNodeStatus Execute()
        {
            if (children == null || children.Count == 0)
            {
                return BTNodeStatus.Success;
            }

            // Execute children from current index onwards
            while (currentChild < children.Count)
            {
                BTNodeStatus status = children[currentChild].Execute();

                switch (status)
                {
                    case BTNodeStatus.Failure:
                        // Child failed, sequence fails
                        Reset(); // Reset for next execution
                        return BTNodeStatus.Failure;

                    case BTNodeStatus.Running:
                        // Child is running, sequence is running
                        return BTNodeStatus.Running;

                    case BTNodeStatus.Success:
                        // Child succeeded, move to next child
                        currentChild++;
                        break;
                }
            }

            // All children succeeded, sequence succeeds
            Reset(); // Reset for next execution
            return BTNodeStatus.Success;
        }
    }
}
