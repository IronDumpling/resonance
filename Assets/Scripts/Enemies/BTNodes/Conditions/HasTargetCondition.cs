using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy has a player target
    /// Uses vision system to detect player in real-time
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyConditionalBase for component access
    /// - Returns Success if has target, Failure otherwise
    /// - Can be used in Selector/Sequence nodes to control behavior flow
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Checks if enemy has detected a player target using vision system")]
    public class HasTargetCondition : EnemyConditionalBase
    {
        private bool _previousHasTarget = false;
        
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                Debug.LogWarning($"[HasTargetCondition] ValidateComponents failed on {gameObject.name}");
                return TaskStatus.Failure;
            }

            // Check if enemy has player target
            bool hasTarget = Controller.HasPlayerTarget;
            
            // Only log when target state changes
            if (hasTarget != _previousHasTarget)
            {
                if (hasTarget)
                {
                    Debug.Log($"[HasTargetCondition] ✓ TARGET ACQUIRED: {Controller.PlayerTarget?.name} at {Controller.LastKnownPlayerPosition}");
                }
                else
                {
                    Debug.Log($"[HasTargetCondition] ✗ TARGET LOST");
                }
                _previousHasTarget = hasTarget;
            }
            
            return hasTarget ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
