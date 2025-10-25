using UnityEngine;
using System.Collections.Generic;
using Resonance.Utilities;
using Resonance.Utilities.Types;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Player.Data
{
    /// <summary>
    /// 玩家运行时属性
    /// 游戏过程中可修改的实际属性值, 会受到装备、增益等影响
    /// </summary>
    [System.Serializable]
    public class PlayerRuntimeStats
    {
        [Header("Survival Attributes")]
        public float currentHealth;
        public float maxHealth;
        public float invulnerabilityTime;
        
        [Header("Crystal Core Attributes")]
        public CrystalCore crystalCore;
        public float healthRestoreValue;
        public float physicalDamageToCoreEnergyRatio;
        public float chaosRecoveryRate;

        [Header("Movement Attributes")]
        public float walkSpeed;
        public float runSpeed;
        public float aimMoveSpeed;
        public float reloadMoveSpeed;

        [Header("Equipment Attributes")]
        public int inventoryGridWidth;
        public int inventoryGridHeight;
        public int moduleSlots;

        [Header("Interaction Attributes")]
        public float interactionRange;
        public LayerMask interactionLayerMask;
        public LayerMask waveInteractionLayerMask;

        [Header("Visual Effects")]
        public string normalMaterialPath;
        public string damageMaterialPath;
        public float damageFlashDuration;

        [Header("Status Tiers")]
        public HealthTier healthTier;

        // Event system
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action<HealthTier> OnHealthTierChanged;

        // Property accessors
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsAlive => currentHealth > 0f;
        public bool IsDead => currentHealth <= 0f;
        public bool IsCoreDestroyed => crystalCore == null || crystalCore.CoreHealthState == CoreHealthState.Destroyed;
        public bool CanUseHealthRestore => crystalCore != null && crystalCore.CanConsumeSlot();

        public PlayerRuntimeStats(PlayerBaseStats baseStats)
        {
            // Copy survival attributes
            maxHealth = baseStats.MaxHealth;
            currentHealth = maxHealth; // Start with full health
            invulnerabilityTime = baseStats.InvulnerabilityTime;

            // Copy crystal core attributes
            // Player uses default QTE configuration
            crystalCore = new CrystalCore(baseStats.CrystalCoreConfig);
            healthRestoreValue = baseStats.HealthRestoreValue;
            physicalDamageToCoreEnergyRatio = baseStats.PhysicalDamageToCoreEnergyRatio;
            
            // Copy movement attributes
            walkSpeed = baseStats.WalkSpeed;
            runSpeed = baseStats.RunSpeed;
            aimMoveSpeed = baseStats.AimMoveSpeed;
            reloadMoveSpeed = baseStats.ReloadMoveSpeed;
            
            // Copy equipment attributes
            inventoryGridWidth = baseStats.InventoryGridWidth;
            inventoryGridHeight = baseStats.InventoryGridHeight;
            moduleSlots = baseStats.ModuleSlots;
            
            // Copy interaction attributes
            interactionRange = baseStats.InteractionRange;
            interactionLayerMask = baseStats.InteractionLayerMask;
            waveInteractionLayerMask = baseStats.WaveInteractionLayerMask;

            // Copy visual effects
            normalMaterialPath = baseStats.NormalMaterialPath;
            damageMaterialPath = baseStats.DamageMaterialPath;
            damageFlashDuration = baseStats.DamageFlashDuration;

            // Initialize status tiers
            UpdateHealthTier();
        }

        /// <summary>
        /// Update health tier
        /// </summary>
        public void UpdateHealthTier()
        {
            var previousTier = healthTier;
            healthTier = HealthTierHelper.CalculateHealthTier(HealthPercentage);
            chaosRecoveryRate = HealthTierHelper.GetChaosRecoveryRate(healthTier);

            if (previousTier != healthTier)
            {
                OnHealthTierChanged?.Invoke(healthTier);
                Debug.Log($"PlayerRuntimeStats: Health tier changed to {healthTier}");
            }
        }

        /// <summary>
        /// Take health damage
        /// </summary>
        public float TakeHealthDamage(float damage)
        {
            if (damage <= 0f || !IsAlive) return 0f;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            float actualDamage = previousHealth - currentHealth;

            if (actualDamage > 0f)
            {
                UpdateHealthTier();
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                Debug.Log($"PlayerRuntimeStats: Took {actualDamage} health damage. Current: {currentHealth}/{maxHealth}");
            }

            return actualDamage;
        }

        /// <summary>
        /// 恢复生命值
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
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                Debug.Log($"PlayerRuntimeStats: Restored {actualRestore} health. Current: {currentHealth}/{maxHealth}");
            }

            return actualRestore;
        }

        /// <summary>
        /// 使用晶核能量恢复生命值
        /// </summary>
        public bool UseHealthRestore()
        {
            if (crystalCore == null || !crystalCore.CanConsumeSlot()) return false;

            if (crystalCore.ConsumeEnergySlot())
            {
                RestoreHealth(healthRestoreValue);
                Debug.Log($"PlayerRuntimeStats: Used crystal core energy to restore {healthRestoreValue} health");
                return true;
            }

            return false;
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
        /// 完全恢复生命和晶核(存档点使用)
        /// </summary>
        public void FullRestore()
        {
            currentHealth = maxHealth;
            crystalCore?.FullRepairCoreHealth();
            crystalCore?.ResetChaos();

            UpdateHealthTier();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log("PlayerRuntimeStats: Full restore completed");
        }

        /// <summary>
        /// 获取保存数据
        /// </summary>
        public PlayerRuntimeStatsSaveData GetSaveData()
        {
            return new PlayerRuntimeStatsSaveData
            {
                currentHealth = this.currentHealth,
                crystalCoreSaveData = crystalCore?.GetSaveData(),
                inventoryGridWidth = this.inventoryGridWidth,
                inventoryGridHeight = this.inventoryGridHeight
            };
        }

        /// <summary>
        /// 从保存数据加载
        /// </summary>
        public void LoadFromSaveData(PlayerRuntimeStatsSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("PlayerRuntimeStats: Cannot load from null save data");
                return;
            }

            currentHealth = Mathf.Clamp(saveData.currentHealth, 0f, maxHealth);

            if (saveData.crystalCoreSaveData != null && crystalCore != null)
            {
                crystalCore.LoadFromSaveData(saveData.crystalCoreSaveData);
            }

            // 可升级属性
            if (saveData.inventoryGridWidth > 0) inventoryGridWidth = saveData.inventoryGridWidth;
            if (saveData.inventoryGridHeight > 0) inventoryGridHeight = saveData.inventoryGridHeight;

            UpdateHealthTier();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"PlayerRuntimeStats: Loaded from save data. Health: {currentHealth}/{maxHealth}");
        }

        /// <summary>
        /// 清理事件订阅
        /// </summary>
        public void Cleanup()
        {
            OnHealthChanged = null;
            OnHealthTierChanged = null;
            crystalCore?.Cleanup();
        }
    }

    /// <summary>
    /// 玩家运行时属性保存数据结构
    /// </summary>
    [System.Serializable]
    public class PlayerRuntimeStatsSaveData
    {
        public float currentHealth;
        public CrystalCoreSaveData crystalCoreSaveData;
        public int inventoryGridWidth;
        public int inventoryGridHeight;

        public PlayerRuntimeStatsSaveData()
        {
            currentHealth = 100f;
            crystalCoreSaveData = null;
            inventoryGridWidth = 5;
            inventoryGridHeight = 5;
        }
    }
}