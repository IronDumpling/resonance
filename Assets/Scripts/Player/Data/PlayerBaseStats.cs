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
        
        [Header("Legacy Health (Backwards Compatibility)")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _healthRegenRate = 0f; // Health per second

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _runSpeedMultiplier = 1.5f;
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private int _maxJumps = 1; // Air jumps + ground jump

        [Header("Combat")]
        [SerializeField] private float _attackDamage = 25f;
        [SerializeField] private float _attackCooldown = 0.5f;
        [SerializeField] private float _invulnerabilityTime = 1f; // After taking damage

        [Header("Inventory")]
        [SerializeField] private int _maxInventorySlots = 20;
        [SerializeField] private float _maxCarryWeight = 100f;

        // Dual Health Properties
        public float MaxPhysicalHealth => _maxPhysicalHealth;
        public float PhysicalHealthRegenRate => _physicalHealthRegenRate;
        public float MaxMentalHealth => _maxMentalHealth;
        public float MentalHealthDecayRate => _mentalHealthDecayRate;
        public float MentalHealthRegenRate => _mentalHealthRegenRate;
        
        // Legacy Properties (Backwards Compatibility)
        public float MaxHealth => _maxHealth;
        public float HealthRegenRate => _healthRegenRate;
        
        // Other Properties
        public float MoveSpeed => _moveSpeed;
        public float RunSpeedMultiplier => _runSpeedMultiplier;
        public float JumpForce => _jumpForce;
        public int MaxJumps => _maxJumps;
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float InvulnerabilityTime => _invulnerabilityTime;
        public int MaxInventorySlots => _maxInventorySlots;
        public float MaxCarryWeight => _maxCarryWeight;

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
            
            // Copy legacy health stats for backwards compatibility
            maxHealth = baseStats.MaxHealth;
            currentHealth = maxHealth; // Start at full health
            healthRegenRate = baseStats.HealthRegenRate;
            
            moveSpeed = baseStats.MoveSpeed;
            runSpeedMultiplier = baseStats.RunSpeedMultiplier;
            jumpForce = baseStats.JumpForce;
            maxJumps = baseStats.MaxJumps;
            
            attackDamage = baseStats.AttackDamage;
            attackCooldown = baseStats.AttackCooldown;
            invulnerabilityTime = baseStats.InvulnerabilityTime;
            
            maxInventorySlots = baseStats.MaxInventorySlots;
            maxCarryWeight = baseStats.MaxCarryWeight;
        }

        /// <summary>
        /// Restore all health to maximum (used at save points)
        /// </summary>
        public void RestoreToFullHealth()
        {
            currentPhysicalHealth = maxPhysicalHealth;
            currentMentalHealth = maxMentalHealth;
            // Also restore legacy health for backwards compatibility
            currentHealth = maxHealth;
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
        /// Check if player is truly alive (both physical and mental health > 0)
        /// True death occurs when mental health reaches 0
        /// </summary>
        public bool IsTrulyAlive => currentMentalHealth > 0f;

        /// <summary>
        /// Check if player is in physical death state (physical health = 0 but mental health > 0)
        /// </summary>
        public bool IsInPhysicalDeathState => currentPhysicalHealth <= 0f && currentMentalHealth > 0f;

        /// <summary>
        /// Legacy alive check for backwards compatibility
        /// </summary>
        public bool IsAlive => currentHealth > 0f;

        /// <summary>
        /// Get physical health percentage (0-1)
        /// </summary>
        public float PhysicalHealthPercentage => maxPhysicalHealth > 0 ? currentPhysicalHealth / maxPhysicalHealth : 0f;

        /// <summary>
        /// Get mental health percentage (0-1)
        /// </summary>
        public float MentalHealthPercentage => maxMentalHealth > 0 ? currentMentalHealth / maxMentalHealth : 0f;

        /// <summary>
        /// Legacy health percentage for backwards compatibility
        /// </summary>
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }
}
