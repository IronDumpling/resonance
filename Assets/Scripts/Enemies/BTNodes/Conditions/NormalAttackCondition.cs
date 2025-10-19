using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy can perform normal attack
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyConditionalBase for component access
    /// - Returns Success if can attack, Failure otherwise
    /// - Checks cooldown and basic attack requirements
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Checks if enemy can perform a normal attack (cooldown ready)")]
    public class NormalAttackCondition : EnemyConditionalBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            bool canAttack = Controller.CanNormalAttack;
            return canAttack ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}

