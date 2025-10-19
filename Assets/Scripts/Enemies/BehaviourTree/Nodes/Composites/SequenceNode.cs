using UnityEngine;
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
                Debug.LogWarning("[BT] SequenceNode: No children!");
                return BTNodeStatus.Success;
            }

            // Execute children from current index onwards
            while (currentChild < children.Count)
            {
                var child = children[currentChild];
                string childName = child.GetType().Name;
                
                BTNodeStatus status = child.Execute();
                Debug.Log($"[BT] SequenceNode - Child[{currentChild}] ({childName}): Executed with status {status}");

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

            Reset(); // Reset for next execution
            return BTNodeStatus.Success;
        }
    }
}
