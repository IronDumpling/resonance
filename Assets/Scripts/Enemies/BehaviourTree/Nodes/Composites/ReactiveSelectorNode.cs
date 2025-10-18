using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Composites
{
    /// <summary>
    /// Reactive Selector node - re-evaluates all conditions every tick
    /// This is critical for responsive AI that needs to react to changing conditions
    /// 
    /// Difference from regular Selector:
    /// - Regular Selector: remembers which child is running, continues from there
    /// - Reactive Selector: always starts from first child, re-evaluates conditions
    /// 
    /// Use case: Enemy behavior that needs to switch immediately when player detected
    /// </summary>
    public class ReactiveSelectorNode : CompositeNode
    {
        public override BTNodeStatus Execute()
        {
            if (children == null || children.Count == 0)
            {
                UnityEngine.Debug.LogWarning("ReactiveSelectorNode: No children to execute!");
                return BTNodeStatus.Failure;
            }

            UnityEngine.Debug.Log($"[BT] ReactiveSelectorNode: Evaluating {children.Count} children...");

            // CRITICAL: Always evaluate from the first child
            // This ensures we re-check all conditions every tick
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                string childName = child.GetType().Name;
                
                UnityEngine.Debug.Log($"[BT]   - Child[{i}] ({childName}): Executing...");
                BTNodeStatus status = child.Execute();
                UnityEngine.Debug.Log($"[BT]   - Child[{i}] ({childName}): Returned {status}");

                switch (status)
                {
                    case BTNodeStatus.Success:
                        // Child succeeded, selector succeeds
                        UnityEngine.Debug.Log($"[BT] ReactiveSelectorNode: Child[{i}] ({childName}) succeeded. Selector succeeds.");
                        return BTNodeStatus.Success;

                    case BTNodeStatus.Running:
                        // Child is running, selector is running
                        // IMPORTANT: We return here but will re-evaluate from start next tick
                        UnityEngine.Debug.Log($"[BT] ReactiveSelectorNode: Child[{i}] ({childName}) running. Selector running.");
                        return BTNodeStatus.Running;

                    case BTNodeStatus.Failure:
                        // Child failed, try next child
                        UnityEngine.Debug.Log($"[BT]   - Child[{i}] ({childName}): Failed, trying next...");
                        continue;
                }
            }

            // All children failed, selector fails
            UnityEngine.Debug.Log($"[BT] ReactiveSelectorNode: All {children.Count} children failed. Selector fails.");
            return BTNodeStatus.Failure;
        }

        /// <summary>
        /// Reset is called when behavior completes or is interrupted
        /// For reactive selector, we don't need to maintain state between frames
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            // Reactive selector always starts fresh, so no additional reset needed
        }
    }
}

