# Behavior Designer 迁移指南

## 概述

本指南说明如何将自定义 Behavior Tree 系统迁移到 Unity Behavior Designer。

## 已完成的迁移工作

### 1. 创建 EnemyTaskBase 基类
**文件**: `Scripts/Enemies/BTNodes/EnemyTaskBase.cs`

这是所有 Enemy 行为树节点的基类，提供：
- 自动获取 `EnemyMonoBehaviour` 组件
- 缓存 `EnemyController`、`MovementSystem`、`Animator` 引用
- `ValidateComponents()` 方法确保组件已初始化
- 遵循 Behavior Designer 生命周期：`OnAwake()` → `OnStart()` → `OnUpdate()` → `OnEnd()`

### 2. 重写所有 Action 节点

所有 Action 节点现在继承自 `EnemyTaskBase` 而不是 `BehaviorDesigner.Runtime.Tasks.Action`：

| 节点名称 | 文件路径 | 功能 |
|---------|---------|------|
| `IdleAction` | `Actions/IdleAction.cs` | 待机状态 |
| `PatrolAction` | `Actions/PatrolAction.cs` | 巡逻行为 |
| `ChaseAction` | `Actions/ChaseAction.cs` | 追逐玩家 |
| `NormalAttackAction` | `Actions/NormalAttackAction.cs` | 普通攻击 |
| `WaveAttackAction` | `Actions/WaveAttackAction.cs` | 波纹攻击（消耗能量） |
| `ReviveAction` | `Actions/ReviveAction.cs` | 复活行为 |
| `DeathAction` | `Actions/DeathAction.cs` | 真实死亡 |

**关键改进**：
- ✅ 使用 `OnStart()` 进行初始化（每次任务启动时调用）
- ✅ 使用 `OnEnd()` 进行清理（任务结束时调用）
- ✅ 移除 `OnReset()`（Behavior Designer 不使用这个方法）
- ✅ 添加 `[TaskCategory]` 和 `[TaskDescription]` 特性，便于在编辑器中分类和搜索
- ✅ 所有节点都通过 `ValidateComponents()` 验证组件可用性

### 3. 重写所有 Condition 节点

所有 Condition 节点现在继承自 `EnemyTaskBase`：

| 节点名称 | 文件路径 | 功能 |
|---------|---------|------|
| `HasTargetCondition` | `Conditions/HasTargetCondition.cs` | 检查是否有玩家目标 |
| `InAttackRangeCondition` | `Conditions/InAttackRangeCondition.cs` | 检查玩家是否在攻击范围内 |
| `NormalAttackCondition` | `Conditions/NormalAttackCondition.cs` | 检查是否可以普通攻击（冷却） |
| `WaveAttackCondition` | `Conditions/WaveAttackCondition.cs` | 检查是否可以波纹攻击（能量+冷却） |
| `PhysicalDeathCondition` | `Conditions/PhysicalDeathCondition.cs` | 检查物理生命值是否耗尽 |
| `CoreDeathCondition` | `Conditions/CoreDeathCondition.cs` | 检查核心生命值是否耗尽 |

**关键改进**：
- ✅ 简洁的条件检查逻辑
- ✅ 返回 `TaskStatus.Success` 或 `TaskStatus.Failure`
- ✅ 添加分类和描述特性

---

## 需要完成的后续工作

### 4. 修改 EnemyMonoBehaviour.cs

**目标**：确保 Behavior Designer 的 BehaviorTree 组件能够正确访问 Enemy 组件。

#### 需要做的修改：

1. **（可选）添加 BehaviorTree 组件引用**
   ```csharp
   using BehaviorDesigner.Runtime;
   
   [Header("Behavior Tree")]
   [SerializeField] private BehaviorTree behaviorTree;
   ```

2. **在 Awake/Start 中验证 BehaviorTree**
   ```csharp
   void Start()
   {
       // ... 现有代码 ...
       
       // 验证 BehaviorTree 组件
       if (behaviorTree == null)
       {
           behaviorTree = GetComponent<BehaviorTree>();
           if (behaviorTree == null)
           {
               Debug.LogError($"EnemyMonoBehaviour: No BehaviorTree component found on {gameObject.name}!");
           }
       }
   }
   ```

3. **（可选）添加暂停/恢复行为树的方法**
   ```csharp
   /// <summary>
   /// 暂停行为树执行
   /// </summary>
   public void PauseBehaviorTree()
   {
       if (behaviorTree != null)
       {
           behaviorTree.DisableBehavior();
       }
   }
   
   /// <summary>
   /// 恢复行为树执行
   /// </summary>
   public void ResumeBehaviorTree()
   {
       if (behaviorTree != null)
       {
           behaviorTree.EnableBehavior();
       }
   }
   ```

4. **移除旧的自定义 BT 系统**（如果存在）
   - 删除任何旧的 BT 更新逻辑
   - 移除自定义的 BT runner/executor

**注意**：`EnemyMonoBehaviour` 主要作为桥接层，无需大量修改。Behavior Designer 会自动处理行为树的执行。

---

### 5. 修改 EnemyController.cs

**目标**：确保 Controller 提供的所有接口都能被 BT 节点正确调用。

#### 检查清单：

1. **确认所有属性和方法都是 public**
   - ✅ `HasPlayerTarget`
   - ✅ `IsPlayerInAttackRange()`
   - ✅ `CanNormalAttack`
   - ✅ `CanWaveAttack`
   - ✅ `IsPhysicallyDead`
   - ✅ `IsCoreDead`
   - ✅ `IsPhysicallyAlive`
   - ✅ 等等...

2. **（可选）添加 BT 专用的辅助方法**
   ```csharp
   /// <summary>
   /// 检查是否可以执行任何行为（不在死亡/复活状态）
   /// </summary>
   public bool CanPerformAction
   {
       get
       {
           return IsPhysicallyAlive && !IsCoreDead;
       }
   }
   ```

3. **确保事件系统正常工作**
   - ✅ `OnAttackSequenceFinished` 事件被正确触发
   - ✅ Attack actions 订阅和取消订阅事件
   - ✅ 动画事件正确触发

**注意**：根据现有的 `EnemyController` 代码，它已经设计得很好，应该不需要太多修改。

---

## 在 Unity Editor 中设置 Behavior Tree

### 1. 为 Enemy 添加 BehaviorTree 组件

1. 选择场景中的 Enemy GameObject
2. 点击 `Add Component`
3. 搜索 "Behavior Tree"
4. 添加 `Behavior Tree` 组件

### 2. 创建 External Behavior Tree 资产

1. 在 `Resources/Data/Enemies/` 文件夹中右键
2. 选择 `Create > Behavior Designer > Behavior Tree`
3. 命名为 `EnemyBT_[EnemyType]`（例如：`EnemyBT_Soldier`）

### 3. 设计行为树结构

推荐的行为树结构（优先级从上到下）：

```
Root (Selector)
├─ Sequence [Priority 1: Core Death]
│  ├─ CoreDeathCondition
│  └─ DeathAction
│
├─ Sequence [Priority 2: Physical Death]
│  ├─ PhysicalDeathCondition
│  └─ ReviveAction
│
├─ Sequence [Priority 3: Combat]
│  ├─ HasTargetCondition
│  ├─ Selector
│  │  ├─ Sequence [Wave Attack]
│  │  │  ├─ InAttackRangeCondition
│  │  │  ├─ WaveAttackCondition
│  │  │  └─ WaveAttackAction
│  │  │
│  │  ├─ Sequence [Normal Attack]
│  │  │  ├─ InAttackRangeCondition
│  │  │  ├─ NormalAttackCondition
│  │  │  └─ NormalAttackAction
│  │  │
│  │  └─ ChaseAction
│
└─ PatrolAction [Default Behavior]
```

### 4. 在 Inspector 中配置

1. 将创建的 External Behavior Tree 拖到 `Behavior Tree` 组件的 `External Behavior` 字段
2. 设置 `Update Interval`（推荐 0.1-0.2 秒）
3. 勾选 `Start When Enabled`

---

## 行为树节点说明

### Actions（动作节点）

| 节点 | 返回值 | 说明 |
|-----|--------|------|
| `IdleAction` | Running | 待机，持续返回 Running 直到被条件打断 |
| `PatrolAction` | Running | 巡逻，持续返回 Running |
| `ChaseAction` | Running | 追逐玩家，持续返回 Running |
| `NormalAttackAction` | Running → Success | 执行攻击动画，完成后返回 Success |
| `WaveAttackAction` | Running → Success | 执行波纹攻击，完成后返回 Success |
| `ReviveAction` | Running → Success | 复活过程，完成后返回 Success |
| `DeathAction` | Running → Success | 死亡动画，延迟后返回 Success |

### Conditionals（条件节点）

| 节点 | 返回值 | 说明 |
|-----|--------|------|
| `HasTargetCondition` | Success/Failure | 有玩家目标返回 Success |
| `InAttackRangeCondition` | Success/Failure | 玩家在攻击范围内返回 Success |
| `NormalAttackCondition` | Success/Failure | 可以普通攻击返回 Success |
| `WaveAttackCondition` | Success/Failure | 可以波纹攻击返回 Success |
| `PhysicalDeathCondition` | Success/Failure | 物理生命值耗尽返回 Success |
| `CoreDeathCondition` | Success/Failure | 核心生命值耗尽返回 Success |

---

## 常见问题和解决方案

### Q1: 节点在编辑器中找不到？

**A**: 确保：
1. 所有节点文件都在 `Scripts/Enemies/BTNodes/` 目录下
2. 脚本编译没有错误
3. 重启 Unity Editor
4. 在 Behavior Designer 窗口中点击 "Refresh"

### Q2: 节点执行时报 "Controller is null" 错误？

**A**: 检查：
1. Enemy GameObject 上是否有 `EnemyMonoBehaviour` 组件
2. `EnemyMonoBehaviour.IsInitialized` 是否为 true
3. Behavior Tree 组件是否附加在 Enemy 根 GameObject 上（不是子对象）

### Q3: 动画不播放？

**A**: 检查：
1. Animator 组件是否在子对象上
2. Animator Controller 是否正确设置
3. 参数名称是否匹配（HasTarget, InAttackRange, Speed 等）
4. Animation events 是否正确设置

### Q4: 攻击动画播放但伤害不生效？

**A**: 检查：
1. DamageHitbox 是否正确设置
2. Animation Event 是否调用 `EnableDamageHitbox()` / `DisableDamageHitbox()`
3. EnemyAnimator 组件是否正确初始化
4. 攻击结束是否调用 `OnAttackSequenceFinished` 事件

---

## Behavior Designer 最佳实践

### 1. 节点设计原则
- **单一职责**：每个节点只做一件事
- **无状态**：尽量避免在节点中存储复杂状态，使用 Controller 管理状态
- **可重用**：设计通用的节点，通过参数控制行为

### 2. 性能优化
- 使用 `Update Interval` 减少更新频率（不需要每帧都执行）
- 在 `OnAwake()` 中缓存组件引用
- 避免在 `OnUpdate()` 中进行昂贵的查找操作

### 3. 调试技巧
- 使用 Behavior Designer 的可视化调试（绿色表示 Success，红色表示 Failure）
- 添加 Debug.Log 查看节点执行流程
- 使用 `[InspectorLabel]` 特性为变量添加友好的名称

---

## 总结

迁移工作主要包括：
1. ✅ 创建 `EnemyTaskBase` 基类
2. ✅ 重写所有 Action 节点
3. ✅ 重写所有 Condition 节点
4. ⏳ 微调 `EnemyMonoBehaviour`（可选）
5. ⏳ 验证 `EnemyController` 接口
6. ⏳ 在 Unity Editor 中设置 Behavior Tree

**优势**：
- 🎨 可视化编辑行为树，无需修改代码
- 🔧 方便调试和调整 AI 行为
- 📦 可重用的节点库
- 🚀 更好的性能（Behavior Designer 内部优化）
- 👥 团队协作友好（设计师可以调整 BT）

祝迁移顺利！有问题随时查阅本指南。

