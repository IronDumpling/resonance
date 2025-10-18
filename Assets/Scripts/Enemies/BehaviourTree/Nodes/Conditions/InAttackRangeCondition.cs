using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if player is in attack range
    /// </summary>
    public class InAttackRangeCondition : ConditionNode
    {
        public override BTNodeStatus Execute()
        {
            bool inRange = Controller.IsPlayerInAttackRange();
            var result = inRange ? BTNodeStatus.Success : BTNodeStatus.Failure;
            UnityEngine.Debug.Log($"[BT Condition] InAttackRange: {inRange} → {result}");
            return result;
        }
    }
}
