using UnityEngine;
using Resonance.Interfaces;
using Resonance.Enemies.Core;

namespace Resonance.Enemies
{
    /// <summary>
    /// Weakpoint activator component - manages weakpoint colliders based on enemy state
    /// Should be attached to the Weakpoints GameObject (child of Visual)
    /// Automatically finds and manages child weakpoint colliders
    /// </summary>
    public class EnemyHitboxManager : MonoBehaviour
    {
        [Header("Weakpoint Configuration")]
        [SerializeField] private bool _debugMode = false;
        [Tooltip("Enable debug logging for weakpoint events")]

        // References
        private EnemyMonoBehaviour _enemyMono;
        private EnemyController _enemyController;
        private Collider[] _healthHitboxes;  // Head, Body, etc.
        private Collider[] _coreHitboxes;    // Core, etc.
        
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
            _enemyController.OnPhysicalDeath    += HandlePhysicalDeath;
            _enemyController.OnRevivalStarted   += HandleRevivingStart;
            _enemyController.OnRevivalCompleted += HandleRevivingEnd;
            _enemyController.OnTrueDeath        += HandleTrueDeath;
            
            // Initial state: enabled health hitboxes, disabled core hitboxes
            SetPhysicalHitboxes(true);
            SetCoreHitboxes(false);
            
            // Initially hide resonance UI since core hitboxes start disabled
            _enemyMono?.HideWaveUI();
            
            if (_debugMode)
            {
                Debug.Log($"EnemyHitboxManager: Initialized with enemy controller from {enemyMono.name}");
            }
        }

        /// <summary>
        /// Setup weakpoint colliders and attach EnemyHitbox components
        /// </summary>
        private void SetupWeakpointColliders()
        {
            _healthHitboxes = new Collider[2];
            _coreHitboxes = new Collider[1];
            
            // Find health weakpoints (Head, etc.)
            Transform headTransform = transform.Find("Head");
            if (headTransform != null)
            {
                _healthHitboxes[0] = GetOrCreateCollider(headTransform.gameObject);
                SetupEnemyHitbox(headTransform.gameObject, EnemyHitboxType.Head);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("EnemyHitboxManager: No Head child found for health weakpoint");
            }

            // Find body weakpoints (Body, etc.)
            Transform bodyTransform = transform.Find("Body");
            if (bodyTransform != null)
            {
                _healthHitboxes[1] = GetOrCreateCollider(bodyTransform.gameObject);
                SetupEnemyHitbox(bodyTransform.gameObject, EnemyHitboxType.Body);
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
                SetupEnemyHitbox(coreTransform.gameObject, EnemyHitboxType.Core);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("EnemyHitboxManager: No Core child found for core weakpoint");
            }
            
            if (_debugMode)
            {
                Debug.Log($"EnemyHitboxManager: Found {_healthHitboxes.Length} health and {_coreHitboxes.Length} core weakpoints");
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
        /// Setup EnemyHitbox component for a weakpoint GameObject
        /// </summary>
        private void SetupEnemyHitbox(GameObject weakpointObject, EnemyHitboxType type)
        {
            EnemyHitbox existingHitbox = weakpointObject.GetComponent<EnemyHitbox>();
            
            if (existingHitbox == null)
            {
                EnemyHitbox newHitbox = weakpointObject.AddComponent<EnemyHitbox>();
                newHitbox.type = type;
                
                switch (type)
                {
                    case EnemyHitboxType.Head:
                        newHitbox.healthMultiplier = 2f;
                        newHitbox.coreMultiplier = 0f;
                        newHitbox.resilienceMultiplier = 2f;
                        break;
                    case EnemyHitboxType.Body:
                        newHitbox.healthMultiplier = 1f;
                        newHitbox.coreMultiplier = 0f;
                        newHitbox.resilienceMultiplier = 1f;
                        break;
                    case EnemyHitboxType.Core:
                        newHitbox.healthMultiplier = 0f;
                        newHitbox.coreMultiplier = 1.5f;
                        newHitbox.resilienceMultiplier = 0f; 
                        break;
                }
                
                // Initialize the weakpoint hitbox with enemy reference
                newHitbox.Initialize(_enemyMono);
                
                if (_debugMode)
                {
                    Debug.Log($"EnemyHitboxManager: Added and initialized EnemyHitbox ({type}) to {weakpointObject.name}");
                }
            }
            else
            {
                // Ensure existing hitbox has correct type and is initialized
                existingHitbox.type = type;
                existingHitbox.Initialize(_enemyMono);
                
                if (_debugMode)
                {
                    Debug.Log($"EnemyHitboxManager: Updated and initialized existing EnemyHitbox on {weakpointObject.name}");
                }
            }
        }

        #endregion

        void OnDestroy()
        {
            // Unsubscribe from events
            if (_isInitialized && _enemyController != null)
            {
                _enemyController.OnPhysicalDeath    -= HandlePhysicalDeath;
                _enemyController.OnRevivalStarted   -= HandleRevivingStart;
                _enemyController.OnRevivalCompleted -= HandleRevivingEnd;
                _enemyController.OnTrueDeath        -= HandleTrueDeath;
            }
        }

        #region Event Handlers

        void HandlePhysicalDeath()  
        { 
            SetPhysicalHitboxes(false); 
            SetCoreHitboxes(true);
            
            // Show resonance UI when core hitboxes (including Core) are enabled
            _enemyMono?.ShowWaveUI();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Physical death - disabled health, enabled core weakpoints, showing resonance UI");
            }
        }
        
        void HandleRevivingStart()  
        { 
            SetPhysicalHitboxes(false); 
            SetCoreHitboxes(true);
            
            // Show resonance UI when core hitboxes (including Core) are enabled
            _enemyMono?.ShowWaveUI();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Revival started - disabled health, enabled core weakpoints, showing resonance UI");
            }
        }
        
        void HandleRevivingEnd()    
        { 
            SetPhysicalHitboxes(true);  
            SetCoreHitboxes(false);
            
            // Hide resonance UI when core hitboxes (including Core) are disabled
            _enemyMono?.HideWaveUI();
            
            // Force update collider states to ensure proper synchronization
            ForceRefreshColliderStates();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Revival ended - enabled health, disabled core weakpoints, hiding resonance UI, refreshed collider states");
            }
        }
        
        void HandleTrueDeath()
        { 
            SetPhysicalHitboxes(false); 
            SetCoreHitboxes(false);
            
            // Hide resonance UI when all hitboxes are disabled
            _enemyMono?.HideWaveUI();
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: True death - disabled all weakpoints, hiding resonance UI");
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
            foreach (var healthHitbox in _healthHitboxes)
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
            if (_healthHitboxes != null)
            {
                foreach (var collider in _healthHitboxes)
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

        #region Public Interface

        /// <summary>
        /// Check if weakpoint activator is properly initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Get count of health weakpoints
        /// </summary>
        public int PhysicalHitboxCount => _healthHitboxes?.Length ?? 0;

        /// <summary>
        /// Get count of core weakpoints
        /// </summary>
        public int CoreHitboxCount => _coreHitboxes?.Length ?? 0;

        #endregion
    }
}