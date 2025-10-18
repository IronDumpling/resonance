using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if player is NOT in attack range
    /// Inverse of InAttackRangeCondition, used for Chase behavior
    /// </summary>
    public class NotInAttackRangeCondition : ConditionNode
    {
        public override BTNodeStatus Execute()
        {
            bool notInRange = !Controller.IsPlayerInAttackRange();
            var result = notInRange ? BTNodeStatus.Success : BTNodeStatus.Failure;
            UnityEngine.Debug.Log($"[BT Condition] NotInAttackRange: {notInRange} (InRange={Controller.IsPlayerInAttackRange()}) → {result}");
            return result;
        }
    }
}

