using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy's core health is dead (≤ 0)
    /// When true, enemy should be truly dead
    /// </summary>
    public class CoreDeathCondition : ConditionNode
    {
        public override BTNodeStatus Execute()
        {
            // Check if core health is depleted
            return !Controller.IsCoreAlive ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }
}

