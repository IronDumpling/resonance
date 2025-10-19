using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy has a player target
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyTaskBase for component access
    /// - Returns Success if has target, Failure otherwise
    /// - Can be used in Selector/Sequence nodes to control behavior flow
    /// </summary>
    [TaskCategory("Resonance/Enemy/Conditions")]
    [TaskDescription("Checks if enemy has detected a player target")]
    public class HasTargetCondition : EnemyTaskBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            bool hasTarget = Controller.HasPlayerTarget;
            return hasTarget ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
