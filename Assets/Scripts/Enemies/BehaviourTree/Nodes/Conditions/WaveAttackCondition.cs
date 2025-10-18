using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy can perform wave attack
    /// Wave attack conditions:
    /// 1. Attack count is multiple of 3 OR
    /// 2. Player's CrystalCore is in Chaos state
    /// </summary>
    public class WaveAttackCondition : ConditionNode
    {
        public override BTNodeStatus Execute()
        {
            bool canWave = Controller.CanWaveAttack;
            var result = canWave ? BTNodeStatus.Success : BTNodeStatus.Failure;
            UnityEngine.Debug.Log($"[BT Condition] CanWaveAttack: {canWave} → {result}");
            return result;
        }
    }
}

