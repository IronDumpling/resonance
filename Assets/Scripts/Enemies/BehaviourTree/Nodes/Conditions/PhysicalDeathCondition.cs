using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy's physical health is dead (≤ 0)
    /// When true, enemy should revive if core is still alive
    /// </summary>
    public class PhysicalDeathCondition : ConditionNode
    {
        public override BTNodeStatus Execute()
        {
            // Check if physical health is depleted
            bool isDead = !Controller.IsAlive;
            var result = isDead ? BTNodeStatus.Success : BTNodeStatus.Failure;
            UnityEngine.Debug.Log($"[BT Condition] PhysicalDeath: isDead={isDead} (IsAlive={Controller.IsAlive}) → {result}");
            return result;
        }
    }
}

