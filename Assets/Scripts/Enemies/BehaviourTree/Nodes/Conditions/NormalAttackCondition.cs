using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy can perform normal attack
    /// Checks cooldown and basic attack requirements
    /// </summary>
    public class NormalAttackCondition : ConditionNode
    {
        public override BTNodeStatus Execute()
        {
            bool canAttack = Controller.CanNormalAttack;
            return canAttack ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }
}

