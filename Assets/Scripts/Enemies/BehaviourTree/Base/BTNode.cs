using UnityEngine;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.BehaviourTree.Base
{
    public enum BTNodeStatus
    {
        Success,
        Failure,
        Running
    }

    public abstract class BTNode
    {
        protected EnemyBlackboard blackboard;

        public void SetBlackboard(EnemyBlackboard bb) => blackboard = bb;
        public abstract BTNodeStatus Execute();
    }
}
