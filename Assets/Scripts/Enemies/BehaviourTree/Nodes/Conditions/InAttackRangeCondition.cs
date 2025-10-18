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
            return Controller.IsPlayerInAttackRange() ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }
}
