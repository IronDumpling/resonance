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
            bool hasTarget = Controller.HasPlayerTarget;
            var result = hasTarget ? BTNodeStatus.Success : BTNodeStatus.Failure;
            return result;
        }
    }
}
