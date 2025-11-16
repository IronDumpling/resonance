using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Resonance.Enemies.Data;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy is in core exposed state
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyConditionalBase for component access
    /// - Returns Success if in core exposed state, Failure otherwise
    /// - When true, enemy should execute core exposed action (balance recovery)
    /// - Has high priority in behavior tree (but lower than core death and unbalanced)
    /// 
    /// This condition should ONLY check if state is CoreExposed
    /// The Sequence node will keep CoreExposedAction running once started.
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Checks if enemy is in core exposed state (after wave execution)")]
    public class CoreExposedCondition : EnemyConditionalBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            // Check if in core exposed state
            bool isCoreExposed = Controller.CurrentState == EnemyState.CoreExposed;
            return isCoreExposed ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}

