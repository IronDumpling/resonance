using UnityEngine;

namespace Resonance.Enemies.Data
{
    /// <summary>
    /// Enemy state enum - unified state management
    /// Based on physical health, core health and combat state
    /// </summary>
    public enum EnemyState
    {
        Normal,      // Alive and active
        Stunned,     // Temporarily incapacitated by chaos damage
        Reviving,    // Physical health depleted, restoring (core alive)
        Dead         // Core destroyed, permanently dead
    }

    /// <summary>
    /// Enemy state data class - stores all state information centrally
    /// Updated by EnemyController every frame
    /// </summary>
    public class EnemyStateData
    {
        // Original data cache (synchronized from EnemyRuntimeStats)
        private float _currentHealth;
        private float _currentCoreHealth;
        private bool _isStunned;
        
        // Revival process tracking
        // When the enemy enters the revival process, set to true until the revival is complete
        // This ensures that the state remains Reviving throughout the entire revival process (even if the physical health is > 0)
        private bool _isRevivingInProgress = false;
        
        /// <summary>
        /// Current logical state (Normal/Stunned/Reviving/Dead)
        /// Use enum instead of multiple bools to avoid state confusion
        /// </summary>
        public EnemyState CurrentState { get; private set; }
        
        /// <summary>
        /// Health-related states - three mutually exclusive bools
        /// </summary>
        
        // Both physical and core health exist
        public bool IsPhysicallyAlive => _currentHealth > 0f && _currentCoreHealth > 0f;
        
        // Physical health depleted, but core health exists (can be revived)
        public bool IsPhysicallyDead => _currentHealth <= 0f && _currentCoreHealth > 0f;
        
        // Core health depleted (true death)
        public bool IsCoreDead => _currentCoreHealth <= 0f;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public EnemyStateData()
        {
            _currentHealth = 0f;
            _currentCoreHealth = 0f;
            _isStunned = false;
            CurrentState = EnemyState.Dead;
        }
        
        /// <summary>
        /// Update the state data (called by EnemyController every frame)
        /// </summary>
        /// <param name="health">Current physical health</param>
        /// <param name="coreHealth">Current core health</param>
        /// <param name="isStunned">Whether the enemy is stunned</param>
        public void UpdateState(float health, float coreHealth, bool isStunned)
        {
            // Cache the original data
            _currentHealth = health;
            _currentCoreHealth = coreHealth;
            _isStunned = isStunned;
            
            // Calculate the current state (priority: Dead > Stunned > Reviving > Normal)
            EnemyState newState;
            
            if (IsCoreDead)
            {
                newState = EnemyState.Dead;
            }
            else if (_isStunned)
            {
                newState = EnemyState.Stunned;
            }
            else if (_isRevivingInProgress || IsPhysicallyDead)
            {
                // If the physical health is > 0 but the revival is not complete, keep the state as Reviving
                newState = EnemyState.Reviving;
            }
            else
            {
                newState = EnemyState.Normal;
            }
            
            CurrentState = newState;
        }
        
        /// <summary>
        /// Start the revival process (called by BehaviorTree through EnemyController)
        /// Set the revival flag, ensuring the state remains Reviving throughout the entire revival process
        /// </summary>
        public void StartRevival()
        {
            _isRevivingInProgress = true;
        }
        
        /// <summary>
        /// Complete the revival process (called by BehaviorTree through EnemyController)
        /// Clear the revival flag, allowing the state to be calculated normally based on health
        /// </summary>
        public void CompleteRevival()
        {
            _isRevivingInProgress = false;
        }
        
        /// <summary>
        /// Get the current health information (for debugging)
        /// </summary>
        public string GetHealthInfo()
        {
            return $"Health: {_currentHealth:F1}, CoreHealth: {_currentCoreHealth:F1}, " +
                   $"State: {CurrentState}, IsRevivingInProgress: {_isRevivingInProgress}, " +
                   $"IsPhysicallyAlive: {IsPhysicallyAlive}, IsPhysicallyDead: {IsPhysicallyDead}, " +
                   $"IsCoreDead: {IsCoreDead}";
        }
    }

    /// <summary>
    /// Enemy state helper class
    /// Provides descriptions and configurations for enemy states
    /// </summary>
    public static class EnemyStateHelper
    {
        /// <summary>
        /// Get the description text for the state
        /// </summary>
        public static string GetStateDescription(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Normal:
                    return "Normal";
                case EnemyState.Stunned:
                    return "Stunned";
                case EnemyState.Reviving:
                    return "Reviving";
                case EnemyState.Dead:
                    return "Dead";
                default:
                    return "Unknown";
            }
        }
    }
}
