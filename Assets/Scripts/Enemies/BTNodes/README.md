# Behavior Designer Enemy AI 系统

## 快速开始

这是一个完整的 Unity Behavior Designer 敌人 AI 系统，包含所有必要的行为树节点和配置指南。

### 文件说明

| 文件 | 说明 |
|------|------|
| `README.md` | 本文件 - 快速开始指南 |
| `MIGRATION_GUIDE.md` | **详细的迁移指南** - 从自定义 BT 到 Behavior Designer |
| `IMPLEMENTATION_NOTES.md` | **实现细节** - EnemyMonoBehaviour 和 EnemyController 的修改方案 |
| `EnemyTaskBase.cs` | **基类** - 所有 BT 节点的基类 |
| `Actions/` | **动作节点** - 7 个 Action 节点 |
| `Conditions/` | **条件节点** - 6 个 Conditional 节点 |

---

## 节点清单

### Actions（动作节点）

| 节点名称 | 功能 | 文件 |
|---------|------|------|
| `IdleAction` | 待机状态 | `Actions/IdleAction.cs` |
| `PatrolAction` | 巡逻行为（支持航点和随机巡逻） | `Actions/PatrolAction.cs` |
| `ChaseAction` | 追逐玩家 | `Actions/ChaseAction.cs` |
| `NormalAttackAction` | 普通物理攻击 | `Actions/NormalAttackAction.cs` |
| `WaveAttackAction` | 波纹攻击（消耗能量，伤害核心） | `Actions/WaveAttackAction.cs` |
| `ReviveAction` | 复活行为（物理生命耗尽时） | `Actions/ReviveAction.cs` |
| `DeathAction` | 真实死亡（核心生命耗尽时） | `Actions/DeathAction.cs` |

### Conditions（条件节点）

| 节点名称 | 功能 | 文件 |
|---------|------|------|
| `HasTargetCondition` | 检查是否有玩家目标 | `Conditions/HasTargetCondition.cs` |
| `InAttackRangeCondition` | 检查玩家是否在攻击范围内 | `Conditions/InAttackRangeCondition.cs` |
| `NormalAttackCondition` | 检查是否可以普通攻击（冷却） | `Conditions/NormalAttackCondition.cs` |
| `WaveAttackCondition` | 检查是否可以波纹攻击（能量+冷却） | `Conditions/WaveAttackCondition.cs` |
| `PhysicalDeathCondition` | 检查物理生命是否耗尽 | `Conditions/PhysicalDeathCondition.cs` |
| `CoreDeathCondition` | 检查核心生命是否耗尽 | `Conditions/CoreDeathCondition.cs` |

---

## 5 分钟快速配置

### 步骤 1: 为 Enemy 添加组件
1. 选择场景中的 Enemy GameObject
2. 添加 `Behavior Tree` 组件
3. 确保 Enemy 已有 `EnemyMonoBehaviour` 组件

### 步骤 2: 创建行为树资产
1. 在 Project 窗口右键
2. 选择 `Create > Behavior Designer > Behavior Tree`
3. 命名为 `EnemyBT_Basic`

### 步骤 3: 设计基础行为树
双击打开 `EnemyBT_Basic`，按照以下结构创建：

```
Selector (Root)
├─ Sequence [CoreDeath]
│  ├─ CoreDeathCondition
│  └─ DeathAction
│
├─ Sequence [PhysicalDeath]  
│  ├─ PhysicalDeathCondition
│  └─ ReviveAction
│
├─ Sequence [Combat]
│  ├─ HasTargetCondition
│  └─ Selector
│     ├─ Sequence [Attack]
│     │  ├─ InAttackRangeCondition
│     │  ├─ NormalAttackCondition
│     │  └─ NormalAttackAction
│     └─ ChaseAction
│
└─ PatrolAction
```

### 步骤 4: 配置 BehaviorTree 组件
1. 将 `EnemyBT_Basic` 拖到 `External Behavior` 字段
2. 设置 `Update Interval` = 0.15
3. 勾选 `Start When Enabled`

### 步骤 5: 运行测试
1. 按下 Play
2. 打开 `Window > Behavior Designer > Editor`
3. 选择 Enemy 查看行为树执行状态

---

## 推荐的行为树模板

### 基础敌人（近战）
```
Selector
├─ CoreDeath → DeathAction
├─ PhysicalDeath → ReviveAction
├─ Combat
│  ├─ HasTarget
│  └─ Attack OR Chase
└─ Patrol
```

### 高级敌人（带波纹攻击）
```
Selector
├─ CoreDeath → DeathAction
├─ PhysicalDeath → ReviveAction
├─ Combat
│  ├─ HasTarget
│  └─ Selector
│     ├─ WaveAttack (优先)
│     ├─ NormalAttack
│     └─ Chase
└─ Patrol
```

### Boss 敌人（多阶段）
```
Selector
├─ CoreDeath → DeathAction
├─ PhysicalDeath → ReviveAction
├─ EnragedMode (低血量)
│  └─ ...
├─ NormalMode
│  └─ ...
└─ Patrol
```

---

## 调试技巧

### 1. 使用可视化调试
- 运行时打开 Behavior Designer Editor 窗口
- 绿色 = Success
- 红色 = Failure  
- 蓝色 = Running
- 灰色 = 未执行

### 2. 检查节点执行
在节点的 `OnUpdate()` 中添加 Debug.Log：
```csharp
Debug.Log($"[{GetType().Name}] Executing...");
```

### 3. 检查组件状态
在 Inspector 中查看：
- `EnemyMonoBehaviour.IsInitialized`
- `BehaviorTree.enabled`
- Animator 参数值

---

## 常见问题

### Q: 节点在编辑器中找不到？
**A**: 
1. 检查脚本编译是否成功
2. 重启 Unity Editor
3. 在 BD 窗口点击 "Refresh"

### Q: "Controller is null" 错误？
**A**:
1. 确认 BehaviorTree 在 Enemy 根对象上
2. 确认 EnemyMonoBehaviour.IsInitialized = true
3. 在 OnUpdate() 中调用 ValidateComponents()

### Q: Enemy 不移动？
**A**:
1. 检查 NavMeshAgent 组件
2. 检查 MovementSystem 初始化
3. 检查巡逻航点设置

### Q: 攻击没有伤害？
**A**:
1. 检查 DamageHitbox 组件
2. 检查 Animation Events
3. 检查 OnAttackSequenceFinished 事件

---

## 进阶功能

### 自定义节点
继承 `EnemyTaskBase` 创建新节点：
```csharp
using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    [TaskCategory("Resonance/Enemy/Actions")]
    [TaskDescription("Your custom action")]
    public class MyCustomAction : EnemyTaskBase
    {
        public override TaskStatus OnUpdate()
        {
            if (!ValidateComponents())
                return TaskStatus.Failure;
            
            // Your logic here
            
            return TaskStatus.Success;
        }
    }
}
```

### 使用 Shared Variables
在节点中声明共享变量：
```csharp
public SharedFloat attackDamage = 10f;
public SharedBool isAngry = false;
```

### 运行时切换行为树
```csharp
enemyMono.SwitchBehaviorTree(enragedBehaviorTree);
```

---

## 性能优化建议

1. **Update Interval**: 设置为 0.1-0.2 秒（不需要每帧更新）
2. **Component Caching**: 在 OnAwake() 中缓存引用
3. **Conditional Early Exit**: 条件检查尽早返回
4. **Batch Operations**: 批量处理多个 Enemy

---

## 架构说明

```
EnemyMonoBehaviour (Unity GameObject)
    ├─ BehaviorTree Component (Behavior Designer)
    │   └─ Executes BT nodes
    │
    ├─ EnemyController (业务逻辑)
    │   ├─ Stats Management
    │   ├─ Combat System
    │   └─ Movement System
    │
    └─ EnemyAnimator (动画管理)
        └─ Animation Events

BT Nodes (EnemyTaskBase)
    └─ Access Controller via enemyMono.Controller
```

---

## 资源链接

- [Behavior Designer 官方文档](https://opsive.com/support/documentation/behavior-designer/)
- [Behavior Designer 视频教程](https://www.youtube.com/watch?v=T_of4_jRoJA)
- 内部文档：
  - `MIGRATION_GUIDE.md` - 迁移指南
  - `IMPLEMENTATION_NOTES.md` - 实现细节

---

## 联系和支持

遇到问题？查看：
1. `MIGRATION_GUIDE.md` 的常见问题章节
2. `IMPLEMENTATION_NOTES.md` 的问题排查章节
3. Unity Console 的错误信息

祝开发愉快！🎮✨

