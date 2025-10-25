using UnityEngine;
using Resonance.Utilities.Types;
using Resonance.Utilities.Waves;

namespace Resonance.Utilities.CrystalCore
{
    /// <summary>
    /// Crystal Core Configuration Data
    /// Used to configure different types of crystal cores (Player/Enemy)
    /// </summary>
    [CreateAssetMenu(fileName = "New Crystal Core Config", menuName = "Resonance/Core/Crystal Core Config")]
    public class CrystalCoreConfig : ScriptableObject
    {
        [Header("Core Health Configuration")]
        [Tooltip("Initial Maximum Core Health (Slots * Energy Per Slot, e.g. 3 slots * 30 energy = 90)")]
        public float initialMaxCoreHealth = 90f;
        
        [Tooltip("Energy Per Slot")]
        public float energyPerSlot = 30f;
        
        [Tooltip("Start with Full Energy (Enemy uses, Player starts at 0)")]
        public bool startWithFullEnergy = false;
        
        [Header("Core Wave Configuration")]
        [Tooltip("Wave Configuration")]
        public WaveConfig waveConfig;
        
        [Header("Upgrade Configuration")]
        [Tooltip("Can Upgrade Maximum Health")]
        public bool canUpgradeMaxHealth = true;
        
        [Tooltip("Health Upgrade Step (e.g. 30 points per slot)")]
        public float healthUpgradeStep = 30f;
        
        [Tooltip("Maximum Upgradeable Health")]
        public float maxUpgradeableHealth = 180f;
        
        [Header("Visual Configuration")]
        [Tooltip("Crystal Core Material Path")]
        public string coreMaterialPath = "Art/Materials/CrystalCore";
        
        [Tooltip("Abundant Color")]
        public Color abundantColor = Color.cyan;
        
        [Tooltip("Normal Color")]
        public Color normalColor = Color.blue;
        
        [Tooltip("Low Color")]
        public Color lowColor = Color.red;
        
        
        [Header("Audio Configuration")]
        [Tooltip("Energy Consume Sound")]
        public AudioClip energyConsumeSound;
        
        [Tooltip("Energy Gain Sound")]
        public AudioClip energyGainSound;
        
        [Tooltip("Core Health Damage Sound")]
        public AudioClip coreHealthDamageSound;
        
        [Tooltip("Core Health Repair Sound")]
        public AudioClip coreHealthRepairSound;
        

        /// <summary>
        /// Validate Configuration Data
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
        /// Get Slots for Health
        /// </summary>
        public int GetSlotsForHealth(float health)
        {
            return energyPerSlot > 0 ? Mathf.FloorToInt(health / energyPerSlot) : 0;
        }

        /// <summary>
        /// Get Health for Slots
        /// </summary>
        public float GetHealthForSlots(int slots)
        {
            return slots * energyPerSlot;
        }

        /// <summary>
        /// Calculate Next Upgrade Health
        /// </summary>
        public float GetNextUpgradeHealth(float currentHealth)
        {
            if (!canUpgradeMaxHealth) return -1f;

            float nextHealth = currentHealth + healthUpgradeStep;
            if (nextHealth > maxUpgradeableHealth) return -1f;

            return nextHealth;
        }

        /// <summary>
        /// Check if Can Upgrade Health
        /// </summary>
        public bool CanUpgradeHealth(float currentHealth)
        {
            return GetNextUpgradeHealth(currentHealth) > 0f;
        }

        /// <summary>
        /// Get Color for Energy Tier
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
        /// Get Color for Chaos State
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
            // Ensure values are within reasonable range
            initialMaxCoreHealth = Mathf.Max(energyPerSlot, initialMaxCoreHealth);
            energyPerSlot = Mathf.Max(1f, energyPerSlot);
            
            if (canUpgradeMaxHealth)
            {
                healthUpgradeStep = Mathf.Max(energyPerSlot, healthUpgradeStep);
                maxUpgradeableHealth = Mathf.Max(initialMaxCoreHealth, maxUpgradeableHealth);
            }

            // Ensure slot configuration is reasonable
            if (energyPerSlot > initialMaxCoreHealth)
            {
                energyPerSlot = initialMaxCoreHealth;
                Debug.LogWarning($"CrystalCoreConfig: {name} energyPerSlot adjusted to match initialMaxCoreHealth");
            }
        }

        #endregion
    }
}
