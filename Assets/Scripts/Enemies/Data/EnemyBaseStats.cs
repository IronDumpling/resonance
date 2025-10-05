using UnityEngine;
using DG.Tweening;
using Resonance.Core.Data;

namespace Resonance.Enemies.Data
{
    /// <summary>
    /// 敌人基础属性配置
    /// 定义敌人的基准属性数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Stats", menuName = "Resonance/Enemies/Enemy Stats", order = 1)]
    public class EnemyBaseStats : ScriptableObject
    {
        [Header("基础信息 - Basic Info")]
        [Tooltip("敌人名称")]
        public string enemyName = "Basic Enemy";
        [TextArea(2, 4)]
        [Tooltip("敌人描述")]
        public string enemyDescription = "A basic enemy";
        
        [Header("生存属性 - Survival Attributes")]
        [Tooltip("最大生命值")]
        public float maxHealth = 100f;
        [Tooltip("最大韧性值")]
        public float maxResilience = 50f;
        [Tooltip("眩晕阈值")]
        public float stunThreshold = 10f;
        
        [Header("晶核属性 - Crystal Core Attributes")]
        [Tooltip("晶核配置")]
        public CrystalCoreConfig crystalCoreConfig;
        [Tooltip("晶核波纹")]
        public string corePattern = "Enemy_Basic";
        
        [Header("复活系统 - Revival System")]
        [Tooltip("复活前等待时间")]
        public float revivalDelay = 2f;
        [Tooltip("复活完成时间")]
        public float revivalDuration = 5f;
        [Tooltip("复活时生命恢复速率")]
        public float revivalRate = 20f;
        
        [Header("战斗属性 - Combat Attributes")]
        [Tooltip("普通攻击伤害")]
        public float normalDamage = 20f;
        [Tooltip("晶核攻击伤害")]
        public float coreDamage = 10f;
        [Tooltip("攻击冷却时间")]
        public float attackCooldown = 2f;
        [Tooltip("攻击范围")]
        public float attackRange = 3f;
        [Tooltip("检测范围")]
        public float detectionRange = 8f;
        
        [Header("移动属性 - Movement Attributes")]
        [Tooltip("普通移动速度")]
        public float moveSpeed = 1f;
        [Tooltip("追击移动速度")]
        public float chaseMoveSpeed = 2f;
        [Tooltip("巡逻半径")]
        public float patrolRadius = 5f;
        [Tooltip("到达目标的距离阈值")]
        public float arrivalThreshold = 1.2f;
        
        [Header("视觉效果 - Visual Effects")]
        [Tooltip("正常状态材质路径")]
        public string normalMaterialPath = "Art/Materials/Enemy_Body";
        [Tooltip("受伤状态材质路径")]
        public string damageMaterialPath = "Art/Materials/Damage_Body";
        [Tooltip("复活状态材质路径")]
        public string revivalMaterialPath = "Art/Materials/Revival_Body";
        [Tooltip("受伤闪烁持续时间")]
        public float damageFlashDuration = 0.2f;
        
        [Header("音频配置 - Audio Configuration")]
        [Tooltip("是否启用音频")]
        public bool enableAudio = true;
        
        [Header("QTE配置 - QTE Configuration")]
        [Tooltip("QTE动画缓动类型")]
        public DG.Tweening.Ease qteEaseType = DG.Tweening.Ease.InOutSine;
        [Tooltip("QTE循环持续时间")]
        public float qteCycleDuration = 3f;
        [Tooltip("QTE目标窗口大小")]
        [Range(0.05f, 0.5f)]
        public float qteTargetWindow = 0.2f;
        
        [Header("掉落系统 - Loot System")]
        [Tooltip("死亡时生成的掉落物预制体")]
        public GameObject deathLootPrefab;
        [Tooltip("掉落物数量")]
        [Range(1, 5)]
        public int lootCount = 1;
        [Tooltip("掉落物生成半径")]
        public float lootSpawnRadius = 1.5f;
        [Tooltip("掉落概率")]
        [Range(0f, 1f)]
        public float lootDropChance = 1f;

        [Header("调试选项 - Debug Options")]
        [Tooltip("显示生命条")]
        public bool showHealthBar = true;
        [Tooltip("显示检测范围")]
        public bool showDetectionRange = false;
        [Tooltip("显示攻击范围")]
        public bool showAttackRange = false;

        /// <summary>
        /// 创建运行时属性实例
        /// </summary>
        /// <returns>运行时属性</returns>
        public EnemyRuntimeStats CreateRuntimeStats()
        {
            return new EnemyRuntimeStats(this);
        }

        /// <summary>
        /// 验证配置数据
        /// </summary>
        /// <returns>是否有效</returns>
        public bool ValidateConfig()
        {
            if (string.IsNullOrEmpty(enemyName))
            {
                Debug.LogError($"EnemyBaseStats: {name} has empty enemy name");
                return false;
            }

            if (maxHealth <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid maxHealth: {maxHealth}");
                return false;
            }

            if (maxResilience <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid maxResilience: {maxResilience}");
                return false;
            }

            if (stunThreshold < 0f || stunThreshold >= maxResilience)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid stunThreshold: {stunThreshold}");
                return false;
            }

            if (crystalCoreConfig == null)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has no crystal core config assigned");
                return false;
            }

            if (!crystalCoreConfig.ValidateConfig())
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid crystal core config");
                return false;
            }

            if (normalDamage <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid normal damage: {normalDamage}");
                return false;
            }

            if (moveSpeed <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid move speed: {moveSpeed}");
                return false;
            }

            return true;
        }

        #region Unity Editor

        void OnValidate()
        {
            // 确保数值在合理范围内
            maxHealth = Mathf.Max(1f, maxHealth);
            maxResilience = Mathf.Max(1f, maxResilience);
            stunThreshold = Mathf.Clamp(stunThreshold, 0f, maxResilience - 1f);
            
            // 复活系统验证
            revivalDelay = Mathf.Max(0f, revivalDelay);
            revivalDuration = Mathf.Max(0.1f, revivalDuration);
            revivalRate = Mathf.Max(0.1f, revivalRate);
            
            // 战斗属性验证
            normalDamage = Mathf.Max(0.1f, normalDamage);
            coreDamage = Mathf.Max(0f, coreDamage);
            attackCooldown = Mathf.Max(0.1f, attackCooldown);
            attackRange = Mathf.Max(0.1f, attackRange);
            detectionRange = Mathf.Max(0.1f, detectionRange);
            
            // 移动属性验证
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            chaseMoveSpeed = Mathf.Max(0.1f, chaseMoveSpeed);
            patrolRadius = Mathf.Max(0f, patrolRadius);
            arrivalThreshold = Mathf.Max(0.1f, arrivalThreshold);
            
            // 视觉效果验证
            damageFlashDuration = Mathf.Max(0.1f, damageFlashDuration);
            
            // QTE配置验证
            qteCycleDuration = Mathf.Max(0.5f, qteCycleDuration);
            qteTargetWindow = Mathf.Clamp(qteTargetWindow, 0.05f, 0.5f);
            
            // 掉落系统验证
            lootCount = Mathf.Clamp(lootCount, 1, 5);
            lootSpawnRadius = Mathf.Max(0.5f, lootSpawnRadius);
            lootDropChance = Mathf.Clamp01(lootDropChance);
        }

        #endregion
    }

    /// <summary>
    /// 敌人运行时属性
    /// 游戏过程中可修改的实际属性值
    /// </summary>
    [System.Serializable]
    public class EnemyRuntimeStats
    {
        [Header("生存属性 - Survival Attributes")]
        public float currentHealth;
        public float maxHealth;
        public float currentResilience;
        public float maxResilience;
        public float stunThreshold;
        public float resilienceRegenRate;
        
        [Header("晶核属性 - Crystal Core Attributes")]
        public CrystalCore crystalCore;
        public string corePattern;
        
        [Header("复活系统 - Revival System")]
        public float revivalDelay;
        public float revivalDuration;
        public float revivalRate;
        
        [Header("战斗属性 - Combat Attributes")]
        public float normalDamage;
        public float coreDamage;
        public float attackCooldown;
        public float attackRange;
        public float detectionRange;
        
        [Header("移动属性 - Movement Attributes")]
        public float moveSpeed;
        public float chaseMoveSpeed;
        public float patrolRadius;
        public float arrivalThreshold;
        
        [Header("视觉效果 - Visual Effects")]
        public string normalMaterialPath;
        public string damageMaterialPath;
        public string revivalMaterialPath;
        public float damageFlashDuration;
        
        [Header("音频配置 - Audio Configuration")]
        public bool enableAudio;
        
        [Header("QTE配置 - QTE Configuration")]
        public DG.Tweening.Ease qteEaseType;
        public float qteCycleDuration;
        public float qteTargetWindow;
        
        [Header("掉落系统 - Loot System")]
        public GameObject deathLootPrefab;
        public int lootCount;
        public float lootSpawnRadius;
        public float lootDropChance;
        
        [Header("调试选项 - Debug Options")]
        public bool showHealthBar;
        public bool showDetectionRange;
        public bool showAttackRange;

        [Header("状态等级 - Status Tiers")]
        public EnemyHealthTier healthTier;
        public EnemyResilienceState resilienceState;
        public EnemyLifeState lifeState;

        // 事件系统
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action<float, float> OnResilienceChanged; // current, max
        public System.Action<EnemyHealthTier> OnHealthTierChanged;
        public System.Action<EnemyResilienceState> OnResilienceStateChanged;
        public System.Action<EnemyLifeState> OnLifeStateChanged;
        
        // 属性访问器
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public float ResiliencePercentage => maxResilience > 0 ? currentResilience / maxResilience : 0f;
        public bool IsAlive => lifeState == EnemyLifeState.Alive;
        public bool IsReviving => lifeState == EnemyLifeState.Reviving;
        public bool IsDead => lifeState == EnemyLifeState.Dead;
        public bool IsStunned => resilienceState == EnemyResilienceState.Stunned;
        public bool IsCoreIntact => crystalCore != null && crystalCore.IsIntact;

        public EnemyRuntimeStats(EnemyBaseStats baseStats)
        {
            // 复制生存属性
            maxHealth = baseStats.maxHealth;
            currentHealth = maxHealth; // 开始时满生命值
            maxResilience = baseStats.maxResilience;
            currentResilience = maxResilience; // 开始时满韧性值
            stunThreshold = baseStats.stunThreshold;
            
            // 复制晶核属性
            crystalCore = new CrystalCore(baseStats.crystalCoreConfig);
            crystalCore.SetInfiniteEnergy(); // 敌人拥有无限能量
            crystalCore.SetCorePattern(baseStats.corePattern);
            corePattern = baseStats.corePattern;
            
            // 复制复活系统
            revivalDelay = baseStats.revivalDelay;
            revivalDuration = baseStats.revivalDuration;
            revivalRate = baseStats.revivalRate;
            
            // 复制战斗属性
            normalDamage = baseStats.normalDamage;
            coreDamage = baseStats.coreDamage;
            attackCooldown = baseStats.attackCooldown;
            attackRange = baseStats.attackRange;
            detectionRange = baseStats.detectionRange;
            
            // 复制移动属性
            moveSpeed = baseStats.moveSpeed;
            chaseMoveSpeed = baseStats.chaseMoveSpeed;
            patrolRadius = baseStats.patrolRadius;
            arrivalThreshold = baseStats.arrivalThreshold;
            
            // 复制视觉效果
            normalMaterialPath = baseStats.normalMaterialPath;
            damageMaterialPath = baseStats.damageMaterialPath;
            revivalMaterialPath = baseStats.revivalMaterialPath;
            damageFlashDuration = baseStats.damageFlashDuration;
            
            // 复制音频配置
            enableAudio = baseStats.enableAudio;
            
            // 复制QTE配置
            qteEaseType = baseStats.qteEaseType;
            qteCycleDuration = baseStats.qteCycleDuration;
            qteTargetWindow = baseStats.qteTargetWindow;
            
            // 复制掉落系统
            deathLootPrefab = baseStats.deathLootPrefab;
            lootCount = baseStats.lootCount;
            lootSpawnRadius = baseStats.lootSpawnRadius;
            lootDropChance = baseStats.lootDropChance;
            
            // 复制调试选项
            showHealthBar = baseStats.showHealthBar;
            showDetectionRange = baseStats.showDetectionRange;
            showAttackRange = baseStats.showAttackRange;

            // 初始化状态等级
            UpdateHealthTier();
            UpdateResilienceState();
            UpdateLifeState();
        }

        /// <summary>
        /// 更新生命等级
        /// </summary>
        public void UpdateHealthTier()
        {
            var previousTier = healthTier;
            healthTier = EnemyHealthTierHelper.CalculateHealthTier(HealthPercentage);
            resilienceRegenRate = EnemyHealthTierHelper.GetResilienceRegenRate(healthTier);

            if (previousTier != healthTier)
            {
                OnHealthTierChanged?.Invoke(healthTier);
                Debug.Log($"EnemyRuntimeStats: Health tier changed to {healthTier}");
            }
        }

        /// <summary>
        /// 更新韧性状态
        /// </summary>
        public void UpdateResilienceState()
        {
            var previousState = resilienceState;
            resilienceState = EnemyHealthTierHelper.CalculateResilienceState(currentResilience, stunThreshold);

            if (previousState != resilienceState)
            {
                OnResilienceStateChanged?.Invoke(resilienceState);
                Debug.Log($"EnemyRuntimeStats: Resilience state changed to {resilienceState}");
            }
        }

        /// <summary>
        /// 更新生命状态
        /// </summary>
        public void UpdateLifeState()
        {
            var previousState = lifeState;
            float coreIntegrity = crystalCore?.Integrity ?? 0f;
            lifeState = EnemyHealthTierHelper.CalculateLifeState(currentHealth, coreIntegrity);

            if (previousState != lifeState)
            {
                OnLifeStateChanged?.Invoke(lifeState);
                Debug.Log($"EnemyRuntimeStats: Life state changed to {lifeState}");
            }
        }

        /// <summary>
        /// 受到生命伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <returns>实际造成的伤害</returns>
        public float TakeHealthDamage(float damage)
        {
            if (damage <= 0f || IsDead) return 0f;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            float actualDamage = previousHealth - currentHealth;

            if (actualDamage > 0f)
            {
                UpdateHealthTier();
                UpdateLifeState();
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                Debug.Log($"EnemyRuntimeStats: Took {actualDamage} health damage. Current: {currentHealth}/{maxHealth}");
            }

            return actualDamage;
        }

        /// <summary>
        /// 受到韧性伤害（硬直）
        /// </summary>
        /// <param name="damage">韧性伤害值</param>
        /// <returns>实际造成的韧性损失</returns>
        public float TakeResilienceDamage(float damage)
        {
            if (damage <= 0f) return 0f;

            float previousResilience = currentResilience;
            currentResilience = Mathf.Max(0f, currentResilience - damage);
            float actualDamage = previousResilience - currentResilience;

            if (actualDamage > 0f)
            {
                UpdateResilienceState();
                OnResilienceChanged?.Invoke(currentResilience, maxResilience);
                Debug.Log($"EnemyRuntimeStats: Took {actualDamage} resilience damage. Current: {currentResilience}/{maxResilience}");
            }

            return actualDamage;
        }

        /// <summary>
        /// 晶核受到伤害
        /// </summary>
        /// <param name="damage">晶核容量损失</param>
        /// <returns>实际造成的容量损失</returns>
        public float TakeCoreDamage(float damage)
        {
            if (crystalCore == null || damage <= 0f) return 0f;

            float actualDamage = crystalCore.DamageCapacity(damage);
            if (actualDamage > 0f)
            {
                UpdateLifeState();
                Debug.Log($"EnemyRuntimeStats: Crystal core took {actualDamage} damage. Integrity: {crystalCore.Integrity:P1}");
            }

            return actualDamage;
        }

        /// <summary>
        /// 恢复生命值（复活时使用）
        /// </summary>
        /// <param name="amount">恢复量</param>
        /// <returns>实际恢复的量</returns>
        public float RestoreHealth(float amount)
        {
            if (amount <= 0f) return 0f;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            float actualRestore = currentHealth - previousHealth;

            if (actualRestore > 0f)
            {
                UpdateHealthTier();
                UpdateLifeState();
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                Debug.Log($"EnemyRuntimeStats: Restored {actualRestore} health. Current: {currentHealth}/{maxHealth}");
            }

            return actualRestore;
        }

        /// <summary>
        /// 更新韧性值（自然恢复）
        /// </summary>
        /// <param name="deltaTime">时间间隔</param>
        public void UpdateResilience(float deltaTime)
        {
            if (resilienceRegenRate <= 0f || currentResilience >= maxResilience) return;

            float previousResilience = currentResilience;
            currentResilience = Mathf.Min(currentResilience + resilienceRegenRate * deltaTime, maxResilience);
            
            if (currentResilience != previousResilience)
            {
                UpdateResilienceState();
                OnResilienceChanged?.Invoke(currentResilience, maxResilience);
            }
        }

        /// <summary>
        /// 获取修正后的移动速度
        /// </summary>
        /// <returns>考虑健康等级修正后的移动速度</returns>
        public float GetModifiedMoveSpeed()
        {
            if (!IsAlive) return 0f; // 死亡时无法移动
            
            float speedMultiplier = EnemyHealthTierHelper.GetSpeedMultiplier(healthTier);
            return moveSpeed * speedMultiplier;
        }
        
        /// <summary>
        /// 获取修正后的追击速度
        /// </summary>
        /// <returns>考虑健康等级修正后的追击速度</returns>
        public float GetModifiedChaseMoveSpeed()
        {
            if (!IsAlive) return 0f; // 死亡时无法移动
            
            float speedMultiplier = EnemyHealthTierHelper.GetSpeedMultiplier(healthTier);
            return chaseMoveSpeed * speedMultiplier;
        }

        /// <summary>
        /// 完全恢复生命和韧性
        /// </summary>
        public void FullRestore()
        {
            currentHealth = maxHealth;
            currentResilience = maxResilience;
            crystalCore?.FullRepair();

            UpdateHealthTier();
            UpdateResilienceState();
            UpdateLifeState();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnResilienceChanged?.Invoke(currentResilience, maxResilience);

            Debug.Log("EnemyRuntimeStats: Full restore completed");
        }

        /// <summary>
        /// 获取保存数据
        /// </summary>
        /// <returns>运行时属性保存数据</returns>
        public EnemyRuntimeStatsSaveData GetSaveData()
        {
            return new EnemyRuntimeStatsSaveData
            {
                currentHealth = this.currentHealth,
                currentResilience = this.currentResilience,
                crystalCoreSaveData = crystalCore?.GetSaveData()
            };
        }

        /// <summary>
        /// 从保存数据加载
        /// </summary>
        /// <param name="saveData">保存数据</param>
        public void LoadFromSaveData(EnemyRuntimeStatsSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("EnemyRuntimeStats: Cannot load from null save data");
                return;
            }

            currentHealth = Mathf.Clamp(saveData.currentHealth, 0f, maxHealth);
            currentResilience = Mathf.Clamp(saveData.currentResilience, 0f, maxResilience);

            if (saveData.crystalCoreSaveData != null && crystalCore != null)
            {
                crystalCore.LoadFromSaveData(saveData.crystalCoreSaveData);
            }

            UpdateHealthTier();
            UpdateResilienceState();
            UpdateLifeState();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnResilienceChanged?.Invoke(currentResilience, maxResilience);

            Debug.Log($"EnemyRuntimeStats: Loaded from save data. Health: {currentHealth}/{maxHealth}, Resilience: {currentResilience}/{maxResilience}");
        }

        /// <summary>
        /// 清理事件订阅
        /// </summary>
        public void Cleanup()
        {
            OnHealthChanged = null;
            OnResilienceChanged = null;
            OnHealthTierChanged = null;
            OnResilienceStateChanged = null;
            OnLifeStateChanged = null;
            crystalCore?.Cleanup();
        }
    }

    /// <summary>
    /// 敌人运行时属性保存数据结构
    /// </summary>
    [System.Serializable]
    public class EnemyRuntimeStatsSaveData
    {
        public float currentHealth;
        public float currentResilience;
        public CrystalCoreSaveData crystalCoreSaveData;

        public EnemyRuntimeStatsSaveData()
        {
            currentHealth = 100f;
            currentResilience = 100f;
            crystalCoreSaveData = null;
        }
    }
}
