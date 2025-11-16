using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Gameplay.Enemies;
using Resonance.Gameplay.Enemies.Triggers;
using Resonance.Utilities.Types;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Gameplay.Enemies.Data
{
    /// <summary>
    /// Enemy runtime stats
    /// Gameplay-specific attributes that can be modified during runtime
    /// </summary>
    [System.Serializable]
    public class EnemyRuntimeStats
    {
        [Header("Balance System (Stance/Posture)")]
        public float currentBalance;
        public float maxBalance;
        public float balanceRecoveryRate;
        public float balanceRecoveryRateInCoreExposed;
        public float unbalancedDuration;
        public float staggerDurationPerDamage;
        
        [Header("Crystal Core Attributes")]
        public CrystalCore crystalCore;
        public string corePattern;
        
        [Header("Combat Attributes")]
        public AttackStats normalAttackStats;
        public AttackStats waveAttackStats;
        public float detectionRange;
        public float normalAttackToEnergyRatio;
        
        [Header("Vision System")]
        public float visionAngle;
        public float visionDistance;
        public float eyeHeightOffset;
        public float visionHeightRange;
        public float visionLossTimeout;
        public LayerMask visionObstacleLayers;
        
        [Header("Hitbox Damage Multipliers")]
        public List<HitboxMultiplierConfig> hitboxMultipliers;
        
        [Header("Navigation Configuration")]
        public float moveSpeed;
        public float chaseMoveSpeed;
        public float patrolRadius;
        public float arrivalThreshold;
        public float baseOffset;
        public float acceleration;
        public float angularSpeed;
        public float stoppingDistance;
        public bool autoBraking;
        
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
        public bool showBalanceBar;
        public bool showDetectionRange;
        public bool showAttackRange;

        [Header("Status Tiers")]
        public BalanceTier balanceTier;

        // Event system
        public System.Action<float, float> OnBalanceChanged; // current, max
        public System.Action<BalanceTier> OnBalanceTierChanged;
        
        // Attribute accessors
        public float BalancePercentage => maxBalance > 0 ? currentBalance / maxBalance : 0f;
        public bool IsCoreIntact => crystalCore != null && crystalCore.CoreHealthState == CoreHealthState.Intact;

        public EnemyRuntimeStats(EnemyBaseStats baseStats)
        {
            // Copy balance system attributes
            maxBalance = baseStats.maxBalance;
            currentBalance = maxBalance;
            balanceRecoveryRate = baseStats.balanceRecoveryRate;
            balanceRecoveryRateInCoreExposed = baseStats.balanceRecoveryRateInCoreExposed;
            unbalancedDuration = baseStats.unbalancedDuration;
            staggerDurationPerDamage = baseStats.staggerDurationPerDamage;
            
            // Copy crystal core attributes
            crystalCore = new CrystalCore(baseStats.crystalCoreConfig);
            crystalCore.SetFullEnergy(); // Enemy has full energy
            corePattern = baseStats.corePattern;
            
            // Combat attributes - Clone to avoid sharing damages reference with other instances
            normalAttackStats = baseStats.normalAttackStats.Clone();
            waveAttackStats = baseStats.waveAttackStats.Clone();
            detectionRange = baseStats.detectionRange;
            normalAttackToEnergyRatio = baseStats.normalAttackToEnergyRatio;
            
            // Vision system
            visionAngle = baseStats.visionAngle;
            visionDistance = baseStats.visionDistance;
            eyeHeightOffset = baseStats.eyeHeightOffset;
            visionHeightRange = baseStats.visionHeightRange;
            visionLossTimeout = baseStats.visionLossTimeout;
            visionObstacleLayers = baseStats.visionObstacleLayers;
            
            // Hitbox multipliers - Copy the list for independent configuration
            hitboxMultipliers = new List<HitboxMultiplierConfig>(baseStats.hitboxMultipliers ?? new List<HitboxMultiplierConfig>());
            
            // Movement attributes
            moveSpeed = baseStats.moveSpeed;
            chaseMoveSpeed = baseStats.chaseMoveSpeed;
            patrolRadius = baseStats.patrolRadius;
            arrivalThreshold = baseStats.arrivalThreshold;
            
            // NavMesh Agent configuration
            baseOffset = baseStats.baseOffset;
            acceleration = baseStats.acceleration;
            angularSpeed = baseStats.angularSpeed;
            stoppingDistance = baseStats.stoppingDistance;
            autoBraking = baseStats.autoBraking;
            
            // Visual effects
            normalMaterialPath = baseStats.normalMaterialPath;
            damageMaterialPath = baseStats.damageMaterialPath;
            revivalMaterialPath = baseStats.revivalMaterialPath;
            damageFlashDuration = baseStats.damageFlashDuration;
            
            // Audio configuration
            enableAudio = baseStats.enableAudio;
            
            // Loot system
            deathLootPrefab = baseStats.deathLootPrefab;
            lootCount = baseStats.lootCount;
            lootSpawnRadius = baseStats.lootSpawnRadius;
            lootDropChance = baseStats.lootDropChance;
            
            // Debug options
            showBalanceBar = baseStats.showBalanceBar;
            showDetectionRange = baseStats.showDetectionRange;
            showAttackRange = baseStats.showAttackRange;

            // Initialize balance tier
            UpdateBalanceTier();
        }

        /// <summary>
        /// Update balance tier
        /// </summary>
        public void UpdateBalanceTier()
        {
            var previousTier = balanceTier;
            balanceTier = BalanceTierHelper.CalculateBalanceTier(BalancePercentage);

            if (previousTier != balanceTier)
            {
                OnBalanceTierChanged?.Invoke(balanceTier);
                Debug.Log($"EnemyRuntimeStats: Balance tier changed to {balanceTier}");
            }
        }

        /// <summary>
        /// Take balance damage
        /// </summary>
        public float TakeBalanceDamage(float damage)
        {
            if (damage <= 0f) return 0f;

            float previousBalance = currentBalance;
            currentBalance = Mathf.Max(0f, currentBalance - damage);
            float actualDamage = previousBalance - currentBalance;

            if (actualDamage > 0f)
            {
                UpdateBalanceTier();
                OnBalanceChanged?.Invoke(currentBalance, maxBalance);
                Debug.Log($"EnemyRuntimeStats: Took {actualDamage} balance damage. Current: {currentBalance}/{maxBalance}");
            }

            return actualDamage;
        }

        /// <summary>
        /// Restore Balance (used when recovering in CoreExposed state or Unbalanced timeout)
        /// </summary>
        public float RestoreBalance(float amount)
        {
            if (amount <= 0f) return 0f;

            float previousBalance = currentBalance;
            currentBalance = Mathf.Min(currentBalance + amount, maxBalance);
            float actualRestore = currentBalance - previousBalance;

            if (actualRestore > 0f)
            {
                UpdateBalanceTier();
                OnBalanceChanged?.Invoke(currentBalance, maxBalance);
            }

            return actualRestore;
        }

        /// <summary>
        /// Get modified move speed
        /// </summary>
        public float GetModifiedMoveSpeed()
        {
            if (currentBalance <= 0f) return 0f;
            
            float speedMultiplier = BalanceTierHelper.GetSpeedMultiplier(balanceTier);
            return moveSpeed * speedMultiplier;
        }
        
        /// <summary>
        /// Get modified chase move speed
        /// </summary>
        public float GetModifiedChaseMoveSpeed()
        {
            if (currentBalance <= 0f) return 0f;
            
            float speedMultiplier = BalanceTierHelper.GetSpeedMultiplier(balanceTier);
            return chaseMoveSpeed * speedMultiplier;
        }

        /// <summary>
        /// Full restore balance and crystal core
        /// </summary>
        public void FullRestore()
        {
            currentBalance = maxBalance;
            crystalCore?.FullRestoreCoreHealth();

            UpdateBalanceTier();
            OnBalanceChanged?.Invoke(currentBalance, maxBalance);

            Debug.Log("EnemyRuntimeStats: Full restore completed");
        }

        /// <summary>
        /// Get save data
        /// </summary>
        public EnemyRuntimeStatsSaveData GetSaveData()
        {
            return new EnemyRuntimeStatsSaveData
            {
                currentBalance = this.currentBalance,
                crystalCoreSaveData = crystalCore?.GetSaveData()
            };
        }

        /// <summary>
        /// Load from save data
        /// </summary>
        public void LoadFromSaveData(EnemyRuntimeStatsSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("EnemyRuntimeStats: Cannot load from null save data");
                return;
            }

            currentBalance = Mathf.Clamp(saveData.currentBalance, 0f, maxBalance);

            if (saveData.crystalCoreSaveData != null && crystalCore != null)
            {
                crystalCore.LoadFromSaveData(saveData.crystalCoreSaveData);
            }

            UpdateBalanceTier();
            OnBalanceChanged?.Invoke(currentBalance, maxBalance);

            Debug.Log($"EnemyRuntimeStats: Loaded from save data. Balance: {currentBalance}/{maxBalance}");
        }

        /// <summary>
        /// Get hitbox multiplier configuration by hitbox type
        /// </summary>
        public HitboxMultiplierConfig GetHitboxMultiplierConfig(HitboxType hitboxType)
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
        /// Cleanup event subscriptions
        /// </summary>
        public void Cleanup()
        {
            OnBalanceChanged = null;
            OnBalanceTierChanged = null;
            crystalCore?.Cleanup();
        }
    }

    /// <summary>
    /// Enemy runtime stats save data structure
    /// </summary>
    [System.Serializable]
    public class EnemyRuntimeStatsSaveData
    {
        public float currentBalance;
        public CrystalCoreSaveData crystalCoreSaveData;

        public EnemyRuntimeStatsSaveData()
        {
            currentBalance = 100f;
            crystalCoreSaveData = null;
        }
    }
}