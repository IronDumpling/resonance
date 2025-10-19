using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy's physical health is dead (≤ 0)
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyTaskBase for component access
    /// - Returns Success if physical health is depleted, Failure otherwise
    /// - When true, enemy should revive if core is still alive
    /// - Has high priority in behavior tree (but lower than core death)
    /// </summary>
    [TaskCategory("Resonance/Enemy/Conditions")]
    [TaskDescription("Checks if enemy's physical health has been depleted (revival condition)")]
    public class PhysicalDeathCondition : EnemyTaskBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            // Check if physical health is depleted
            bool isDead = Controller.IsPhysicallyDead;
            return isDead ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}

