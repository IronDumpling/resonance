using UnityEngine;

namespace Resonance.Gameplay.Enemies.Data
{
    /// <summary>
    /// Enemy state enum - unified state management
    /// Based on balance, core health and combat state
    /// </summary>
    public enum EnemyState
    {
        Normal,        // Alive and active, balance > 0
        Staggered,       // Temporarily staggered by balance damage (will be renamed to Stagger)
        Unbalanced,    // Balance depleted to 0, vulnerable to wave execution
        CoreExposed,   // Being executed by player wave attack, balance recovering
        Dead           // Core destroyed, permanently dead
    }

    /// <summary>
    /// Enemy state data class - stores all state information centrally
    /// Updated by EnemyController every frame
    /// </summary>
    public class EnemyStateData
    {
        // Original data cache (synchronized from EnemyRuntimeStats)
        private float _currentBalance;
        private float _currentCoreHealth;
        private bool _isStaggered;  // Will be renamed to _isStaggered later
        
        // Unbalanced process tracking
        // When balance reaches 0, enemy enters Unbalanced state
        private bool _isUnbalancedInProgress = false;
        
        // CoreExposed process tracking
        // When enemy is being executed by player wave attack
        // This ensures that the state remains CoreExposed throughout the entire execution process
        private bool _isCoreExposedInProgress = false;
        
        /// <summary>
        /// Current logical state (Normal/Staggered/Unbalanced/CoreExposed/Dead)
        /// Use enum instead of multiple bools to avoid state confusion
        /// </summary>
        public EnemyState CurrentState { get; private set; }
        
        /// <summary>
        /// Balance-related states
        /// </summary>
        
        // Balance is above 0 and core health exists
        public bool IsBalanced => _currentBalance > 0f && _currentCoreHealth > 0f;
        
        // Balance depleted to 0, but core health exists (can be executed)
        public bool IsUnbalanced => _currentBalance <= 0f && _currentCoreHealth > 0f;
        
        // Core health depleted (true death)
        public bool IsCoreDead => _currentCoreHealth <= 0f;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public EnemyStateData()
        {
            _currentBalance = 0f;
            _currentCoreHealth = 0f;
            _isStaggered = false;
            CurrentState = EnemyState.Dead;
        }
        
        /// <summary>
        /// Update the state data (called by EnemyController every frame)
        /// </summary>
        /// <param name="balance">Current balance value</param>
        /// <param name="coreHealth">Current core health</param>
        /// <param name="isStaggered">Whether the enemy is staggerned/staggered</param>
        public void UpdateState(float balance, float coreHealth, bool isStaggered)
        {
            // Cache the original data
            _currentBalance = balance;
            _currentCoreHealth = coreHealth;
            _isStaggered = isStaggered;
            
            // Calculate the current state (priority: Dead > CoreExposed > Staggered > Unbalanced > Normal)
            EnemyState newState;
            
            if (IsCoreDead)
            {
                newState = EnemyState.Dead;
            }
            else if (_isCoreExposedInProgress)
            {
                // If in CoreExposed process, keep the state as CoreExposed
                // (triggered by player wave attack, ends when balance is fully restored)
                newState = EnemyState.CoreExposed;
            }
            else if (_isStaggered)
            {
                // Staggered by balance damage
                newState = EnemyState.Staggered;
            }
            else if (_isUnbalancedInProgress || IsUnbalanced)
            {
                // If balance = 0 or in unbalanced process, keep the state as Unbalanced
                newState = EnemyState.Unbalanced;
            }
            else
            {
                newState = EnemyState.Normal;
            }
            
            CurrentState = newState;
        }
        
        /// <summary>
        /// Start the unbalanced process (called when balance reaches 0)
        /// Set the unbalanced flag, ensuring the state remains Unbalanced
        /// </summary>
        public void StartUnbalanced()
        {
            _isUnbalancedInProgress = true;
        }
        
        /// <summary>
        /// Complete the unbalanced process (called when timer expires or enters CoreExposed)
        /// Clear the unbalanced flag
        /// </summary>
        public void CompleteUnbalanced()
        {
            _isUnbalancedInProgress = false;
        }
        
        /// <summary>
        /// Start the core exposed process (called by player wave attack)
        /// Set the core exposed flag, ensuring the state remains CoreExposed throughout execution
        /// </summary>
        public void StartCoreExposure()
        {
            _isCoreExposedInProgress = true;
            _isUnbalancedInProgress = false; // Exit unbalanced state
        }
        
        /// <summary>
        /// Complete the core exposed process (called when balance is fully restored)
        /// Clear the core exposed flag, allowing the state to return to Normal
        /// </summary>
        public void CompleteCoreExposure()
        {
            _isCoreExposedInProgress = false;
        }
        
        /// <summary>
        /// Get the current state information (for debugging)
        /// </summary>
        public string GetStateInfo()
        {
            return $"Balance: {_currentBalance:F1}, CoreHealth: {_currentCoreHealth:F1}, " +
                   $"State: {CurrentState}, IsUnbalancedInProgress: {_isUnbalancedInProgress}, " +
                   $"IsCoreExposedInProgress: {_isCoreExposedInProgress}, " +
                   $"IsBalanced: {IsBalanced}, IsUnbalanced: {IsUnbalanced}, IsCoreDead: {IsCoreDead}";
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
                case EnemyState.Staggered:
                    return "Staggered (Staggered)";
                case EnemyState.Unbalanced:
                    return "Unbalanced";
                case EnemyState.CoreExposed:
                    return "Core Exposed";
                case EnemyState.Dead:
                    return "Dead";
                default:
                    return "Unknown";
            }
        }
    }
}
