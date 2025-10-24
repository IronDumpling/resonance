using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Resonance.Enemies.Data;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy's physical health is dead (≤ 0)
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyConditionalBase for component access
    /// - Returns Success if physical health is depleted, Failure otherwise
    /// - When true, enemy should revive if core is still alive
    /// - Has high priority in behavior tree (but lower than core death)
    /// 
    /// This condition should ONLY check IsPhysicallyDead
    /// The Sequence node will keep ReviveAction running once started.
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Checks if enemy's physical health has been depleted (revival condition)")]
    public class PhysicalDeathCondition : EnemyConditionalBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            // Check if physical health is depleted
            bool isPhysicallyDead = Controller.IsPhysicallyDead;
            return isPhysicallyDead ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}

