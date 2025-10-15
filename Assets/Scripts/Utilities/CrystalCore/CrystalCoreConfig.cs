using UnityEngine;
using Resonance.Utilities;
using Resonance.Utilities.Wave;

namespace Resonance.Utilities.CrystalCore
{
    /// <summary>
    /// 晶核配置数据
    /// 用于配置不同类型的晶核(玩家/敌人)
    /// </summary>
    [CreateAssetMenu(fileName = "New Crystal Core Config", menuName = "Resonance/Core/Crystal Core Config")]
    public class CrystalCoreConfig : ScriptableObject
    {
        [Header("Core Health Configuration")]
        [Tooltip("初始最大晶核生命值(格数 * 每格能量值, 例如3格 * 30 = 90)")]
        public float initialMaxCoreHealth = 90f;
        
        [Tooltip("每个能量槽的能量值")]
        public float energyPerSlot = 30f;
        
        [Tooltip("是否以满能量开始(敌人用, 玩家从0开始)")]
        public bool startWithFullEnergy = false;
        
        [Header("Core Wave Configuration")]
        [Tooltip("Wave配置")]
        public WaveConfig waveConfig;
        
        [Header("Upgrade Configuration")]
        [Tooltip("是否可以升级最大生命值")]
        public bool canUpgradeMaxHealth = true;
        
        [Tooltip("生命值升级的步长(每格30点)")]
        public float healthUpgradeStep = 30f;
        
        [Tooltip("最大可升级到的生命值")]
        public float maxUpgradeableHealth = 300f;
        
        [Header("Visual Configuration")]
        [Tooltip("晶核材质路径")]
        public string coreMaterialPath = "Art/Materials/CrystalCore";
        
        [Tooltip("能量充盈时的颜色")]
        public Color abundantColor = Color.cyan;
        
        [Tooltip("能量正常时的颜色")]
        public Color normalColor = Color.blue;
        
        [Tooltip("能量低下时的颜色")]
        public Color lowColor = Color.red;
        
        
        [Header("Audio Configuration")]
        [Tooltip("能量消耗音效")]
        public AudioClip energyConsumeSound;
        
        [Tooltip("能量获得音效")]
        public AudioClip energyGainSound;
        
        [Tooltip("晶核生命损坏音效")]
        public AudioClip coreHealthDamageSound;
        
        [Tooltip("晶核生命修复音效")]
        public AudioClip coreHealthRepairSound;
        

        /// <summary>
        /// 验证配置数据
        /// </summary>
        public bool ValidateConfig()
        {
            if (initialMaxCoreHealth <= 0f)
            {
                Debug.LogError($"CrystalCoreConfig: {name} has invalid initialMaxCoreHealth: {initialMaxCoreHealth}");
                return false;
            }

            if (energyPerSlot <= 0f)
            {
                Debug.LogError($"CrystalCoreConfig: {name} has invalid energyPerSlot: {energyPerSlot}");
                return false;
            }

            if (energyPerSlot > initialMaxCoreHealth)
            {
                Debug.LogError($"CrystalCoreConfig: {name} energyPerSlot ({energyPerSlot}) cannot be greater than initialMaxCoreHealth ({initialMaxCoreHealth})");
                return false;
            }
            
            if (waveConfig != null && !waveConfig.ValidateConfig())
            {
                Debug.LogError($"CrystalCoreConfig: {name} has invalid wave config");
                return false;
            }

            if (canUpgradeMaxHealth)
            {
                if (healthUpgradeStep <= 0f)
                {
                    Debug.LogError($"CrystalCoreConfig: {name} has invalid healthUpgradeStep: {healthUpgradeStep}");
                    return false;
                }

                if (maxUpgradeableHealth < initialMaxCoreHealth)
                {
                    Debug.LogError($"CrystalCoreConfig: {name} maxUpgradeableHealth ({maxUpgradeableHealth}) cannot be less than initialMaxCoreHealth ({initialMaxCoreHealth})");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取指定生命值下的槽位数量
        /// </summary>
        public int GetSlotsForHealth(float health)
        {
            return energyPerSlot > 0 ? Mathf.FloorToInt(health / energyPerSlot) : 0;
        }

        /// <summary>
        /// 获取指定槽位数的生命值
        /// </summary>
        public float GetHealthForSlots(int slots)
        {
            return slots * energyPerSlot;
        }

        /// <summary>
        /// 计算下一个升级等级的生命值
        /// </summary>
        public float GetNextUpgradeHealth(float currentHealth)
        {
            if (!canUpgradeMaxHealth) return -1f;

            float nextHealth = currentHealth + healthUpgradeStep;
            if (nextHealth > maxUpgradeableHealth) return -1f;

            return nextHealth;
        }

        /// <summary>
        /// 检查是否可以升级生命值
        /// </summary>
        public bool CanUpgradeHealth(float currentHealth)
        {
            return GetNextUpgradeHealth(currentHealth) > 0f;
        }

        /// <summary>
        /// 根据能量等级获取颜色
        /// </summary>
        public Color GetColorForEnergyTier(CrystalEnergyTier tier)
        {
            switch (tier)
            {
                case CrystalEnergyTier.Abundant:
                    return abundantColor;
                case CrystalEnergyTier.Normal:
                    return normalColor;
                case CrystalEnergyTier.Low:
                    return lowColor;
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// 根据波纹状态获取颜色
        /// </summary>
        public Color GetColorForChaosState(WaveChaosState state)
        {
            if (waveConfig == null) return Color.white;
            
            switch (state)
            {
                case WaveChaosState.Order:
                    return waveConfig.orderColor;
                case WaveChaosState.Chaos:
                    return waveConfig.chaosColor;
                default:
                    return Color.white;
            }
        }

        #region Unity Editor

        void OnValidate()
        {
            // 确保数值在合理范围内
            initialMaxCoreHealth = Mathf.Max(energyPerSlot, initialMaxCoreHealth);
            energyPerSlot = Mathf.Max(1f, energyPerSlot);
            
            if (canUpgradeMaxHealth)
            {
                healthUpgradeStep = Mathf.Max(energyPerSlot, healthUpgradeStep);
                maxUpgradeableHealth = Mathf.Max(initialMaxCoreHealth, maxUpgradeableHealth);
            }

            // 确保槽位配置合理
            if (energyPerSlot > initialMaxCoreHealth)
            {
                energyPerSlot = initialMaxCoreHealth;
                Debug.LogWarning($"CrystalCoreConfig: {name} energyPerSlot adjusted to match initialMaxCoreHealth");
            }
        }

        #endregion
    }
}
