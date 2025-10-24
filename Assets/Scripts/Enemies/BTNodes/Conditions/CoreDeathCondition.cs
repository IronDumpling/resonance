using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy's core health is dead (≤ 0)
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyConditionalBase for component access
    /// - Returns Success if core is dead, Failure otherwise
    /// - When true, enemy should enter true death state
    /// - Has highest priority in behavior tree
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Checks if enemy's core health has been depleted (true death condition)")]
    public class CoreDeathCondition : EnemyConditionalBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            // Check if core health is depleted
            bool isCoreDead = Controller.IsCoreDead;
            return isCoreDead ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
