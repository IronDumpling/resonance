using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Player.Data
{
    /// <summary>
    /// Base player statistics and configuration data.
    /// This defines the baseline stats for the player character.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Wave/Player/Base Stats")]
    public class PlayerBaseStats : ScriptableObject
    {
        [Header("Physical Health")]
        [SerializeField] private float _maxPhysicalHealth = 100f;
        [SerializeField] private float _physicalHealthRegenRate = 0f; // Physical health per second
        
        [Header("Mental Health")]
        [SerializeField] private float _maxMentalHealth = 50f;
        [SerializeField] private float _mentalHealthDecayRate = 1f; // Mental health decay per second when in core mode
        [SerializeField] private float _mentalHealthRegenRate = 0f; // Mental health regen per second in normal state

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _runSpeedMultiplier = 1.5f;

        [Header("Combat")]
        [SerializeField] private float _attackDamage = 25f;
        [SerializeField] private float _attackCooldown = 0.5f;
        [SerializeField] private float _invulnerabilityTime = 1f; // After taking damage

        [Header("Inventory")]
        [SerializeField] private int _maxInventorySlots = 20;

        [Header("Mental Health Slots")]
        [SerializeField] private int _mentalHealthSlots = 3;  // Fixed to 3 slots
        [SerializeField] private float _mentalAttackRange = 1.5f;

        [Header("Interaction")]
        [SerializeField] private float _interactionRange = 1.5f;
        [SerializeField] private LayerMask _interactionLayerMask = 1 << 7; // Layer 7 (Interactable)

        [Header("Physical Health Tiers")]
        [SerializeField] private float _healthyThreshold = 0.7f;   // 70%
        [SerializeField] private float _woundedThreshold = 0.3f;   // 30%

        [Header("Ammo Inventory")]
        [SerializeField] private AmmoInventoryConfig _defaultAmmoInventory;

        // Dual Health Properties
        public float MaxPhysicalHealth => _maxPhysicalHealth;
        public float PhysicalHealthRegenRate => _physicalHealthRegenRate;
        public float MaxMentalHealth => _maxMentalHealth;
        public float MentalHealthDecayRate => _mentalHealthDecayRate;
        public float MentalHealthRegenRate => _mentalHealthRegenRate;
        
        // Other Properties
        public float MoveSpeed => _moveSpeed;
        public float RunSpeedMultiplier => _runSpeedMultiplier;
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float InvulnerabilityTime => _invulnerabilityTime;
        public int MaxInventorySlots => _maxInventorySlots;

        // Mental Health Slots Properties
        public int MentalHealthSlots => _mentalHealthSlots;
        public float MentalAttackRange => _mentalAttackRange;

        // Interaction Properties
        public float InteractionRange => _interactionRange;
        public LayerMask InteractionLayerMask => _interactionLayerMask;

        // Physical Health Tier Properties
        public float HealthyThreshold => _healthyThreshold;
        public float WoundedThreshold => _woundedThreshold;

        // Ammo Inventory Properties
        public AmmoInventoryConfig DefaultAmmoInventory => _defaultAmmoInventory;

        /// <summary>
        /// Create a runtime copy of these stats that can be modified
        /// </summary>
        public PlayerRuntimeStats CreateRuntimeStats()
        {
            return new PlayerRuntimeStats(this);
        }
    }

    /// <summary>
    /// Runtime player stats that can be modified during gameplay.
    /// These are the actual values used during play and can be affected by
    /// items, buffs, progression, etc.
    /// </summary>
    [System.Serializable]
    public class PlayerRuntimeStats
    {
        [Header("Current Physical Health")]
        public float currentPhysicalHealth;
        public float maxPhysicalHealth;
        public float physicalHealthRegenRate;
        
        [Header("Current Mental Health")]
        public float currentMentalHealth;
        public float maxMentalHealth;
        public float mentalHealthDecayRate;
        public float mentalHealthRegenRate;

        [Header("Current Movement")]
        public float moveSpeed;
        public float runSpeedMultiplier;

        [Header("Current Combat")]
        public float attackDamage;
        public float attackCooldown;
        public float invulnerabilityTime;

        [Header("Current Inventory")]
        public int maxInventorySlots;

        [Header("Health Tiers")]
        public MentalHealthTier mentalTier;
        public PhysicalHealthTier physicalTier;
        public int mentalHealthSlots;
        public float slotValue; // 每个slot的数值

        [Header("Ammo Inventory")]
        public PlayerAmmoInventory ammoInventory;

        public PlayerRuntimeStats(PlayerBaseStats baseStats)
        {
            // Copy dual health stats to runtime stats
            maxPhysicalHealth = baseStats.MaxPhysicalHealth;
            currentPhysicalHealth = maxPhysicalHealth; // Start at full physical health
            physicalHealthRegenRate = baseStats.PhysicalHealthRegenRate;
            
            maxMentalHealth = baseStats.MaxMentalHealth;
            currentMentalHealth = maxMentalHealth; // Start at full mental health
            mentalHealthDecayRate = baseStats.MentalHealthDecayRate;
            mentalHealthRegenRate = baseStats.MentalHealthRegenRate;
            
            moveSpeed = baseStats.MoveSpeed;
            runSpeedMultiplier = baseStats.RunSpeedMultiplier;
            
            attackDamage = baseStats.AttackDamage;
            attackCooldown = baseStats.AttackCooldown;
            invulnerabilityTime = baseStats.InvulnerabilityTime;
            
            maxInventorySlots = baseStats.MaxInventorySlots;

            // Initialize health tiers
            mentalHealthSlots = baseStats.MentalHealthSlots;
            slotValue = maxMentalHealth / mentalHealthSlots;
            UpdateHealthTiers();

            // Initialize ammo inventory
            ammoInventory = new PlayerAmmoInventory(baseStats.DefaultAmmoInventory);
        }

        /// <summary>
        /// Restore all health to maximum (used at save points)
        /// </summary>
        public void RestoreToFullHealth()
        {
            currentPhysicalHealth = maxPhysicalHealth;
            currentMentalHealth = maxMentalHealth;
        }

        /// <summary>
        /// Restore only physical health to maximum
        /// </summary>
        public void RestorePhysicalHealth()
        {
            currentPhysicalHealth = maxPhysicalHealth;
        }

        /// <summary>
        /// Restore only mental health to maximum
        /// </summary>
        public void RestoreMentalHealth()
        {
            currentMentalHealth = maxMentalHealth;
        }

        /// <summary>
        /// Check if player is physically alive (physical health > 0)
        /// </summary>
        public bool IsPhysicallyAlive => currentPhysicalHealth > 0f;

        /// <summary>
        /// Check if player is mentally alive (mental health > 0)
        /// </summary>
        public bool IsMentallyAlive => currentMentalHealth > 0f;

        /// <summary>
        /// Check if player is in death state (physical health = 0)
        /// </summary>
        public bool IsInDeathState => currentPhysicalHealth <= 0f;

        /// <summary>
        /// Get physical health percentage (0-1)
        /// </summary>
        public float PhysicalHealthPercentage => maxPhysicalHealth > 0 ? currentPhysicalHealth / maxPhysicalHealth : 0f;

        /// <summary>
        /// Get mental health percentage (0-1)
        /// </summary>
        public float MentalHealthPercentage => maxMentalHealth > 0 ? currentMentalHealth / maxMentalHealth : 0f;
        
        /// <summary>
        /// Update health tiers based on current health values
        /// </summary>
        public void UpdateHealthTiers()
        {
            // Update slot value in case mental health max changed
            slotValue = maxMentalHealth / mentalHealthSlots;
            
            // Mental Tier calculation
            if (currentMentalHealth <= 0f)
                mentalTier = MentalHealthTier.Empty;
            else if (currentMentalHealth <= slotValue)
                mentalTier = MentalHealthTier.Low;
            else
                mentalTier = MentalHealthTier.High;
                
            // Physical Tier calculation  
            float physicalPercent = PhysicalHealthPercentage;
            if (physicalPercent > 0.7f)
                physicalTier = PhysicalHealthTier.Healthy;
            else if (physicalPercent > 0.3f)
                physicalTier = PhysicalHealthTier.Wounded;
            else
                physicalTier = PhysicalHealthTier.Critical;
        }

        /// <summary>
        /// Check if player can consume one mental health slot
        /// </summary>
        public bool CanConsumeSlot() => currentMentalHealth >= slotValue;

        /// <summary>
        /// Consume one mental health slot (precise slot value)
        /// </summary>
        /// <returns>True if successful, false if insufficient mental health</returns>
        public bool ConsumeSlot()
        {
            if (!CanConsumeSlot()) return false;
            
            currentMentalHealth = Mathf.Max(0f, currentMentalHealth - slotValue);
            UpdateHealthTiers();
            return true;
        }

        /// <summary>
        /// Get current mental health in slot units
        /// </summary>
        public float GetMentalHealthInSlots() => slotValue > 0 ? currentMentalHealth / slotValue : 0f;
    }

    /// <summary>
    /// Configuration for default ammo inventory setup
    /// </summary>
    [System.Serializable]
    public class AmmoInventoryConfig
    {
        [Header("Default Ammo Types")]
        public List<string> ammoTypes = new List<string> { "Pisto" };
        
        [Header("Default Ammo Counts")]
        public List<int> ammoCounts = new List<int> { 6 };
        
        /// <summary>
        /// Get default ammo as dictionary for easy initialization
        /// </summary>
        public Dictionary<string, int> GetDefaultAmmo()
        {
            var result = new Dictionary<string, int>();
            
            for (int i = 0; i < ammoTypes.Count && i < ammoCounts.Count; i++)
            {
                if (!string.IsNullOrEmpty(ammoTypes[i]))
                {
                    result[ammoTypes[i]] = Mathf.Max(0, ammoCounts[i]);
                }
            }
            
            return result;
        }
    }

    /// <summary>
    /// Player's ammo inventory system
    /// Manages different types of ammunition and their quantities
    /// Simple two-layer structure: PlayerAmmoInventory -> Dictionary<string, int>
    /// Serialization handled by save system, not direct Unity serialization
    /// </summary>
    public class PlayerAmmoInventory
    {
        // Single source of truth - runtime dictionary
        private Dictionary<string, int> _ammoCount = new Dictionary<string, int>();
        
        // Events for ammo changes
        public System.Action<string, int> OnAmmoAdded; // ammoType, amount added
        public System.Action<string, int, int> OnAmmoChanged; // ammoType, oldAmount, newAmount

        public PlayerAmmoInventory()
        {
            _ammoCount = new Dictionary<string, int>();
        }

        public PlayerAmmoInventory(AmmoInventoryConfig config)
        {
            _ammoCount = new Dictionary<string, int>();
            
            if (config != null)
            {
                var defaultAmmo = config.GetDefaultAmmo();
                foreach (var kvp in defaultAmmo)
                {
                    _ammoCount[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Check if player has enough ammo of specified type
        /// </summary>
        /// <param name="ammoType">Type of ammo to check</param>
        /// <param name="amount">Amount needed (default: 1)</param>
        /// <returns>True if player has enough ammo</returns>
        public bool HasAmmo(string ammoType, int amount = 1)
        {
            if (string.IsNullOrEmpty(ammoType) || amount <= 0)
                return false;
                
            return _ammoCount.GetValueOrDefault(ammoType, 0) >= amount;
        }

        /// <summary>
        /// Consume specified amount of ammo
        /// </summary>
        /// <param name="ammoType">Type of ammo to consume</param>
        /// <param name="amount">Amount to consume</param>
        /// <returns>True if successful, false if insufficient ammo</returns>
        public bool ConsumeAmmo(string ammoType, int amount)
        {
            if (string.IsNullOrEmpty(ammoType) || amount <= 0)
                return false;
            
            int oldAmount = _ammoCount.GetValueOrDefault(ammoType, 0);
            if (oldAmount < amount)
                return false;
                
            int newAmount = oldAmount - amount;
            _ammoCount[ammoType] = newAmount;
            
            Debug.Log($"PlayerAmmoInventory: Consumed {amount} {ammoType} ammo. Remaining: {newAmount}");
            
            // Trigger events
            OnAmmoChanged?.Invoke(ammoType, oldAmount, newAmount);
            return true;
        }

        /// <summary>
        /// Add ammo to inventory
        /// </summary>
        /// <param name="ammoType">Type of ammo to add</param>
        /// <param name="amount">Amount to add</param>
        public void AddAmmo(string ammoType, int amount)
        {
            if (string.IsNullOrEmpty(ammoType) || amount <= 0)
                return;
            
            int oldAmount = _ammoCount.GetValueOrDefault(ammoType, 0);
            int newAmount = oldAmount + amount;
            _ammoCount[ammoType] = newAmount;
            
            Debug.Log($"PlayerAmmoInventory: Added {amount} {ammoType} ammo. Total: {newAmount}");
            
            // Trigger events
            OnAmmoAdded?.Invoke(ammoType, amount);
            OnAmmoChanged?.Invoke(ammoType, oldAmount, newAmount);
        }

        /// <summary>
        /// Get current count of specific ammo type
        /// </summary>
        /// <param name="ammoType">Type of ammo to check</param>
        /// <returns>Current count</returns>
        public int GetAmmoCount(string ammoType)
        {
            if (string.IsNullOrEmpty(ammoType))
                return 0;
                
            return _ammoCount.GetValueOrDefault(ammoType, 0);
        }

        /// <summary>
        /// Get all ammo types and their counts
        /// </summary>
        /// <returns>Dictionary of ammo types and counts</returns>
        public Dictionary<string, int> GetAllAmmo()
        {
            return new Dictionary<string, int>(_ammoCount);
        }

        /// <summary>
        /// Get list of all available ammo types
        /// </summary>
        /// <returns>List of ammo type names</returns>
        public List<string> GetAvailableAmmoTypes()
        {
            var types = new List<string>();
            
            foreach (var kvp in _ammoCount)
            {
                if (kvp.Value > 0)
                {
                    types.Add(kvp.Key);
                }
            }
            
            return types;
        }

        /// <summary>
        /// Check if inventory has any ammo at all
        /// </summary>
        /// <returns>True if any ammo is available</returns>
        public bool HasAnyAmmo()
        {
            foreach (var kvp in _ammoCount)
            {
                if (kvp.Value > 0)
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Get total ammo count across all types
        /// </summary>
        /// <returns>Total ammo count</returns>
        public int GetTotalAmmoCount()
        {
            int total = 0;
            foreach (var kvp in _ammoCount)
            {
                total += kvp.Value;
            }
            
            return total;
        }

        /// <summary>
        /// Clear all ammo (for testing or special events)
        /// </summary>
        public void ClearAllAmmo()
        {
            _ammoCount.Clear();
            Debug.Log("PlayerAmmoInventory: All ammo cleared");
        }

        /// <summary>
        /// Set ammo count for a specific type (for testing or special events)
        /// </summary>
        /// <param name="ammoType">Type of ammo</param>
        /// <param name="count">New count</param>
        public void SetAmmoCount(string ammoType, int count)
        {
            if (string.IsNullOrEmpty(ammoType))
                return;
            
            int oldAmount = _ammoCount.GetValueOrDefault(ammoType, 0);
            int newAmount = Mathf.Max(0, count);
            _ammoCount[ammoType] = newAmount;
            
            Debug.Log($"PlayerAmmoInventory: Set {ammoType} ammo to {newAmount}");
            
            // Trigger events if amount actually changed
            if (oldAmount != newAmount)
            {
                OnAmmoChanged?.Invoke(ammoType, oldAmount, newAmount);
            }
        }

        /// <summary>
        /// Load ammo data from save system
        /// </summary>
        /// <param name="ammoData">Dictionary of ammo type to count</param>
        public void LoadFromSaveData(Dictionary<string, int> ammoData)
        {
            _ammoCount.Clear();
            
            if (ammoData != null)
            {
                foreach (var kvp in ammoData)
                {
                    if (!string.IsNullOrEmpty(kvp.Key))
                    {
                        _ammoCount[kvp.Key] = Mathf.Max(0, kvp.Value);
                    }
                }
            }
            
            Debug.Log($"PlayerAmmoInventory: Loaded {_ammoCount.Count} ammo types from save data");
        }

        /// <summary>
        /// Get ammo data for save system
        /// </summary>
        /// <returns>Dictionary of ammo type to count for serialization</returns>
        public Dictionary<string, int> GetSaveData()
        {
            return new Dictionary<string, int>(_ammoCount);
        }
    }
}
