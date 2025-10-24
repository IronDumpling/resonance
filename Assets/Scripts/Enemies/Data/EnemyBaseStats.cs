using UnityEngine;
using DG.Tweening;
using System.Linq;
using System.Collections.Generic;
using Resonance.Enemies.Triggers;
using Resonance.Utilities;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Enemies.Data
{
    /// <summary>
    /// Attack stats configuration
    /// Defines the damage, cooldown, and range of an attack
    /// </summary>
    [System.Serializable]
    public struct AttackStats
    {
        public Damages damages;
        public float cooldown;
        public float range;

        /// <summary>
        /// Create a deep copy of this AttackStats
        /// Clones the damages reference to avoid sharing between instances
        /// </summary>
        /// <returns>A new AttackStats with cloned damages</returns>
        public AttackStats Clone()
        {
            return new AttackStats
            {
                damages = this.damages?.Clone(),
                cooldown = this.cooldown,
                range = this.range
            };
        }
    }

    /// <summary>
    /// Hitbox multiplier configuration for a specific hitbox type
    /// Stores the damage multipliers that should be applied for each part of the enemy
    /// </summary>
    [System.Serializable]
    public struct HitboxMultiplierConfig
    {
        [Tooltip("Hitbox type")]
        public EnemyHitboxType hitboxType;
        
        [Tooltip("Multiplier for physical health damage")]
        public float physicalHealthMultiplier;
        
        [Tooltip("Multiplier for core health damage")]
        public float coreHealthMultiplier;
        
        [Tooltip("Multiplier for chaos damage")]
        public float chaosMultiplier;
    }

    /// <summary>
    /// Enemy attack type enumeration
    /// </summary>
    public enum AttackType
    {
        Normal,  // Normal physical attack
        Wave     // Wave attack targeting player's core health
    }
    
    /// <summary>
    /// Enemy base stats configuration
    /// 定义敌人的基准属性数据
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Stats", menuName = "Resonance/Enemies/Enemy Stats", order = 1)]
    public class EnemyBaseStats : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("敌人名称")]
        public string enemyName = "Basic Enemy";
        [TextArea(2, 4)]
        [Tooltip("敌人描述")]
        public string enemyDescription = "A basic enemy";
        
        [Header("Survival Attributes")]
        [Tooltip("最大生命值")]
        public float maxHealth = 100f;
        
        [Header("Crystal Core Attributes")]
        [Tooltip("晶核配置")]
        public CrystalCoreConfig crystalCoreConfig;
        [Tooltip("晶核波纹")]
        public string corePattern = "Enemy_Basic";
        
        [Header("Revival System")]
        [Tooltip("复活前等待时间")]
        public float revivalDelay = 2f;
        [Tooltip("复活时生命恢复速率")]
        public float revivalRate = 10f;
        
        [Header("Combat Attributes")]
        [Tooltip("普通攻击伤害")]
        public AttackStats normalAttackStats;
        [Tooltip("晶核攻击伤害")]
        public AttackStats waveAttackStats;
        [Tooltip("检测范围")]
        public float detectionRange = 4f;
        [Tooltip("普通攻击到能量的比例")]
        public float normalAttackToEnergyRatio = 0.5f;
        
        [Header("Vision System")]
        [Tooltip("视野扇形角度 (度)")]
        [Range(0f, 360f)]
        public float visionAngle = 120f;
        [Tooltip("最大视野距离")]
        public float visionDistance = 15f;
        [Tooltip("眼睛位置相对于敌人根节点的高度偏移")]
        public float eyeHeightOffset = 0f;
        [Tooltip("视野高度范围 (以眼睛位置为中心，上下对称)")]
        public float visionHeightRange = 1.5f;
        [Tooltip("失去视线后多久丢失目标 (秒)")]
        public float visionLossTimeout = 5f;
        [Tooltip("视野检测层 (用于射线检测)")]
        public LayerMask visionObstacleLayers = ~0; // Default to all layers
        
        [Header("Hitbox Damage Multipliers")]
        [Tooltip("Damage multipliers for each hitbox type")]
        public List<HitboxMultiplierConfig> hitboxMultipliers = new List<HitboxMultiplierConfig>();
        
        [Header("Navigation Configuration")]
        [Tooltip("普通移动速度")]
        public float moveSpeed = 1f;
        [Tooltip("追击移动速度")]
        public float chaseMoveSpeed = 2f;
        [Tooltip("巡逻半径")]
        public float patrolRadius = 5f;
        [Tooltip("到达目标的距离阈值")]
        public float arrivalThreshold = 0.5f;
        [Tooltip("基底偏移")]
        public float baseOffset = 1f;
        [Tooltip("加速度")]
        public float acceleration = 8f;
        [Tooltip("角速度 (度/秒)")]
        public float angularSpeed = 120f;
        [Tooltip("停止距离")]
        public float stoppingDistance = 0.5f;
        [Tooltip("是否自动刹车")]
        public bool autoBraking = true;
        
        [Header("Visual Effects")]
        [Tooltip("正常状态材质路径")]
        public string normalMaterialPath = "Art/Materials/Enemy/Enemy_Body";
        [Tooltip("受伤状态材质路径")]
        public string damageMaterialPath = "Art/Materials/Damage_Body";
        [Tooltip("复活状态材质路径")]
        public string revivalMaterialPath = "Art/Materials/Enemy/Enemy_Revival";
        [Tooltip("受伤闪烁持续时间")]
        public float damageFlashDuration = 0.2f;
        
        [Header("Audio Configuration")]
        [Tooltip("是否启用音频")]
        public bool enableAudio = true;
        
        [Header("Loot System")]
        [Tooltip("死亡时生成的掉落物预制体")]
        public GameObject deathLootPrefab;
        [Tooltip("掉落物数量")]
        [Range(1, 5)]
        public int lootCount = 1;
        [Tooltip("掉落物生成半径")]
        public float lootSpawnRadius = 0.5f;
        [Tooltip("掉落概率")]
        [Range(0f, 1f)]
        public float lootDropChance = 1f;

        [Header("Debug Options")]
        [Tooltip("显示生命条")]
        public bool showHealthBar = true;
        [Tooltip("显示检测范围")]
        public bool showDetectionRange = false;
        [Tooltip("显示攻击范围")]
        public bool showAttackRange = false;

        /// <summary>
        /// 创建运行时属性实例
        /// </summary>
        public EnemyRuntimeStats CreateRuntimeStats()
        {
            return new EnemyRuntimeStats(this);
        }

        /// <summary>
        /// 验证配置数据
        /// </summary>
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

            float physicalDamage = normalAttackStats.damages.GetDamage(DamageType.PhysicalHealth);
            if (physicalDamage <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid normal damage: {physicalDamage}");
                return false;
            }

            float coreDamage = waveAttackStats.damages.GetDamage(DamageType.CoreHealth);
            if (coreDamage <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid core damage: {coreDamage}");
                return false;
            }

            if (moveSpeed <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid move speed: {moveSpeed}");
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Get hitbox multiplier configuration by hitbox type
        /// </summary>
        public HitboxMultiplierConfig GetHitboxMultiplierConfig(EnemyHitboxType hitboxType)
        {
            if (hitboxMultipliers == null || hitboxMultipliers.Count == 0)
            {
                Debug.LogWarning($"EnemyBaseStats: {enemyName} has no hitbox multiplier configurations");
                return GetDefaultMultiplierConfig(hitboxType);
            }
            
            var config = hitboxMultipliers.FirstOrDefault(c => c.hitboxType == hitboxType);
            return config;
        }
        
        /// <summary>
        /// Get default hitbox multiplier configuration for a specific type
        /// Used when no custom configuration is found
        /// </summary>
        public static HitboxMultiplierConfig GetDefaultMultiplierConfig(EnemyHitboxType hitboxType)
        {
            switch (hitboxType)
            {
                case EnemyHitboxType.Head:
                    return new HitboxMultiplierConfig
                    {
                        hitboxType = EnemyHitboxType.Head,
                        physicalHealthMultiplier = 1.5f,
                        coreHealthMultiplier = 0f,
                        chaosMultiplier = 1.5f
                    };
                    
                case EnemyHitboxType.Core:
                    return new HitboxMultiplierConfig
                    {
                        hitboxType = EnemyHitboxType.Core,
                        physicalHealthMultiplier = 0f,
                        coreHealthMultiplier = 1f,
                        chaosMultiplier = 0f
                    };
                    
                case EnemyHitboxType.Knee:
                    return new HitboxMultiplierConfig
                    {
                        hitboxType = EnemyHitboxType.Knee,
                        physicalHealthMultiplier = 0.5f,
                        coreHealthMultiplier = 0f,
                        chaosMultiplier = 2f
                    };
                    
                case EnemyHitboxType.Body:
                default:
                    return new HitboxMultiplierConfig
                    {
                        hitboxType = EnemyHitboxType.Body,
                        physicalHealthMultiplier = 1f,
                        coreHealthMultiplier = 0f,
                        chaosMultiplier = 0.5f
                    };
            }
        }
        
        /// <summary>
        /// Initialize default hitbox multiplier configurations if empty
        /// </summary>
        public void InitializeDefaultHitboxMultipliers()
        {
            if (hitboxMultipliers == null)
            {
                hitboxMultipliers = new List<HitboxMultiplierConfig>();
            }
            
            // Only initialize if empty
            if (hitboxMultipliers.Count > 0)
                return;
            
            // Add default configurations for all hitbox types
            hitboxMultipliers.Add(GetDefaultMultiplierConfig(EnemyHitboxType.Head));
            hitboxMultipliers.Add(GetDefaultMultiplierConfig(EnemyHitboxType.Body));
            hitboxMultipliers.Add(GetDefaultMultiplierConfig(EnemyHitboxType.Knee));
            hitboxMultipliers.Add(GetDefaultMultiplierConfig(EnemyHitboxType.Core));
            
            Debug.Log($"EnemyBaseStats: {enemyName} initialized default hitbox multiplier configurations");
        }

        #region Unity Editor

        void OnValidate()
        {
            // 确保数值在合理范围内
            maxHealth = Mathf.Max(1f, maxHealth);
            
            // 复活系统验证
            revivalDelay = Mathf.Max(0f, revivalDelay);
            revivalRate = Mathf.Max(0.1f, revivalRate);
            
            // Initialize default hitbox multipliers if not set
            InitializeDefaultHitboxMultipliers();
            
            // 战斗属性验证
            float physicalDamage = Mathf.Max(0.1f, normalAttackStats.damages.GetDamage(DamageType.PhysicalHealth));
            float chaosDamage = Mathf.Max(0.1f, normalAttackStats.damages.GetDamage(DamageType.Chaos));
            normalAttackStats.damages.SetDamage(DamageType.PhysicalHealth, physicalDamage);
            normalAttackStats.damages.SetDamage(DamageType.Chaos, chaosDamage);
            normalAttackStats.cooldown = Mathf.Max(0.1f, normalAttackStats.cooldown);
            normalAttackStats.range = Mathf.Max(0.1f, normalAttackStats.range);

            float coreDamage = Mathf.Max(0.1f, waveAttackStats.damages.GetDamage(DamageType.CoreHealth));
            waveAttackStats.damages.SetDamage(DamageType.CoreHealth, coreDamage);
            waveAttackStats.cooldown = Mathf.Max(0.1f, waveAttackStats.cooldown);
            waveAttackStats.range = Mathf.Max(0.1f, waveAttackStats.range);

            detectionRange = Mathf.Max(0.1f, detectionRange);
            normalAttackToEnergyRatio = Mathf.Clamp01(normalAttackToEnergyRatio);
            
            // 视野系统验证
            visionAngle = Mathf.Clamp(visionAngle, 0f, 360f);
            visionDistance = Mathf.Max(0.1f, visionDistance);
            visionHeightRange = Mathf.Max(0.1f, visionHeightRange);
            visionLossTimeout = Mathf.Max(0.1f, visionLossTimeout);
            
            // 移动属性验证
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            chaseMoveSpeed = Mathf.Max(0.1f, chaseMoveSpeed);
            patrolRadius = Mathf.Max(0f, patrolRadius);
            arrivalThreshold = Mathf.Max(0.1f, arrivalThreshold);
            
            // NavMesh Agent 配置验证
            acceleration = Mathf.Max(0.1f, acceleration);
            angularSpeed = Mathf.Max(0f, angularSpeed);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
            
            // 视觉效果验证
            damageFlashDuration = Mathf.Max(0.1f, damageFlashDuration);
            
            
            // 掉落系统验证
            lootCount = Mathf.Clamp(lootCount, 1, 5);
            lootSpawnRadius = Mathf.Max(0.5f, lootSpawnRadius);
            lootDropChance = Mathf.Clamp01(lootDropChance);
        }

        #endregion
    }
}
