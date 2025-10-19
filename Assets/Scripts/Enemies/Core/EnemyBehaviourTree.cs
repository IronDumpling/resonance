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
                var result = _rootNode.Execute();
            }
            else
            {
                Debug.LogWarning("[BT] EnemyBehaviourTree: Root node is null!");
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
            /* Build enemy behavior tree following the IMPROVED design
             * Structure: Root ReactiveSelectorNode (re-evaluates all branches every tick)
             *
             *   Branch 1: [death logic] - highest priority
             *       Sequence: CoreDeathCondition → TrueDeathAction
             *
             *   Branch 2: [revival logic]
             *       Sequence: PhysicalDeathCondition → ReviveAction
             *
             *   Branch 3: [combat logic] - core logic, contains complete combat decision tree
             *       Sequence: 
             *         - HasTargetCondition (check if player is in detection range)
             *         - ReactiveSelectorNode (combat decision: attack vs chase)
             *             ├─ Sequence [attack branch]: InAttackRangeCondition → AttackSelector
             *             │                          └─ ReactiveSelectorNode (select attack type)
             *             │                              ├─ WaveAttackAction (checks CanWaveAttack internally)
             *             │                              └─ NormalAttackAction (checks CanNormalAttack internally)
             *             └─ ChaseAction (if not in attack range, chase the player)
             *
             *   Branch 4: [patrol logic] - lowest priority, fallback behavior
             *       PatrolAction (execute unconditionally, as default behavior)
             *
             * KEY INSIGHT: 
             * CombatSequence is the core of the combat logic, only enters when a player is detected
             * Use ReactiveSelectorNode to ensure all branches are re-evaluated every tick
             */

            var root = new ReactiveSelectorNode();
            root.SetBlackboard(_blackboard);
            Debug.Log("[BT Build] Building improved behavior tree...");

            // ========== Branch 1: Death Logic ==========
            var coreDeathSequence = new SequenceNode();
            coreDeathSequence.SetBlackboard(_blackboard);
            coreDeathSequence.AddChild(new CoreDeathCondition());
            coreDeathSequence.AddChild(new TrueDeathAction());
            root.AddChild(coreDeathSequence);

            // ========== Branch 2: Revival Logic ==========
            var reviveSequence = new SequenceNode();
            reviveSequence.SetBlackboard(_blackboard);
            reviveSequence.AddChild(new PhysicalDeathCondition());
            reviveSequence.AddChild(new ReviveAction());
            root.AddChild(reviveSequence);

            // ========== Branch 3: Combat Logic (complete sub-tree) ==========
            var combatSequence = new SequenceNode();
            combatSequence.SetBlackboard(_blackboard);
            
            // Condition: player is in detection range
            combatSequence.AddChild(new HasTargetCondition());
            
            // Combat decision: attack vs chase
            var combatDecisionSelector = new ReactiveSelectorNode();
            combatDecisionSelector.SetBlackboard(_blackboard);
            
            // --- Branch 3-1: Attack (if in attack range) ---
            var attackSequence = new SequenceNode();
            attackSequence.SetBlackboard(_blackboard);
            attackSequence.AddChild(new InAttackRangeCondition());
            
            // --- Branch 3-1: Attack type selection ---
            var attackSelector = new ReactiveSelectorNode();
            attackSelector.SetBlackboard(_blackboard);
            
            // --- Branch 3-1-1: Wave Attack ---
            var waveAttackSequence = new SequenceNode();
            waveAttackSequence.SetBlackboard(_blackboard);
            waveAttackSequence.AddChild(new WaveAttackCondition());
            waveAttackSequence.AddChild(new WaveAttackAction());
            attackSelector.AddChild(waveAttackSequence);
            
            // --- Branch 3-1-2: Normal Attack (fallback, gains energy) ---
            var normalAttackSequence = new SequenceNode();
            normalAttackSequence.SetBlackboard(_blackboard);
            normalAttackSequence.AddChild(new NormalAttackCondition());
            normalAttackSequence.AddChild(new NormalAttackAction());
            attackSelector.AddChild(normalAttackSequence);
            
            attackSequence.AddChild(attackSelector);
            combatDecisionSelector.AddChild(attackSequence);
            
            // --- Branch 3-2: Chase (if not in attack range) ---
            var chaseSequence = new SequenceNode();
            chaseSequence.SetBlackboard(_blackboard);
            chaseSequence.AddChild(new NotInAttackRangeCondition());
            chaseSequence.AddChild(new ChaseAction());
            combatDecisionSelector.AddChild(chaseSequence);

            combatSequence.AddChild(combatDecisionSelector);
            root.AddChild(combatSequence);

            // ========== Branch 4: Patrol Logic ==========
            var patrolSequence = new SequenceNode();
            patrolSequence.SetBlackboard(_blackboard);
            patrolSequence.AddChild(new NoTargetCondition());
            patrolSequence.AddChild(new PatrolAction());
            root.AddChild(patrolSequence);

            Debug.Log("[BT Build] Behavior tree construction complete!");
            return root;
        }
    }
}
