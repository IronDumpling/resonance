using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy has a player target
    /// </summary>
    public class HasTargetCondition : ConditionNode
    {
        public override BTNodeStatus Execute()
        {
            return Controller.HasPlayerTarget ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }
}
