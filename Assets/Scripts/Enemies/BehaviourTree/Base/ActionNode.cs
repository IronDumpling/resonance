using Resonance.Enemies.Core;
using Resonance.Enemies.Movement;

namespace Resonance.Enemies.BehaviourTree.Base
{
    public abstract class ActionNode : BTNode
    {
        protected EnemyController Controller => blackboard.GetSystem<EnemyController>();
        protected MovementSystem Movement => blackboard.GetSystem<MovementSystem>();
        protected EnemyAnimator Animator => blackboard.GetSystem<EnemyAnimator>();
    }
}
