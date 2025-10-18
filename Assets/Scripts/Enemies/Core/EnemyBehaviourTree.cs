using Resonance.Enemies.BehaviourTree.Base;
using Resonance.Enemies.BehaviourTree.Nodes.Composites;
using Resonance.Enemies.BehaviourTree.Nodes.Actions;
using Resonance.Enemies.BehaviourTree.Nodes.Conditions;
using UnityEngine;

namespace Resonance.Enemies.Core
{
    public class EnemyBehaviourTree
    {
        private BTNode _rootNode;
        private EnemyBlackboard _blackboard;

        public EnemyBehaviourTree(EnemyBlackboard blackboard)
        {
            _blackboard = blackboard;
            _rootNode = BuildTree();
        }

        public void Tick()
        {
            if (_rootNode != null)
            {
                _rootNode.Execute();
            }
        }

        /// <summary>
        /// Reset the entire behavior tree state
        /// Call this when you need to restart the behavior tree from scratch
        /// </summary>
        public void Reset()
        {
            _rootNode?.Reset();
        }

        private BTNode BuildTree()
        {
            // Build enemy behavior tree following the design draft
            // Structure: Selector (try behaviors in priority order)
            //   1. If coreHealth ≤ 0 -> Enemy truly dead (handled by MonoBehaviour)
            //   2. If health ≤ 0 -> Revive Action
            //   3. If no target (detection range) -> Patrol Action
            //   4. If has target but not in attack range -> Chase Action
            //   5. If has target and in attack range -> Attack (Wave or Normal)

            var root = new SelectorNode();
            root.SetBlackboard(_blackboard);

            // ===== 1. Core Death Check (true death) =====
            // Note: This is primarily handled by EnemyMonoBehaviour checking IsTrulyDead
            // and destroying the GameObject. We check it here to stop all behaviors immediately.
            var coreDeathSequence = new SequenceNode();
            coreDeathSequence.SetBlackboard(_blackboard);
            coreDeathSequence.AddChild(new CoreDeathCondition());
            // When core is dead, just return Success to stop the tree
            // The MonoBehaviour will handle actual destruction
            root.AddChild(coreDeathSequence);

            // ===== 2. Physical Death -> Revive =====
            var reviveSequence = new SequenceNode();
            reviveSequence.SetBlackboard(_blackboard);
            reviveSequence.AddChild(new PhysicalDeathCondition());
            reviveSequence.AddChild(new ReviveAction());
            root.AddChild(reviveSequence);

            // ===== 3. No Target -> Patrol =====
            var patrolSequence = new SequenceNode();
            patrolSequence.SetBlackboard(_blackboard);
            patrolSequence.AddChild(new NoTargetCondition());
            patrolSequence.AddChild(new PatrolAction());
            root.AddChild(patrolSequence);

            // ===== 4. Has Target but Not in Attack Range -> Chase =====
            var chaseSequence = new SequenceNode();
            chaseSequence.SetBlackboard(_blackboard);
            chaseSequence.AddChild(new HasTargetCondition());
            chaseSequence.AddChild(new NotInAttackRangeCondition());
            chaseSequence.AddChild(new ChaseAction());
            root.AddChild(chaseSequence);

            // ===== 5. Has Target and In Attack Range -> Attack =====
            var attackSequence = new SequenceNode();
            attackSequence.SetBlackboard(_blackboard);
            attackSequence.AddChild(new HasTargetCondition());
            attackSequence.AddChild(new InAttackRangeCondition());
            
            // Attack type selection: Wave Attack or Normal Attack
            var attackSelector = new SelectorNode();
            attackSelector.SetBlackboard(_blackboard);
            
            // Try wave attack first (if conditions met)
            var waveAttackSequence = new SequenceNode();
            waveAttackSequence.SetBlackboard(_blackboard);
            waveAttackSequence.AddChild(new WaveAttackCondition());
            waveAttackSequence.AddChild(new WaveAttackAction());
            attackSelector.AddChild(waveAttackSequence);
            
            // Fall back to normal attack
            attackSelector.AddChild(new NormalAttackAction());
            
            attackSequence.AddChild(attackSelector);
            root.AddChild(attackSequence);

            return root;
        }
    }
}
