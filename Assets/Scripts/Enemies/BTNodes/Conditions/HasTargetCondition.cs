using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy has a player target
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyConditionalBase for component access
    /// - Returns Success if has target, Failure otherwise
    /// - Can be used in Selector/Sequence nodes to control behavior flow
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Checks if enemy has detected a player target")]
    public class HasTargetCondition : EnemyConditionalBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                Debug.LogWarning($"[HasTargetCondition] ValidateComponents failed on {gameObject.name}");
                return TaskStatus.Failure;
            }

            bool hasTarget = Controller.HasPlayerTarget;
            
            // Debug log to track detection
            if (hasTarget)
            {
                Debug.Log($"[HasTargetCondition] {gameObject.name} HAS target: {Controller.PlayerTarget?.name}");
            }
            
            return hasTarget ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
