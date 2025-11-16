using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Resonance.Enemies.Data;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy needs to patrol
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyConditionalBase for component access
    /// - Returns Success if enemy needs to patrol, Failure otherwise
    /// - Used to determine when to switch from chase to patrol
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Checks if enemy needs to patrol")]
    public class PatrolCondition : EnemyConditionalBase
    {
        public override TaskStatus OnUpdate()
        {
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            bool isBalanced = Controller.IsBalanced;
            bool isNormal = (Controller.CurrentState == EnemyState.Normal);

            return (isBalanced && isNormal) ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}