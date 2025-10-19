using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy has NO player target
    /// Inverse of HasTargetCondition
    /// </summary>
    public class NoTargetCondition : ConditionNode
    {
        public override BTNodeStatus Execute()
        {
            bool noTarget = !Controller.HasPlayerTarget;
            var result = noTarget ? BTNodeStatus.Success : BTNodeStatus.Failure;
            return result;
        }
    }
}

