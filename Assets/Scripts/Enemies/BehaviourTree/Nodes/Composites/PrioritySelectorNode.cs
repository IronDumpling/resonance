using System.Collections.Generic;
using System.Linq;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Composites
{
    /// <summary>
    /// Priority Selector node - executes children sorted by priority (highest first)
    /// Inspired by network resource design
    /// Returns Success if any child succeeds
    /// Returns Failure if all children fail
    /// Returns Running if a child is running
    /// </summary>
    public class PrioritySelectorNode : CompositeNode
    {
        private List<BTNode> _sortedChildren;

        protected virtual List<BTNode> SortChildren()
        {
            return children.OrderByDescending(child => child.Priority).ToList();
        }

        public override BTNodeStatus Execute()
        {
            // Lazy initialization of sorted children
            if (_sortedChildren == null)
            {
                _sortedChildren = SortChildren();
            }

            if (_sortedChildren == null || _sortedChildren.Count == 0)
            {
                return BTNodeStatus.Failure;
            }

            // Execute children from current index onwards
            while (currentChild < _sortedChildren.Count)
            {
                BTNodeStatus status = _sortedChildren[currentChild].Execute();

                switch (status)
                {
                    case BTNodeStatus.Success:
                        // Child succeeded, selector succeeds
                        Reset();
                        return BTNodeStatus.Success;

                    case BTNodeStatus.Running:
                        // Child is running, selector is running
                        return BTNodeStatus.Running;

                    case BTNodeStatus.Failure:
                        // Child failed, try next child
                        currentChild++;
                        break;
                }
            }

            // All children failed, selector fails
            Reset();
            return BTNodeStatus.Failure;
        }

        public override void Reset()
        {
            base.Reset();
            _sortedChildren = null; // Re-sort on next execution
        }
    }
}

