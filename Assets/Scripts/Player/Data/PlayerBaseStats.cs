using UnityEngine;

namespace Resonance.Player.Data
{
    /// <summary>
    /// Base player statistics and configuration data.
    /// This defines the baseline stats for the player character.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Resonance/Player/Base Stats")]
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
        [SerializeField] private float _maxCarryWeight = 100f;

        [Header("Mental Health Slots")]
        [SerializeField] private int _mentalHealthSlots = 3;  // Fixed to 3 slots
        [SerializeField] private float _mentalAttackRange = 1.5f;

        [Header("Interaction")]
        [SerializeField] private float _interactionRange = 1.5f;
        [SerializeField] private LayerMask _interactionLayerMask = 1 << 7; // Layer 7 (Interactable)

        [Header("Physical Health Tiers")]
        [SerializeField] private float _healthyThreshold = 0.7f;   // 70%
        [SerializeField] private float _woundedThreshold = 0.3f;   // 30%

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
        public float MaxCarryWeight => _maxCarryWeight;

        // Mental Health Slots Properties
        public int MentalHealthSlots => _mentalHealthSlots;
        public float MentalAttackRange => _mentalAttackRange;

        // Interaction Properties
        public float InteractionRange => _interactionRange;
        public LayerMask InteractionLayerMask => _interactionLayerMask;

        // Physical Health Tier Properties
        public float HealthyThreshold => _healthyThreshold;
        public float WoundedThreshold => _woundedThreshold;

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
    /// items, buffs, level progression, etc.
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
        
        [Header("Legacy Health (Backwards Compatibility)")]
        public float currentHealth;
        public float maxHealth;
        public float healthRegenRate;

        [Header("Current Movement")]
        public float moveSpeed;
        public float runSpeedMultiplier;
        public float jumpForce;
        public int maxJumps;

        [Header("Current Combat")]
        public float attackDamage;
        public float attackCooldown;
        public float invulnerabilityTime;

        [Header("Current Inventory")]
        public int maxInventorySlots;
        public float maxCarryWeight;

        [Header("Health Tiers")]
        public MentalHealthTier mentalTier;
        public PhysicalHealthTier physicalTier;
        public int mentalHealthSlots;
        public float slotValue; // 每个slot的数值

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
            maxCarryWeight = baseStats.MaxCarryWeight;

            // Initialize health tiers
            mentalHealthSlots = baseStats.MentalHealthSlots;
            slotValue = maxMentalHealth / mentalHealthSlots;
            UpdateHealthTiers();
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
        /// Check if player is in physical death state (physical health = 0 but mental health > 0)
        /// </summary>
        public bool IsInPhysicalDeathState => currentPhysicalHealth <= 0f && currentMentalHealth > 0f;

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
}
