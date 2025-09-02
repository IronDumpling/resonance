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
        private Collider[] _physicalHitboxes;  // Head, Body, etc.
        private Collider[] _mentalHitboxes;    // Core, etc.
        
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
            
            // Initial state: enabled physical hitboxes, disabled mental hitboxes
            SetPhysicalHitboxes(true);
            SetMentalHitboxes(false);
            
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
            _physicalHitboxes = new Collider[2];
            _mentalHitboxes = new Collider[1];
            
            // Find physical weakpoints (Head, etc.)
            Transform headTransform = transform.Find("Head");
            if (headTransform != null)
            {
                _physicalHitboxes[0] = GetOrCreateCollider(headTransform.gameObject);
                SetupEnemyHitbox(headTransform.gameObject, EnemyHitboxType.Head);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("EnemyHitboxManager: No Head child found for physical weakpoint");
            }

            // Find body weakpoints (Body, etc.)
            Transform bodyTransform = transform.Find("Body");
            if (bodyTransform != null)
            {
                _physicalHitboxes[1] = GetOrCreateCollider(bodyTransform.gameObject);
                SetupEnemyHitbox(bodyTransform.gameObject, EnemyHitboxType.Body);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("EnemyHitboxManager: No Body child found for physical weakpoint");
            }

            // Find mental weakpoints (Core, etc.)
            Transform coreTransform = transform.Find("Core");
            if (coreTransform != null)
            {
                _mentalHitboxes[0] = GetOrCreateCollider(coreTransform.gameObject);
                SetupEnemyHitbox(coreTransform.gameObject, EnemyHitboxType.Core);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("EnemyHitboxManager: No Core child found for mental weakpoint");
            }
            
            if (_debugMode)
            {
                Debug.Log($"EnemyHitboxManager: Found {_physicalHitboxes.Length} physical and {_mentalHitboxes.Length} mental weakpoints");
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
                        newHitbox.physicalMultiplier = 2f;
                        newHitbox.mentalMultiplier = 0f;
                        newHitbox.convertPhysicalToMental = 0f;
                        break;
                    case EnemyHitboxType.Body:
                        newHitbox.physicalMultiplier = 1f;
                        newHitbox.mentalMultiplier = 0f;
                        newHitbox.convertPhysicalToMental = 0f;
                        break;
                    case EnemyHitboxType.Core:
                        newHitbox.physicalMultiplier = 0f;
                        newHitbox.mentalMultiplier = 1.5f;
                        newHitbox.convertPhysicalToMental = 0f; 
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
            SetMentalHitboxes(true);
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Physical death - disabled physical, enabled mental weakpoints");
            }
        }
        
        void HandleRevivingStart()  
        { 
            SetPhysicalHitboxes(false); 
            SetMentalHitboxes(true);
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Revival started - disabled physical, enabled mental weakpoints");
            }
        }
        
        void HandleRevivingEnd()    
        { 
            SetPhysicalHitboxes(true);  
            SetMentalHitboxes(false);
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: Revival ended - enabled physical, disabled mental weakpoints");
            }
        }
        
        void HandleTrueDeath()
        { 
            SetPhysicalHitboxes(false); 
            SetMentalHitboxes(false);
            
            if (_debugMode)
            {
                Debug.Log("EnemyHitboxManager: True death - disabled all weakpoints");
            }
        }

        #endregion

        #region Weakpoint Control

        /// <summary>
        /// Enable/disable all physical weakpoints
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
        /// Enable/disable all mental weakpoints
        /// </summary>
        void SetMentalHitboxes(bool enabled)   
        { 
            if (_mentalHitboxes != null)
            {
                foreach (var collider in _mentalHitboxes)
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
        /// Get count of physical weakpoints
        /// </summary>
        public int PhysicalHitboxCount => _physicalHitboxes?.Length ?? 0;

        /// <summary>
        /// Get count of mental weakpoints
        /// </summary>
        public int MentalHitboxCount => _mentalHitboxes?.Length ?? 0;

        #endregion
    }
}