using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Enemies;
using Resonance.Utilities;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Enemies.Data
{
    /// <summary>
    /// Enemy runtime stats
    /// 游戏过程中可修改的实际属性值
    /// </summary>
    [System.Serializable]
    public class EnemyRuntimeStats
    {
        [Header("Survival Attributes")]
        public float currentHealth;
        public float maxHealth;
        public float chaosRecoveryRate;
        
        [Header("Crystal Core Attributes")]
        public CrystalCore crystalCore;
        public string corePattern;
        
        [Header("Revival System")]
        public float revivalDelay;
        public float revivalRate;
        
        [Header("Combat Attributes")]
        public AttackStats normalAttackStats;
        public AttackStats coreAttackStats;
        public float detectionRange;
        
        [Header("Hitbox Damage Multipliers")]
        public List<HitboxMultiplierConfig> hitboxMultipliers;
        
        [Header("Movement Attributes")]
        public float moveSpeed;
        public float chaseMoveSpeed;
        public float patrolRadius;
        public float arrivalThreshold;
        
        [Header("Visual Effects")]
        public string normalMaterialPath;
        public string damageMaterialPath;
        public string revivalMaterialPath;
        public float damageFlashDuration;
        
        [Header("Audio Configuration")]
        public bool enableAudio;
        
        [Header("Loot System")]
        public GameObject deathLootPrefab;
        public int lootCount;
        public float lootSpawnRadius;
        public float lootDropChance;
        
        [Header("Debug Options")]
        public bool showHealthBar;
        public bool showDetectionRange;
        public bool showAttackRange;

        [Header("Status Tiers")]
        public HealthTier healthTier;
        public EnemyLifeState lifeState;

        // 事件系统
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action<HealthTier> OnHealthTierChanged;
        public System.Action<EnemyLifeState> OnLifeStateChanged;
        
        // 属性访问器
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsAlive => lifeState == EnemyLifeState.Alive;
        public bool IsReviving => lifeState == EnemyLifeState.Reviving;
        public bool IsDead => lifeState == EnemyLifeState.Dead;
        public bool IsCoreIntact => crystalCore != null && crystalCore.CoreHealthState == CoreHealthState.Intact;

        public EnemyRuntimeStats(EnemyBaseStats baseStats)
        {
            // 复制生存属性
            maxHealth = baseStats.maxHealth;
            currentHealth = maxHealth;
            
            // 复制晶核属性
            crystalCore = new CrystalCore(baseStats.crystalCoreConfig);
            crystalCore.SetFullEnergy(); // 敌人拥有满能量
            corePattern = baseStats.corePattern;
            
            // 复活系统
            revivalDelay = baseStats.revivalDelay;
            revivalRate = baseStats.revivalRate;
            
            // 战斗属性 - Clone to avoid sharing damages reference with other instances
            normalAttackStats = baseStats.normalAttackStats.Clone();
            coreAttackStats = baseStats.coreAttackStats.Clone();
            detectionRange = baseStats.detectionRange;
            
            // Hitbox multipliers - Copy the list for independent configuration
            hitboxMultipliers = new List<HitboxMultiplierConfig>(baseStats.hitboxMultipliers ?? new List<HitboxMultiplierConfig>());
            
            // 移动属性
            moveSpeed = baseStats.moveSpeed;
            chaseMoveSpeed = baseStats.chaseMoveSpeed;
            patrolRadius = baseStats.patrolRadius;
            arrivalThreshold = baseStats.arrivalThreshold;
            
            // 视觉效果
            normalMaterialPath = baseStats.normalMaterialPath;
            damageMaterialPath = baseStats.damageMaterialPath;
            revivalMaterialPath = baseStats.revivalMaterialPath;
            damageFlashDuration = baseStats.damageFlashDuration;
            
            // 音频配置
            enableAudio = baseStats.enableAudio;
            
            // 掉落系统
            deathLootPrefab = baseStats.deathLootPrefab;
            lootCount = baseStats.lootCount;
            lootSpawnRadius = baseStats.lootSpawnRadius;
            lootDropChance = baseStats.lootDropChance;
            
            // 调试选项
            showHealthBar = baseStats.showHealthBar;
            showDetectionRange = baseStats.showDetectionRange;
            showAttackRange = baseStats.showAttackRange;

            // 初始化状态等级
            UpdateHealthTier();
            UpdateLifeState();
        }

        /// <summary>
        /// 更新生命等级
        /// </summary>
        public void UpdateHealthTier()
        {
            var previousTier = healthTier;
            healthTier = HealthTierHelper.CalculateHealthTier(HealthPercentage);
            chaosRecoveryRate = HealthTierHelper.GetChaosRecoveryRate(healthTier);

            if (previousTier != healthTier)
            {
                OnHealthTierChanged?.Invoke(healthTier);
                Debug.Log($"EnemyRuntimeStats: Health tier changed to {healthTier}");
            }
        }

        /// <summary>
        /// 更新生命状态
        /// </summary>
        public void UpdateLifeState()
        {
            var previousState = lifeState;
            CoreHealthState coreState = crystalCore?.CoreHealthState ?? CoreHealthState.Destroyed;
            lifeState = EnemyLifeStateHelper.CalculateLifeState(currentHealth, coreState);

            if (previousState != lifeState)
            {
                OnLifeStateChanged?.Invoke(lifeState);
                Debug.Log($"EnemyRuntimeStats: Life state changed to {lifeState}");
            }
        }

        /// <summary>
        /// 受到生命伤害
        /// </summary>
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
        /// Health Restore (used when reviving)
        /// </summary>
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
            }

            return actualRestore;
        }

        /// <summary>
        /// 更新晶核紊乱值(每帧调用)
        /// </summary>
        public void UpdateChaos(float deltaTime)
        {
            if (crystalCore != null)
            {
                crystalCore.UpdateChaos(chaosRecoveryRate, deltaTime);
            }
        }

        /// <summary>
        /// 获取修正后的移动速度
        /// </summary>
        public float GetModifiedMoveSpeed()
        {
            if (!IsAlive) return 0f;
            
            float speedMultiplier = HealthTierHelper.GetSpeedMultiplier(healthTier);
            return moveSpeed * speedMultiplier;
        }
        
        /// <summary>
        /// 获取修正后的追击速度
        /// </summary>
        public float GetModifiedChaseMoveSpeed()
        {
            if (!IsAlive) return 0f;
            
            float speedMultiplier = HealthTierHelper.GetSpeedMultiplier(healthTier);
            return chaseMoveSpeed * speedMultiplier;
        }

        /// <summary>
        /// 完全恢复生命和晶核
        /// </summary>
        public void FullRestore()
        {
            currentHealth = maxHealth;
            crystalCore?.FullRepairCoreHealth();
            crystalCore?.ResetChaos();

            UpdateHealthTier();
            UpdateLifeState();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log("EnemyRuntimeStats: Full restore completed");
        }

        /// <summary>
        /// 获取保存数据
        /// </summary>
        public EnemyRuntimeStatsSaveData GetSaveData()
        {
            return new EnemyRuntimeStatsSaveData
            {
                currentHealth = this.currentHealth,
                crystalCoreSaveData = crystalCore?.GetSaveData()
            };
        }

        /// <summary>
        /// 从保存数据加载
        /// </summary>
        public void LoadFromSaveData(EnemyRuntimeStatsSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("EnemyRuntimeStats: Cannot load from null save data");
                return;
            }

            currentHealth = Mathf.Clamp(saveData.currentHealth, 0f, maxHealth);

            if (saveData.crystalCoreSaveData != null && crystalCore != null)
            {
                crystalCore.LoadFromSaveData(saveData.crystalCoreSaveData);
            }

            UpdateHealthTier();
            UpdateLifeState();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"EnemyRuntimeStats: Loaded from save data. Health: {currentHealth}/{maxHealth}");
        }

        /// <summary>
        /// Get hitbox multiplier configuration by hitbox type
        /// </summary>
        public HitboxMultiplierConfig GetHitboxMultiplierConfig(EnemyHitboxType hitboxType)
        {
            if (hitboxMultipliers == null || hitboxMultipliers.Count == 0)
            {
                Debug.LogWarning($"EnemyRuntimeStats: No hitbox multiplier configurations found, using defaults");
                return EnemyBaseStats.GetDefaultMultiplierConfig(hitboxType);
            }
            
            var config = hitboxMultipliers.FirstOrDefault(c => c.hitboxType == hitboxType);
            
            // If configuration not found, use default
            if (config.hitboxType == default)
            {
                Debug.LogWarning($"EnemyRuntimeStats: No configuration for {hitboxType}, using default");
                return EnemyBaseStats.GetDefaultMultiplierConfig(hitboxType);
            }
            
            return config;
        }

        /// <summary>
        /// 清理事件订阅
        /// </summary>
        public void Cleanup()
        {
            OnHealthChanged = null;
            OnHealthTierChanged = null;
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
        public CrystalCoreSaveData crystalCoreSaveData;

        public EnemyRuntimeStatsSaveData()
        {
            currentHealth = 100f;
            crystalCoreSaveData = null;
        }
    }
}