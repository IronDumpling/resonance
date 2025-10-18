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
                UnityEngine.Debug.LogWarning("[BT] SequenceNode: No children!");
                return BTNodeStatus.Success;
            }

            UnityEngine.Debug.Log($"[BT] SequenceNode: Starting from child[{currentChild}] of {children.Count}");

            // Execute children from current index onwards
            while (currentChild < children.Count)
            {
                var child = children[currentChild];
                string childName = child.GetType().Name;
                
                UnityEngine.Debug.Log($"[BT]   Sequence - Child[{currentChild}] ({childName}): Executing...");
                BTNodeStatus status = child.Execute();
                UnityEngine.Debug.Log($"[BT]   Sequence - Child[{currentChild}] ({childName}): Returned {status}");

                switch (status)
                {
                    case BTNodeStatus.Failure:
                        // Child failed, sequence fails
                        UnityEngine.Debug.Log($"[BT] SequenceNode: Child[{currentChild}] ({childName}) failed. Sequence fails.");
                        Reset(); // Reset for next execution
                        return BTNodeStatus.Failure;

                    case BTNodeStatus.Running:
                        // Child is running, sequence is running
                        UnityEngine.Debug.Log($"[BT] SequenceNode: Child[{currentChild}] ({childName}) running. Sequence running.");
                        return BTNodeStatus.Running;

                    case BTNodeStatus.Success:
                        // Child succeeded, move to next child
                        UnityEngine.Debug.Log($"[BT]   Sequence - Child[{currentChild}] ({childName}) succeeded, moving to next...");
                        currentChild++;
                        break;
                }
            }

            // All children succeeded, sequence succeeds
            UnityEngine.Debug.Log($"[BT] SequenceNode: All {children.Count} children succeeded. Sequence succeeds.");
            Reset(); // Reset for next execution
            return BTNodeStatus.Success;
        }
    }
}
