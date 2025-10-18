using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy is alive
    /// </summary>
    public class IsAliveCondition : ConditionNode
    {
        public override BTNodeStatus Execute()
        {
            return Controller.IsAlive ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }
}
