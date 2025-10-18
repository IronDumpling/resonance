using UnityEngine;
using Resonance.Enemies.Core;
using Resonance.Enemies.Movement;

namespace Resonance.Enemies.BehaviourTree.Base
{
    public abstract class ActionNode : BTNode
    {
        protected EnemyController Controller => blackboard.GetSystem<EnemyController>();
        protected MovementSystem Movement => blackboard.GetSystem<MovementSystem>();
        protected EnemyAnimator AnimatorBridge => blackboard.GetSystem<EnemyAnimator>();
        
        /// <summary>
        /// Get Unity Animator component
        /// Actions can use this to directly control animations
        /// </summary>
        protected Animator GetAnimator()
        {
            return AnimatorBridge?.GetComponent<Animator>();
        }
    }
}
