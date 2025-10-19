# EnemyController 和 EnemyMonoBehaviour 修改指南

## 1. EnemyMonoBehaviour.cs 修改方案

### 需要添加的内容

#### 1.1 添加命名空间
```csharp
using BehaviorDesigner.Runtime;  // 添加这一行
```

#### 1.2 在类开头添加字段
```csharp
[Header("Behavior Tree")]
[SerializeField] private BehaviorTree _behaviorTree;
[Tooltip("Reference to the Behavior Designer BehaviorTree component")]
```

#### 1.3 在 Start() 方法中添加验证
```csharp
void Start()
{
    // ... 现有代码 ...
    
    // 验证 BehaviorTree 组件
    ValidateBehaviorTree();
    
    Debug.Log($"EnemyMonoBehaviour: {gameObject.name} started successfully");
}

private void ValidateBehaviorTree()
{
    if (_behaviorTree == null)
    {
        _behaviorTree = GetComponent<BehaviorTree>();
    }
    
    if (_behaviorTree == null)
    {
        Debug.LogError($"EnemyMonoBehaviour: No BehaviorTree component found on {gameObject.name}! " +
                      "Please add a BehaviorTree component to use Behavior Designer.");
    }
    else
    {
        Debug.Log($"EnemyMonoBehaviour: BehaviorTree component found and ready on {gameObject.name}");
    }
}
```

#### 1.4 添加 BehaviorTree 控制方法（Public Utility Methods 区域）
```csharp
#region Behavior Tree Control

/// <summary>
/// 暂停行为树执行（例如在对话、过场动画时）
/// </summary>
public void PauseBehaviorTree()
{
    if (_behaviorTree != null && _behaviorTree.enabled)
    {
        _behaviorTree.DisableBehavior();
        Debug.Log($"EnemyMonoBehaviour: Behavior tree paused for {gameObject.name}");
    }
}

/// <summary>
/// 恢复行为树执行
/// </summary>
public void ResumeBehaviorTree()
{
    if (_behaviorTree != null && !_behaviorTree.enabled)
    {
        _behaviorTree.EnableBehavior();
        Debug.Log($"EnemyMonoBehaviour: Behavior tree resumed for {gameObject.name}");
    }
}

/// <summary>
/// 重启行为树（重新从根节点开始）
/// </summary>
public void RestartBehaviorTree()
{
    if (_behaviorTree != null)
    {
        _behaviorTree.RestartWhenComplete = true;
        _behaviorTree.DisableBehavior();
        _behaviorTree.EnableBehavior();
        Debug.Log($"EnemyMonoBehaviour: Behavior tree restarted for {gameObject.name}");
    }
}

/// <summary>
/// 检查行为树是否正在运行
/// </summary>
public bool IsBehaviorTreeRunning => _behaviorTree != null && _behaviorTree.enabled;

#endregion
```

#### 1.5 在 OnDestroy() 中清理
```csharp
void OnDestroy()
{
    OnEnemyDestroyed?.Invoke();
    
    // 停止行为树
    if (_behaviorTree != null && _behaviorTree.enabled)
    {
        _behaviorTree.DisableBehavior();
    }
    
    if (_isInitialized)
    {
        _enemyController?.Shutdown();
    }
}
```

### 可选的增强功能

#### 1.6 动态切换行为树（高级功能）
```csharp
/// <summary>
/// 切换到不同的行为树（例如：愤怒模式、逃跑模式）
/// </summary>
public void SwitchBehaviorTree(ExternalBehavior newBehavior)
{
    if (_behaviorTree != null && newBehavior != null)
    {
        _behaviorTree.ExternalBehavior = newBehavior;
        _behaviorTree.DisableBehavior();
        _behaviorTree.EnableBehavior();
        Debug.Log($"EnemyMonoBehaviour: Switched to new behavior tree for {gameObject.name}");
    }
}
```

---

## 2. EnemyController.cs 修改方案

根据现有的 `EnemyController` 代码分析，它已经提供了完善的接口，**几乎不需要修改**。

### 需要验证的内容

#### 2.1 确保所有 BT 节点需要的属性和方法都是 public

检查以下成员的访问修饰符（应该都是 public 或有 public getter）：

**状态检查属性**：
- ✅ `HasPlayerTarget`
- ✅ `IsPhysicallyAlive`
- ✅ `IsPhysicallyDead`
- ✅ `IsCoreDead`
- ✅ `IsPaused`

**攻击相关属性**：
- ✅ `CanNormalAttack`
- ✅ `CanWaveAttack`

**配置属性**：
- ✅ `TargetUpdateInterval`
- ✅ `WaitAtWaypointDuration`

**方法**：
- ✅ `IsPlayerInAttackRange()`
- ✅ `LaunchNormalAttack()`
- ✅ `LaunchWaveAttack()`
- ✅ `SetPlayerTarget(Transform)`
- ✅ `LosePlayer()`
- ✅ `SetPlayerInAttackRange(bool)`
- ✅ `GeneratePatrolPoint()`
- ✅ `SetPatrolTarget(Vector3)`
- ✅ `HasPatrolWaypoints()`
- ✅ `SwitchPatrolDirection()`
- ✅ `StopPatrol()`

**事件**：
- ✅ `OnAttackSequenceFinished`

### 可选的增强功能

#### 2.2 添加便捷属性（可选）
```csharp
/// <summary>
/// 检查敌人是否可以执行任何主动行为（不在死亡/复活/暂停状态）
/// </summary>
public bool CanPerformActions
{
    get
    {
        return IsPhysicallyAlive && !IsCoreDead && !IsPaused;
    }
}

/// <summary>
/// 检查敌人是否应该进入战斗状态
/// </summary>
public bool ShouldEnterCombat
{
    get
    {
        return HasPlayerTarget && IsPhysicallyAlive && !IsCoreDead;
    }
}

/// <summary>
/// 获取当前的主要状态（用于调试）
/// </summary>
public string CurrentMainState
{
    get
    {
        if (IsCoreDead) return "CoreDead";
        if (IsPhysicallyDead) return "PhysicalDead";
        if (HasPlayerTarget && IsPlayerInAttackRange()) return "InCombat";
        if (HasPlayerTarget) return "Chasing";
        if (IsPatrolling) return "Patrolling";
        return "Idle";
    }
}
```

#### 2.3 添加调试辅助方法（可选）
```csharp
/// <summary>
/// 获取完整的状态信息（用于 Behavior Designer 的调试面板）
/// </summary>
public string GetDetailedStatus()
{
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine($"=== Enemy Status: {_transform.name} ===");
    sb.AppendLine($"Main State: {CurrentMainState}");
    sb.AppendLine($"Physical Health: {Stats.currentHealth:F1}/{Stats.maxHealth:F1}");
    sb.AppendLine($"Core Health: {Stats.crystalCore.CurrentCoreHealth:F1}/{Stats.crystalCore.MaxCoreHealth:F1}");
    sb.AppendLine($"Core Energy: {Stats.crystalCore.CurrentEnergy:F1}/{Stats.crystalCore.MaxEnergy:F1}");
    sb.AppendLine($"Has Target: {HasPlayerTarget}");
    sb.AppendLine($"In Attack Range: {IsPlayerInAttackRange()}");
    sb.AppendLine($"Can Normal Attack: {CanNormalAttack}");
    sb.AppendLine($"Can Wave Attack: {CanWaveAttack}");
    sb.AppendLine($"Is Patrolling: {IsPatrolling}");
    return sb.ToString();
}
```

---

## 3. 不需要修改的部分

### 3.1 MovementSystem
`MovementSystem` 类已经提供了完善的移动接口，BT 节点通过 `EnemyTaskBase.Movement` 访问即可。

### 3.2 EnemyAnimator
`EnemyAnimator` 类处理动画事件和参数设置，BT 节点直接访问 `Animator` 组件即可。

### 3.3 触发器系统
`EnemyTrigger`、`EnemyDamageHitbox` 等组件继续正常工作，无需修改。

---

## 4. 代码组织建议

### 4.1 文件结构（无需改动）
```
Scripts/Enemies/
├── BTNodes/
│   ├── EnemyTaskBase.cs          ← 新增的基类
│   ├── MIGRATION_GUIDE.md        ← 迁移指南
│   ├── IMPLEMENTATION_NOTES.md   ← 本文件
│   ├── Actions/
│   │   ├── IdleAction.cs
│   │   ├── PatrolAction.cs
│   │   ├── ChaseAction.cs
│   │   ├── NormalAttackAction.cs
│   │   ├── WaveAttackAction.cs
│   │   ├── ReviveAction.cs
│   │   └── DeathAction.cs
│   └── Conditions/
│       ├── HasTargetCondition.cs
│       ├── InAttackRangeCondition.cs
│       ├── NormalAttackCondition.cs
│       ├── WaveAttackCondition.cs
│       ├── PhysicalDeathCondition.cs
│       └── CoreDeathCondition.cs
├── Core/
│   ├── EnemyController.cs        ← 几乎不需要修改
│   └── EnemyAnimator.cs          ← 不需要修改
├── EnemyMonoBehaviour.cs         ← 需要添加 BehaviorTree 引用
├── Movement/
│   └── MovementSystem.cs         ← 不需要修改
└── ... (其他文件不需要修改)
```

---

## 5. 测试清单

完成修改后，使用以下清单测试：

### 5.1 基础功能测试
- [ ] Enemy GameObject 上添加了 BehaviorTree 组件
- [ ] 创建了 External Behavior Tree 资产
- [ ] 在 Behavior Tree 编辑器中能看到所有自定义节点
- [ ] 节点显示在 "Resonance/Enemy/Actions" 和 "Resonance/Enemy/Conditions" 分类下

### 5.2 运行时测试
- [ ] Enemy 正常初始化（Console 没有错误）
- [ ] 待机状态正常（IdleAction 或 PatrolAction 执行）
- [ ] 检测到玩家后开始追逐（ChaseAction）
- [ ] 进入攻击范围后开始攻击（NormalAttackAction）
- [ ] 攻击命中玩家并造成伤害
- [ ] 攻击冷却正常工作
- [ ] 波纹攻击正常执行（如果有能量）
- [ ] 物理生命值耗尽后开始复活（ReviveAction）
- [ ] 核心生命值耗尽后真实死亡（DeathAction）

### 5.3 Behavior Designer 调试
- [ ] 在运行时打开 Behavior Designer 窗口
- [ ] 观察节点执行流程（绿色/红色/蓝色）
- [ ] 验证条件节点的返回值正确
- [ ] 验证动作节点的状态转换正确

### 5.4 性能测试
- [ ] 场景中有多个 Enemy 时性能正常
- [ ] CPU 使用率在可接受范围内
- [ ] 没有明显的卡顿或延迟

---

## 6. 常见问题排查

### 问题 1: "Controller is null" 错误
**原因**：BT 节点在 Enemy 初始化之前尝试访问 Controller

**解决方案**：
- 在 `EnemyMonoBehaviour.Awake()` 中完成初始化
- 在 BT 节点的 `OnUpdate()` 中始终调用 `ValidateComponents()`
- 设置 BehaviorTree 组件的 `Start When Enabled = false`，在 `EnemyMonoBehaviour.Start()` 结束后手动启用

### 问题 2: 节点不执行或卡住
**原因**：节点返回了错误的 TaskStatus

**解决方案**：
- 检查 Action 节点是否正确返回 Running/Success/Failure
- 检查 Condition 节点是否正确返回 Success/Failure
- 使用 Behavior Designer 的可视化调试查看节点状态

### 问题 3: 动画不播放
**原因**：Animator 组件引用或参数设置错误

**解决方案**：
- 确认 Animator 组件在子对象上
- 检查 Animator Controller 中的参数名称
- 验证 Animation Event 设置正确

### 问题 4: 攻击没有伤害
**原因**：DamageHitbox 没有正确启用或动画事件未触发

**解决方案**：
- 检查 `OnAttackSequenceFinished` 事件是否被触发
- 验证 Animation Event 调用了正确的方法
- 确认 EnemyDamageHitbox 组件正确初始化

---

## 7. 下一步计划

1. **完成 EnemyMonoBehaviour 修改**
   - 添加 BehaviorTree 引用
   - 添加控制方法
   - 测试基础功能

2. **在 Unity Editor 中设置第一个 Enemy**
   - 添加 BehaviorTree 组件
   - 创建 External Behavior Tree
   - 设计基础行为树结构

3. **测试和调试**
   - 运行场景测试所有功能
   - 使用 Behavior Designer 可视化调试
   - 修复发现的问题

4. **创建行为树模板**
   - 为不同类型的 Enemy 创建不同的 BT 模板
   - 例如：近战敌人、远程敌人、Boss 等

5. **优化和扩展**
   - 添加更多行为节点（如：Flee、Guard、Support 等）
   - 优化性能
   - 编写更详细的文档

---

## 总结

**最小改动方案**（推荐）：
- ✅ 已完成所有 BT 节点的重写
- ⏳ `EnemyMonoBehaviour`: 只需添加 BehaviorTree 引用和验证逻辑
- ✅ `EnemyController`: 无需修改（已经很完善）
- ⏳ 在 Unity Editor 中配置 BehaviorTree 组件

**核心优势**：
- 代码侵入性最小
- 保持现有架构不变
- 充分利用 Behavior Designer 的可视化优势
- 易于团队协作和维护

祝开发顺利！🚀

