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

        private BTNode BuildTree()
        {
            // Build enemy behavior tree
            // Structure: Selector (try behaviors in priority order)
            //   1. If truly dead -> Stop (handled by MonoBehaviour destruction)
            //   2. If stunned -> Wait (do nothing, just return Running)
            //   3. If not alive -> Revive
            //   4. If has target and in range -> Attack (Wave or Normal)
            //   5. If has target -> Chase
            //   6. No target -> Patrol

            var root = new SelectorNode();
            
            // Set blackboard BEFORE adding children
            // This is critical: AddChild() will propagate blackboard to children
            root.SetBlackboard(_blackboard);

            // Note: TrueDeath is handled by EnemyMonoBehaviour checking IsTrulyDead
            // and destroying the GameObject
            
            // Stun behavior (highest priority for living enemies)
            // When stunned, just return Running to prevent other behaviors
            // Movement is stopped by StunAction in the action nodes
            
            // Revival behavior (when dead but core alive)
            root.AddChild(new ReviveAction());

            // Combat behavior (target in range)
            var combatSequence = new SequenceNode();
            combatSequence.SetBlackboard(_blackboard); // Set before adding children
            combatSequence.AddChild(new HasTargetCondition());
            combatSequence.AddChild(new InAttackRangeCondition());
            
            var combatSelector = new SelectorNode();
            combatSelector.SetBlackboard(_blackboard); // Set before adding children
            // Try wave attack first (higher priority)
            combatSelector.AddChild(new WaveAttackAction());
            // Fall back to normal attack
            combatSelector.AddChild(new NormalAttackAction());
            
            combatSequence.AddChild(combatSelector);
            root.AddChild(combatSequence);

            // Chase behavior (target detected but not in range)
            var chaseSequence = new SequenceNode();
            chaseSequence.SetBlackboard(_blackboard); // Set before adding children
            chaseSequence.AddChild(new HasTargetCondition());
            chaseSequence.AddChild(new ChaseAction());
            root.AddChild(chaseSequence);

            // Patrol behavior (no target)
            root.AddChild(new PatrolAction());

            return root;
        }
    }
}
