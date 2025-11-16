using UnityEngine;
using Resonance.Interfaces;
using Resonance.Enemies.Core;
using Resonance.Enemies.Data;
using Resonance.Utilities.Types;

namespace Resonance.Enemies.Triggers
{
    /// <summary>
    /// Weakpoint activator component - manages weakpoint colliders based on enemy state
    /// Should be attached to the Weakpoints GameObject (child of Visual)
    /// Automatically finds and manages child weakpoint colliders
    /// </summary>
    public class EnemyHitboxManager : MonoBehaviour
    {
        [SerializeField] private bool _debugMode = false;
        [Tooltip("Enable debug logging for weakpoint events")]

        // References
        private EnemyMonoBehaviour _enemyMono;
        private EnemyController _enemyController;
        private Collider[] _physicalHitboxes;  // Head, Body, etc.
        private Collider[] _coreHitboxes;    // Core, etc.
        private EnemyCrystalCoreHitbox _crystalCoreHitbox;
        
        // State
        private bool _isInitialized = false;

        #region Initialization

        /// <summary>
        /// Initialize the hitbox manager (called by EnemyMonoBehaviour)
        /// </summary>
        /// <param name="enemyMono">Enemy MonoBehaviour reference</param>
        public void Initialize(EnemyMonoBehaviour enemyMono)
        {
            _enemyMono = enemyMono;
            _enemyController = enemyMono.Controller;
            _isInitialized = true;
            
            // Setup hitbox colliders
            SetupWeakpointColliders();
            
            // Subscribe to enemy events
            _enemyController.OnUnbalanced            += HandleUnbalanced;
            _enemyController.OnUnbalancedStarted     += HandleUnbalancedStart;
            _enemyController.OnUnbalancedCompleted   += HandleUnbalancedEnd;
            _enemyController.OnCoreExposureStarted   += HandleCoreExposureStart;
            _enemyController.OnCoreExposureCompleted += HandleCoreExposureEnd;
            _enemyController.OnDeath                 += HandleDeath;
            
            // Initial state: enabled health hitboxes, disabled core hitboxes
            SetPhysicalHitboxes(true);
            SetCoreHitboxes(false);
            
            // Initially hide wave UI since core hitboxes start disabled
            _enemyMono?.HideWaveUI();
            
            if (_debugMode)
            {
                Debug.Log($"EnemyHitboxManager: Initialized with enemy controller from {enemyMono.name}");
            }
        }

        /// <summary>
        /// Setup weakpoint colliders and attach EnemyPhysicalHitbox components
        /// </summary>
        private void SetupWeakpointColliders()
        {
            _physicalHitboxes = new Collider[2];
            _coreHitboxes = new Collider[1];
            
            // Find health weakpoints (Head, etc.)
            Transform headTransform = transform.Find("Head");
            if (headTransform != null)
            {
                _physicalHitboxes[0] = GetOrCreateCollider(headTransform.gameObject);
                SetupEnemyPhysicalHitbox(headTransform.gameObject, HitboxType.Head);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("EnemyHitboxManager: No Head child found for health weakpoint");
            }

            // Find body weakpoints (Body, etc.)
            Transform bodyTransform = transform.Find("Body");
            if (bodyTransform != null)
            {
                _physicalHitboxes[1] = GetOrCreateCollider(bodyTransform.gameObject);
                SetupEnemyPhysicalHitbox(bodyTransform.gameObject, HitboxType.Body);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("EnemyHitboxManager: No Body child found for health weakpoint");
            }

            // Find core weakpoints (Core, etc.)
            Transform coreTransform = transform.Find("Core");
            if (coreTransform != null)
            {
                _coreHitboxes[0] = GetOrCreateCollider(coreTransform.gameObject);
                SetupEnemyCrystalCoreHitbox(coreTransform.gameObject, HitboxType.Core);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("EnemyHitboxManager: No Core child found for core weakpoint");
            }
            
            if (_debugMode)
            {
                Debug.Log($"EnemyHitboxManager: Found {_physicalHitboxes.Length} health and {_coreHitboxes.Length} core weakpoints");
            }
        }

        /// <summary>
        /// Get or create collider for weakpoint GameObject
        /// </summary>
        private Collider GetOrCreateCollider(GameObject weakpointObject)
        {
            Collider collider = weakpointObject.GetComponent<Collider>();
            
            if (collider == null)
            {
                // Create a default sphere collider if none exists
                SphereCollider sphereCollider = weakpointObject.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.5f; // Default radius, can be adjusted in inspector
                collider = sphereCollider;
                
                if (_debugMode)
                {
                    Debug.Log($"EnemyHitboxManager: Created default collider for {weakpointObject.name}");
                }
            }
            else
            {
                // Ensure existing collider is a non-trigger
                if (collider.isTrigger)
                {
                    collider.isTrigger = false;
                    if (_debugMode)
                    {
                        Debug.Log($"EnemyHitboxManager: Set {weakpointObject.name} collider to non-trigger");
                    }
                }
            }
            
            return collider;
        }

        /// <summary>
        /// Setup EnemyPhysicalHitbox component for a weakpoint GameObject
        /// Configures damage multipliers based on hitbox type (Head, Body only)
        /// </summary>
        private void SetupEnemyPhysicalHitbox(GameObject weakpointObject, HitboxType type)
        {
            // Get hitbox multiplier configuration from enemy runtime stats
            var multiplierConfig = _enemyController.Stats.GetHitboxMultiplierConfig(type);
            
            EnemyPhysicalHitbox existingHitbox = weakpointObject.GetComponent<EnemyPhysicalHitbox>();
            
            if (existingHitbox == null)
            {
                EnemyPhysicalHitbox newHitbox = weakpointObject.AddComponent<EnemyPhysicalHitbox>();
                newHitbox.type = type;
                
                // Apply multipliers from configuration (physical hitboxes don't use coreHealthMultiplier)
                newHitbox.balanceMultiplier = multiplierConfig.balanceMultiplier;
                
                // Initialize the hitbox with enemy reference
                newHitbox.Initialize(_enemyMono);
                
                if (_debugMode)
                {
                    Debug.Log($"EnemyHitboxManager: Added EnemyPhysicalHitbox ({type}) with multipliers from configuration - " +
                             $"Balance: x{newHitbox.balanceMultiplier:F1}");
                }
            }
            else
            {
                // Update existing hitbox multipliers from configuration
                existingHitbox.type = type;
                existingHitbox.balanceMultiplier = multiplierConfig.balanceMultiplier;
                
                existingHitbox.Initialize(_enemyMono);
                
                if (_debugMode)
                {
                    Debug.Log($"EnemyHitboxManager: Updated EnemyPhysicalHitbox ({type}) with multipliers from configuration - " +
                             $"Balance: x{existingHitbox.balanceMultiplier:F1}");
                }
            }
        }

        /// <summary>
        /// Setup EnemyCrystalCoreHitbox component for a weakpoint GameObject
        /// Configures damage multipliers based on hitbox type
        /// </summary>
        private void SetupEnemyCrystalCoreHitbox(GameObject weakpointObject, HitboxType type)
        {
            // Get hitbox multiplier configuration from enemy runtime stats
            var multiplierConfig = _enemyController.Stats.GetHitboxMultiplierConfig(type);
            
            EnemyCrystalCoreHitbox existingHitbox = weakpointObject.GetComponent<EnemyCrystalCoreHitbox>();
            
            if (existingHitbox == null)
            {
                EnemyCrystalCoreHitbox newHitbox = weakpointObject.AddComponent<EnemyCrystalCoreHitbox>();
                
                // Apply multipliers from configuration
                newHitbox.coreHealthMultiplier = multiplierConfig.coreHealthMultiplier;
                
                // Initialize the hitbox with enemy reference
                newHitbox.Initialize(_enemyMono);
                
                if (_debugMode)
                {
                    Debug.Log($"EnemyHitboxManager: Added EnemyCrystalCoreHitbox with multipliers from configuration - " +
                             $"Core: x{newHitbox.coreHealthMultiplier:F1}");
                }
            }
            else
            {
                // Update existing hitbox multipliers from configuration
                existingHitbox.coreHealthMultiplier = multiplierConfig.coreHealthMultiplier;
                
                existingHitbox.Initialize(_enemyMono);
                
                if (_debugMode)
                {
                    Debug.Log($"EnemyHitboxManager: Updated EnemyCrystalCoreHitbox with multipliers from configuration - " +
                             $"Core: x{existingHitbox.coreHealthMultiplier:F1}");
                }
            }
            
            // Store reference to crystal core hitbox
            _crystalCoreHitbox = existingHitbox ?? weakpointObject.GetComponent<EnemyCrystalCoreHitbox>();
        }

        #endregion

        void OnDestroy()
        {
            // Unsubscribe from events
            if (_isInitialized && _enemyController != null)
            {
                _enemyController.OnUnbalanced       -= HandleUnbalanced;
                _enemyController.OnUnbalancedStarted -= HandleUnbalancedStart;
                _enemyController.OnUnbalancedCompleted -= HandleUnbalancedEnd;
                _enemyController.OnDeath            -= HandleDeath;
            }
        }

        #region Event Handlers

        /// <summary>
        /// Handle unbalanced state - balance reaches 0, core hitbox becomes active
        /// </summary>
        void HandleUnbalanced()  
        { 
            SetPhysicalHitboxes(false); 
            SetCoreHitboxes(true);
            
            // Show wave UI when core hitboxes (including Core) are enabled
            _enemyMono?.ShowWaveUI();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Unbalanced - disabled physical hitboxes, enabled core weakpoints, showing wave UI");
            }
        }
        
        /// <summary>
        /// Handle unbalanced state started - entering unbalanced state
        /// </summary>
        void HandleUnbalancedStart()  
        { 
            SetPhysicalHitboxes(false); 
            SetCoreHitboxes(true);
            
            // Show wave UI when core hitboxes (including Core) are enabled
            _enemyMono?.ShowWaveUI();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Unbalanced started - disabled physical hitboxes, enabled core weakpoints, showing wave UI");
            }
        }
        
        /// <summary>
        /// Handle unbalanced state ended - balance restored, returning to normal
        /// </summary>
        void HandleUnbalancedEnd()    
        { 
            SetPhysicalHitboxes(true);  
            SetCoreHitboxes(false);
            
            // Hide wave UI when core hitboxes (including Core) are disabled
            _enemyMono?.HideWaveUI();
            
            // Force update collider states to ensure proper synchronization
            ForceRefreshColliderStates();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Unbalanced ended - enabled physical hitboxes, disabled core weakpoints, hiding wave UI, refreshed collider states");
            }
        }
        
        /// <summary>
        /// Handle core exposure started - being executed by player wave attack
        /// </summary>
        void HandleCoreExposureStart()  
        { 
            SetPhysicalHitboxes(false); 
            SetCoreHitboxes(true);
            
            // Keep wave UI visible during core exposure
            _enemyMono?.ShowWaveUI();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Core exposure started - disabled physical hitboxes, enabled core weakpoints, showing wave UI");
            }
        }
        
        /// <summary>
        /// Handle core exposure ended - balance fully restored, returning to normal
        /// </summary>
        void HandleCoreExposureEnd()    
        { 
            SetPhysicalHitboxes(true);  
            SetCoreHitboxes(false);
            
            // Hide wave UI when core hitboxes (including Core) are disabled
            _enemyMono?.HideWaveUI();
            
            // Force update collider states to ensure proper synchronization
            ForceRefreshColliderStates();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Core exposure ended - enabled physical hitboxes, disabled core weakpoints, hiding wave UI, refreshed collider states");
            }
        }
        
        /// <summary>
        /// Handle death - core health reaches 0, enemy permanently dead
        /// </summary>
        void HandleDeath()
        { 
            SetPhysicalHitboxes(false); 
            SetCoreHitboxes(false);
            
            // Hide wave UI when all hitboxes are disabled
            _enemyMono?.HideWaveUI();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Death - disabled all weakpoints, hiding wave UI");
            }
        }

        /// <summary>
        /// Force refresh all collider states to ensure synchronization after revival end
        /// Ensures core hitboxes are disabled and health hitboxes are enabled
        /// </summary>
        private void ForceRefreshColliderStates()
        {
            if (!_isInitialized) return;
            
            // Force disable all core hitboxes (including Core) to ensure they're properly turned off
            foreach (var coreHitbox in _coreHitboxes)
            {
                if (coreHitbox != null)
                {
                    var collider = coreHitbox.GetComponent<Collider>();
                    if (collider != null && collider.enabled)
                    {
                        // Force disable to trigger state change events
                        collider.enabled = false;
                        
                        if (_debugMode)
                        {
                            Debug.Log($"EnemyHitboxManager: Force disabled core hitbox {coreHitbox} to ensure proper state");
                        }
                    }
                }
            }
            
            // Force enable all health hitboxes to ensure they're properly turned on
            foreach (var healthHitbox in _physicalHitboxes)
            {
                if (healthHitbox != null)
                {
                    var collider = healthHitbox.GetComponent<Collider>();
                    if (collider != null && !collider.enabled)
                    {
                        // Force enable to trigger state change events
                        collider.enabled = true;
                        
                        if (_debugMode)
                        {
                            Debug.Log($"EnemyHitboxManager: Force enabled health hitbox {healthHitbox} to ensure proper state");
                        }
                    }
                }
            }
        }

        #endregion

        #region Weakpoint Control

        /// <summary>
        /// Enable/disable all health weakpoints
        /// </summary>
        void SetPhysicalHitboxes(bool enabled) 
        { 
            if (_physicalHitboxes != null)
            {
                foreach (var collider in _physicalHitboxes)
                {
                    if (collider != null)
                    {
                        collider.enabled = enabled;
                    }
                }
            }
        }
        
        /// <summary>
        /// Enable/disable all core weakpoints
        /// </summary>
        void SetCoreHitboxes(bool enabled)   
        { 
            if (_coreHitboxes != null)
            {
                foreach (var collider in _coreHitboxes)
                {
                    if (collider != null)
                    {
                        collider.enabled = enabled;
                    }
                }
            }
        }

        #endregion

        #region Wave Attack Collider Management

        /// <summary>
        /// Enable enemy's crystal core collider for wave attack
        /// Called when enemy starts wave attack action
        /// </summary>
        public void EnableCoreColliderForWaveAttack()
        {
            if (!_isInitialized) return;
            
            // Enable only the core hitboxes (keep physical hitboxes as they are)
            SetCoreHitboxes(true);
            
            // Show wave UI when core hitboxes are enabled
            _enemyMono?.ShowWaveUI();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Enabled core collider for wave attack");
            }
        }

        /// <summary>
        /// Disable enemy's crystal core collider after wave attack
        /// Called when enemy ends wave attack action
        /// Should only disable if enemy is not in CoreExposed state
        /// </summary>
        public void DisableCoreColliderAfterWaveAttack()
        {
            if (!_isInitialized) return;
            
            // Only disable if enemy is not in Unbalanced, Dead or CoreExposed state
            // (CoreExposed state needs core colliders enabled for player wave attacks)
            if (_enemyController != null &&  
                _enemyController.CurrentState != EnemyState.Unbalanced && 
                _enemyController.CurrentState != EnemyState.Dead && 
                _enemyController.CurrentState != EnemyState.CoreExposed)
            {
                SetCoreHitboxes(false);
                
                // Hide wave UI when core hitboxes are disabled
                _enemyMono?.HideWaveUI();
                
                if (_debugMode)
                {
                    Debug.Log("EnemyHitboxManager: Disabled core collider after wave attack");
                }
            }
            else if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Keeping core collider enabled (enemy in CoreExposed state)");
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Check if weakpoint activator is properly initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Get count of health weakpoints
        /// </summary>
        public int PhysicalHitboxCount => _physicalHitboxes?.Length ?? 0;

        /// <summary>
        /// Get count of core weakpoints
        /// </summary>
        public int CoreHitboxCount => _coreHitboxes?.Length ?? 0;

        /// <summary>
        /// Get the enemy's crystal core hitbox
        /// </summary>
        /// <returns>EnemyCrystalCoreHitbox or null if not found</returns>
        public EnemyCrystalCoreHitbox GetCrystalCoreHitbox()
        {
            return _crystalCoreHitbox;
        }

        #endregion
    }
}