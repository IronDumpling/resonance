using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy can perform wave attack
    /// Wave attack requires:
    /// 1. Basic requirements (alive, has target, cooldown)
    /// 2. At least 1 energy slot available (checked in CanWaveAttack)
    /// </summary>
    public class WaveAttackCondition : ConditionNode
    {
        public override BTNodeStatus Execute()
        {
            bool canWave = Controller.CanWaveAttack;
            return canWave ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }
}

