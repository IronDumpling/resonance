using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy can perform wave attack
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyTaskBase for component access
    /// - Returns Success if can perform wave attack, Failure otherwise
    /// - Wave attack requires: alive, has target, cooldown ready, and energy available
    /// </summary>
    [TaskCategory("Resonance/Enemy/Conditions")]
    [TaskDescription("Checks if enemy can perform a wave attack (cooldown ready and energy available)")]
    public class WaveAttackCondition : EnemyTaskBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            bool canWave = Controller.CanWaveAttack;
            return canWave ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}

