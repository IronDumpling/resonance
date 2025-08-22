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
        [Header("Health")]
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

        // Properties
        public float MaxHealth => _maxHealth;
        public float HealthRegenRate => _healthRegenRate;
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
        [Header("Current Health")]
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
            // Copy base stats to runtime stats
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
        /// Restore health to maximum (used at save points)
        /// </summary>
        public void RestoreToFullHealth()
        {
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Check if player is alive
        /// </summary>
        public bool IsAlive => currentHealth > 0f;

        /// <summary>
        /// Get health percentage (0-1)
        /// </summary>
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }
}
