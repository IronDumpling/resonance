using Resonance.Enemies.Core;

namespace Resonance.Enemies.BehaviourTree.Base
{
    public abstract class ConditionNode : BTNode
    {
        protected EnemyController Controller => blackboard.GetSystem<EnemyController>();
    }
}
