using UnityEngine;
using DG.Tweening;
using System.Linq;
using System.Collections.Generic;
using Resonance.Enemies.Triggers;
using Resonance.Utilities.Types;
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
    /// Defines the base attributes of an enemy
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Stats", menuName = "Resonance/Enemies/Enemy Stats", order = 1)]
    public class EnemyBaseStats : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Enemy name")]
        public string enemyName = "Basic Enemy";
        [TextArea(2, 4)]
        [Tooltip("Enemy description")]
        public string enemyDescription = "A basic enemy";
        
        [Header("Survival Attributes")]
        [Tooltip("Maximum health")]
        public float maxHealth = 100f;
        
        [Header("Crystal Core Attributes")]
        [Tooltip("Crystal core configuration")]
        public CrystalCoreConfig crystalCoreConfig;
        [Tooltip("Crystal core pattern")]
        public string corePattern = "Enemy_Basic";
        
        [Header("Revival System")]
        [Tooltip("Revival delay")]
        public float revivalDelay = 2f;
        [Tooltip("Revival rate")]
        public float revivalRate = 10f;
        
        [Header("Combat Attributes")]
        [Tooltip("Normal attack damage")]
        public AttackStats normalAttackStats;
        [Tooltip("Wave attack damage")]
        public AttackStats waveAttackStats;
        [Tooltip("Detection range")]
        public float detectionRange = 4f;
        [Tooltip("Normal attack to energy ratio")]
        public float normalAttackToEnergyRatio = 0.5f;
        
        [Header("Vision System")]
        [Tooltip("Vision angle (degrees)")]
        [Range(0f, 360f)]
        public float visionAngle = 120f;
        [Tooltip("Maximum vision distance")]
        public float visionDistance = 15f;
        [Tooltip("Eye height offset relative to enemy root node")]
        public float eyeHeightOffset = 0f;
        [Tooltip("Vision height range (centered around eye position)")]
        public float visionHeightRange = 1.5f;
        [Tooltip("Time to lose target after losing vision (seconds)")]
        public float visionLossTimeout = 5f;
        [Tooltip("Vision detection layers (for raycast)")]
        public LayerMask visionObstacleLayers = ~0; // Default to all layers
        
        [Header("Hitbox Damage Multipliers")]
        [Tooltip("Damage multipliers for each hitbox type")]
        public List<HitboxMultiplierConfig> hitboxMultipliers = new List<HitboxMultiplierConfig>();
        
        [Header("Navigation Configuration")]
        [Tooltip("Normal move speed")]
        public float moveSpeed = 1f;
        [Tooltip("Chase move speed")]
        public float chaseMoveSpeed = 2f;
        [Tooltip("Patrol radius")]
        public float patrolRadius = 5f;
        [Tooltip("Arrival threshold")]
        public float arrivalThreshold = 0.5f;
        [Tooltip("Base offset")]
        public float baseOffset = 1f;
        [Tooltip("Acceleration")]
        public float acceleration = 8f;
        [Tooltip("Angular speed (degrees/second)")]
        public float angularSpeed = 120f;
        [Tooltip("Stopping distance")]
        public float stoppingDistance = 0.5f;
        [Tooltip("Auto braking")]
        public bool autoBraking = true;
        
        [Header("Visual Effects")]
        [Tooltip("Normal state material path")]
        public string normalMaterialPath = "Art/Materials/Enemy/Enemy_Body";
        [Tooltip("Damage state material path")]
        public string damageMaterialPath = "Art/Materials/Damage_Body";
        [Tooltip("Revival state material path")]
        public string revivalMaterialPath = "Art/Materials/Enemy/Enemy_Revival";
        [Tooltip("Damage flash duration")]
        public float damageFlashDuration = 0.2f;
        
        [Header("Loot System")]
        [Tooltip("Death loot prefab")]
        public GameObject deathLootPrefab;
        [Tooltip("Loot count")]
        [Range(1, 5)]
        public int lootCount = 1;
        [Tooltip("Loot spawn radius")]
        public float lootSpawnRadius = 0.5f;
        [Tooltip("Loot drop chance")]
        [Range(0f, 1f)]
        public float lootDropChance = 1f;

        [Header("Debug Options")]
        [Tooltip("Show health bar")]
        public bool showHealthBar = true;
        [Tooltip("Show detection range")]
        public bool showDetectionRange = false;
        [Tooltip("Show attack range")]
        public bool showAttackRange = false;

        [Header("Audio Configuration")]
        [Tooltip("Enable audio")]
        public bool enableAudio = true;

        /// <summary>
        /// Create runtime stats instance
        /// </summary>
        public EnemyRuntimeStats CreateRuntimeStats()
        {
            return new EnemyRuntimeStats(this);
        }

        /// <summary>
        /// Validate configuration data
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
            // Ensure values are within reasonable ranges
            maxHealth = Mathf.Max(1f, maxHealth);
            
            // Validate revival system
            revivalDelay = Mathf.Max(0f, revivalDelay);
            revivalRate = Mathf.Max(0.1f, revivalRate);
            
            // Initialize default hitbox multipliers if not set
            InitializeDefaultHitboxMultipliers();
            
            // Validate combat attributes
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
            
            // Validate vision system
            visionAngle = Mathf.Clamp(visionAngle, 0f, 360f);
            visionDistance = Mathf.Max(0.1f, visionDistance);
            visionHeightRange = Mathf.Max(0.1f, visionHeightRange);
            visionLossTimeout = Mathf.Max(0.1f, visionLossTimeout);
            
            // Validate movement attributes
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            chaseMoveSpeed = Mathf.Max(0.1f, chaseMoveSpeed);
            patrolRadius = Mathf.Max(0f, patrolRadius);
            arrivalThreshold = Mathf.Max(0.1f, arrivalThreshold);
            
            // Validate NavMesh Agent configuration
            acceleration = Mathf.Max(0.1f, acceleration);
            angularSpeed = Mathf.Max(0f, angularSpeed);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
            
            // Validate visual effects
            damageFlashDuration = Mathf.Max(0.1f, damageFlashDuration);
            
            
            // Validate loot system
            lootCount = Mathf.Clamp(lootCount, 1, 5);
            lootSpawnRadius = Mathf.Max(0.5f, lootSpawnRadius);
            lootDropChance = Mathf.Clamp01(lootDropChance);
        }

        #endregion
    }
}
