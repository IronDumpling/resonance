基于代码分析，我发现了以下几个主要问题和架构混乱的地方：

1. **行为树结构问题**：
   - 行为树的分支优先级可能存在问题。当前结构是：死亡 > 复活 > 战斗 > 巡逻
   - 但在战斗分支中，攻击和追击的选择使用了 ReactiveSelectorNode，这意味着每一帧都会重新评估，可能导致行为抖动
   - 攻击序列中的状态转换不够清晰，可能导致卡在某个状态

2. **状态管理混乱**：
   - EnemyController 中混合了太多职责：状态管理、战斗逻辑、巡逻逻辑等
   - 状态转换逻辑分散在多个地方：Controller、BehaviourTree、Animation
   - 特别是 InAttackRange 这个状态在多个地方被修改：
     - 触发器系统 (EnemyController.SetPlayerInAttackRange)
     - 行为树 (NormalAttackAction)
     - 动画系统 (Animator参数)

3. **动画系统与行为树同步问题**：
   ```csharp
   // NormalAttackAction.cs
   animator.SetBool("InAttackRange", true);  // 在攻击开始时设置
   animator.SetTrigger("NormalAttackStart"); // 触发攻击动画
   
   // Reset时
   animator.SetBool("InAttackRange", false); // 重置状态
   ```
   这里的问题是：
   - InAttackRange 被用作两个目的：表示实际的攻击范围和控制动画状态机
   - 动画完成和行为树状态可能不同步，导致卡死

4. **追击逻辑问题**：
   ```csharp
   // ChaseAction.cs
   animator.SetBool("HasTarget", true);
   animator.SetBool("InAttackRange", false);
   ```
   - Chase动作持续设置 InAttackRange = false，可能与实际的触发器检测冲突
   - 没有明确的追击退出条件，可能导致卡在追击状态

5. **事件系统过于复杂**：
   - EnemyController 中有大量的事件
   - 事件的触发和处理分散在多个类中
   - 可能存在事件清理不完整的情况

建议的改进方案：

1. **简化状态管理**：
   ```csharp
   public enum EnemyActionState {
       Idle,
       Patrol,
       Chase,
       PrepareAttack,
       Attacking,
       AttackCooldown,
       Dead,
       Reviving
   }
   ```
   - 创建明确的状态枚举
   - 将状态转换集中在一个地方管理
   - 分离动画状态和逻辑状态

2. **重构行为树结构**：
   ```csharp
   // 使用普通SelectorNode而不是ReactiveSelectorNode
   var combatDecisionSelector = new SelectorNode();
   
   // 添加明确的状态检查
   attackSequence.AddChild(new StateCheckNode(EnemyActionState.PrepareAttack));
   ```

3. **分离动画控制**：
   ```csharp
   public class EnemyAnimationController {
       private Animator _animator;
       private EnemyActionState _currentState;
       
       public void UpdateState(EnemyActionState newState) {
           _currentState = newState;
           UpdateAnimatorParameters();
       }
       
       private void UpdateAnimatorParameters() {
           switch (_currentState) {
               case EnemyActionState.Chase:
                   _animator.SetBool("HasTarget", true);
                   _animator.SetBool("InAttackRange", false);
                   break;
               case EnemyActionState.PrepareAttack:
                   _animator.SetBool("HasTarget", true);
                   _animator.SetBool("InAttackRange", true);
                   break;
           }
       }
   }
   ```

4. **简化事件系统**：
   - 减少事件数量，只保留必要的核心事件
   - 使用状态变化事件替代具体行为事件
   - 确保所有事件都有配对的注册/注销

5. **改进调试系统**：
   ```csharp
   public class EnemyDebugger {
       public void LogStateTransition(EnemyActionState from, EnemyActionState to) { }
       public void LogAnimationState(string stateName, bool entered) { }
       public void LogBehaviorTreeDecision(string nodeName, BTNodeStatus status) { }
   }
   ```

针对当前的卡死问题，我建议：

1. 检查 NormalAttackAction 中的状态重置：
   ```csharp
   public override void Reset() {
       base.Reset();
       
       // 确保清理所有状态
       _attackLaunched = false;
       _sequenceFinished = false;
       
       // 取消事件订阅
       Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
       
       // 重置动画状态
       var animator = GetAnimator();
       if (animator != null) {
           animator.SetBool("InAttackRange", false);
           animator.ResetTrigger("NormalAttackStart");
       }
       
       // 重置控制器状态
       Controller.SetPlayerInAttackRange(false);
   }
   ```

2. 在 ChaseAction 中添加状态检查：
   ```csharp
   public override BTNodeStatus Execute() {
       // 检查是否真的应该在追击状态
       if (Controller.IsPlayerInAttackRange()) {
           Debug.LogWarning("ChaseAction: Player in attack range but still chasing!");
           return BTNodeStatus.Failure;
       }
       
       // ... 其余代码 ...
   }
   ```

3. 添加超时机制：
   ```csharp
   public class ActionNode {
       protected float _actionTimeout = 5f; // 5秒超时
       protected float _actionTimer = 0f;
       
       public override BTNodeStatus Execute() {
           _actionTimer += Time.deltaTime;
           if (_actionTimer >= _actionTimeout) {
               Debug.LogWarning($"{GetType().Name}: Action timed out!");
               Reset();
               return BTNodeStatus.Failure;
           }
           return BTNodeStatus.Running;
       }
   }
   ```

这些改进应该能帮助解决当前的卡死问题。建议先实现调试系统，这样可以更容易地跟踪状态转换和行为树决策。然后逐步实现其他改进。