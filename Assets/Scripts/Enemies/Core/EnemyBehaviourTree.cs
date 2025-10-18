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
            Debug.Log("========== [BT] EnemyBehaviourTree Tick START ==========");
            if (_rootNode != null)
            {
                var result = _rootNode.Execute();
                Debug.Log($"========== [BT] EnemyBehaviourTree Tick END: {result} ==========");
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
            // Build enemy behavior tree following the IMPROVED design
            // Structure: Root ReactiveSelectorNode (re-evaluates all branches every tick)
            //
            //   分支1: [死亡逻辑] - 最高优先级
            //     Sequence: CoreDeathCondition → (ExecuteDeath handled by MonoBehaviour)
            //
            //   分支2: [复活逻辑]
            //     Sequence: PhysicalDeathCondition → ReviveAction
            //
            //   分支3: [战斗逻辑] - 核心逻辑，包含完整的战斗决策树
            //     Sequence: 
            //       - HasTargetCondition (检查玩家是否在侦测范围内)
            //       - ReactiveSelectorNode (战斗决策: 攻击 vs 追逐)
            //           ├─ Sequence [攻击分支]: InAttackRangeCondition → AttackSelector
            //           │                          └─ ReactiveSelectorNode (选择攻击方式)
            //           │                              ├─ Sequence: WaveAttackCondition → WaveAttackAction
            //           │                              └─ NormalAttackAction (fallback)
            //           └─ ChaseAction (fallback: 不在攻击范围就追逐)
            //
            //   分支4: [巡逻逻辑] - 最低优先级，fallback行为
            //     PatrolAction (无条件执行，作为默认行为)
            //
            // KEY INSIGHT: 
            // - Patrol不需要NoTargetCondition，它自然成为最后的fallback
            // - 战斗逻辑被组织成一个完整的子树，只有检测到玩家才会进入
            // - 使用ReactiveSelectorNode确保每帧重新评估所有分支优先级

            var root = new ReactiveSelectorNode();
            root.SetBlackboard(_blackboard);
            Debug.Log("[BT Build] Building improved behavior tree...");

            // ========== 分支1: 死亡逻辑 ==========
            var coreDeathSequence = new SequenceNode();
            coreDeathSequence.SetBlackboard(_blackboard);
            coreDeathSequence.AddChild(new CoreDeathCondition());
            root.AddChild(coreDeathSequence);
            Debug.Log("[BT Build] Branch 1: Core Death - Added");

            // ========== 分支2: 复活逻辑 ==========
            var reviveSequence = new SequenceNode();
            reviveSequence.SetBlackboard(_blackboard);
            reviveSequence.AddChild(new PhysicalDeathCondition());
            reviveSequence.AddChild(new ReviveAction());
            root.AddChild(reviveSequence);
            Debug.Log("[BT Build] Branch 2: Revival - Added");

            // ========== 分支3: 战斗逻辑（完整子树）==========
            var combatSequence = new SequenceNode();
            combatSequence.SetBlackboard(_blackboard);
            
            // 条件: 玩家在侦测范围内
            combatSequence.AddChild(new HasTargetCondition());
            
            // 战斗决策: 攻击 vs 追逐
            var combatDecisionSelector = new ReactiveSelectorNode();
            combatDecisionSelector.SetBlackboard(_blackboard);
            
            // --- 分支3-1: 攻击 (如果在攻击范围内) ---
            var attackSequence = new SequenceNode();
            attackSequence.SetBlackboard(_blackboard);
            attackSequence.AddChild(new InAttackRangeCondition());
            
            // --- 分支3-1: 攻击方式选择 ---
            var attackSelector = new ReactiveSelectorNode();
            attackSelector.SetBlackboard(_blackboard);
            
            // --- 分支3-1-1: Wave Attack ---
            var waveAttackSequence = new SequenceNode();
            waveAttackSequence.SetBlackboard(_blackboard);
            waveAttackSequence.AddChild(new WaveAttackCondition());
            waveAttackSequence.AddChild(new WaveAttackAction());
            attackSelector.AddChild(waveAttackSequence);
            
            // --- 分支3-1-2: Normal Attack ---
            attackSelector.AddChild(new NormalAttackAction());
            
            attackSequence.AddChild(attackSelector);
            combatDecisionSelector.AddChild(attackSequence);
            Debug.Log("[BT Build]   - Combat Decision: Attack branch added");
            
            // --- 分支3-2: 追逐 (不在攻击范围) ---
            combatDecisionSelector.AddChild(new ChaseAction());
            Debug.Log("[BT Build]   - Combat Decision: Chase branch added");
            
            combatSequence.AddChild(combatDecisionSelector);
            root.AddChild(combatSequence);
            Debug.Log("[BT Build] Branch 3: Combat Logic - Added");

            // ========== 分支4: 巡逻逻辑（default fallback）==========
            root.AddChild(new PatrolAction());
            Debug.Log("[BT Build] Branch 4: Patrol (Fallback) - Added");

            Debug.Log("[BT Build] Behavior tree construction complete!");
            return root;
        }
    }
}
