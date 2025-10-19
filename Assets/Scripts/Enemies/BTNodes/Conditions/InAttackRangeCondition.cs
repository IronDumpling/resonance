using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if player is in attack range
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyTaskBase for component access
    /// - Returns Success if player is in attack range, Failure otherwise
    /// - Used to determine when to switch from chase to attack
    /// </summary>
    [TaskCategory("Resonance/Enemy/Conditions")]
    [TaskDescription("Checks if player is within attack range")]
    public class InAttackRangeCondition : EnemyTaskBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            bool inRange = Controller.IsPlayerInAttackRange();
            return inRange ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}
