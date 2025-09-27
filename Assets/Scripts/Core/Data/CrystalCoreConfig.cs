using UnityEngine;

namespace Resonance.Core.Data
{
    /// <summary>
    /// 晶核配置数据
    /// 用于配置不同类型的晶核（玩家/敌人）
    /// </summary>
    [CreateAssetMenu(fileName = "New Crystal Core Config", menuName = "Resonance/Core/Crystal Core Config")]
    public class CrystalCoreConfig : ScriptableObject
    {
        [Header("Pattern Configuration")]
        [Tooltip("默认晶核波纹")]
        public string defaultPattern = "";
        
        [Header("Energy Configuration")]
        [Tooltip("最大能量容量")]
        public float maxEnergyCapacity = 60f;
        
        [Tooltip("每个能量槽的能量值")]
        public float energyPerSlot = 20f;
        
        [Tooltip("是否以满能量开始（敌人用）")]
        public bool startWithFullEnergy = false;
        
        [Header("Upgrade Configuration")]
        [Tooltip("是否可以升级最大容量")]
        public bool canUpgradeCapacity = true;
        
        [Tooltip("容量升级的步长")]
        public float capacityUpgradeStep = 20f;
        
        [Tooltip("最大可升级到的容量")]
        public float maxUpgradeableCapacity = 200f;
        
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
        
        [Tooltip("容量损坏音效")]
        public AudioClip capacityDamageSound;
        
        [Tooltip("容量修复音效")]
        public AudioClip capacityRepairSound;

        /// <summary>
        /// 验证配置数据
        /// </summary>
        /// <returns>是否有效</returns>
        public bool ValidateConfig()
        {
            if (maxEnergyCapacity <= 0f)
            {
                Debug.LogError($"CrystalCoreConfig: {name} has invalid maxEnergyCapacity: {maxEnergyCapacity}");
                return false;
            }

            if (energyPerSlot <= 0f)
            {
                Debug.LogError($"CrystalCoreConfig: {name} has invalid energyPerSlot: {energyPerSlot}");
                return false;
            }

            if (energyPerSlot > maxEnergyCapacity)
            {
                Debug.LogError($"CrystalCoreConfig: {name} energyPerSlot ({energyPerSlot}) cannot be greater than maxEnergyCapacity ({maxEnergyCapacity})");
                return false;
            }

            if (canUpgradeCapacity)
            {
                if (capacityUpgradeStep <= 0f)
                {
                    Debug.LogError($"CrystalCoreConfig: {name} has invalid capacityUpgradeStep: {capacityUpgradeStep}");
                    return false;
                }

                if (maxUpgradeableCapacity < maxEnergyCapacity)
                {
                    Debug.LogError($"CrystalCoreConfig: {name} maxUpgradeableCapacity ({maxUpgradeableCapacity}) cannot be less than maxEnergyCapacity ({maxEnergyCapacity})");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取指定容量下的槽位数量
        /// </summary>
        /// <param name="capacity">容量值</param>
        /// <returns>槽位数量</returns>
        public int GetSlotsForCapacity(float capacity)
        {
            return energyPerSlot > 0 ? Mathf.FloorToInt(capacity / energyPerSlot) : 0;
        }

        /// <summary>
        /// 获取指定槽位数的容量
        /// </summary>
        /// <param name="slots">槽位数量</param>
        /// <returns>容量值</returns>
        public float GetCapacityForSlots(int slots)
        {
            return slots * energyPerSlot;
        }

        /// <summary>
        /// 计算下一个升级等级的容量
        /// </summary>
        /// <param name="currentCapacity">当前容量</param>
        /// <returns>下一等级容量，如果无法升级返回-1</returns>
        public float GetNextUpgradeCapacity(float currentCapacity)
        {
            if (!canUpgradeCapacity) return -1f;

            float nextCapacity = currentCapacity + capacityUpgradeStep;
            if (nextCapacity > maxUpgradeableCapacity) return -1f;

            return nextCapacity;
        }

        /// <summary>
        /// 检查是否可以升级容量
        /// </summary>
        /// <param name="currentCapacity">当前容量</param>
        /// <returns>是否可以升级</returns>
        public bool CanUpgradeCapacity(float currentCapacity)
        {
            return GetNextUpgradeCapacity(currentCapacity) > 0f;
        }

        /// <summary>
        /// 根据能量等级获取颜色
        /// </summary>
        /// <param name="tier">能量等级</param>
        /// <returns>对应颜色</returns>
        public Color GetColorForTier(CrystalEnergyTier tier)
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

        #region Unity Editor

        void OnValidate()
        {
            // 确保数值在合理范围内
            maxEnergyCapacity = Mathf.Max(1f, maxEnergyCapacity);
            energyPerSlot = Mathf.Max(1f, energyPerSlot);
            
            if (canUpgradeCapacity)
            {
                capacityUpgradeStep = Mathf.Max(1f, capacityUpgradeStep);
                maxUpgradeableCapacity = Mathf.Max(maxEnergyCapacity, maxUpgradeableCapacity);
            }

            // 确保槽位配置合理
            if (energyPerSlot > maxEnergyCapacity)
            {
                energyPerSlot = maxEnergyCapacity;
                Debug.LogWarning($"CrystalCoreConfig: {name} energyPerSlot adjusted to match maxEnergyCapacity");
            }
        }

        #endregion
    }
}
