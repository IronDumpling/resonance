using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Resonance.Enemies.Data;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy is in unbalanced state (balance ≤ 0)
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyConditionalBase for component access
    /// - Returns Success if balance is depleted, Failure otherwise
    /// - When true, enemy should enter unbalanced state and become vulnerable to wave execution
    /// - Has high priority in behavior tree (but lower than core death)
    /// 
    /// This condition should ONLY check IsUnbalanced
    /// The Sequence node will keep UnbalancedAction running once started.
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Checks if enemy's balance has been depleted (unbalanced condition)")]
    public class UnbalancedCondition : EnemyConditionalBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            // Check if balance is depleted
            bool isUnbalanced = Controller.IsUnbalanced;
            return isUnbalanced ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}

