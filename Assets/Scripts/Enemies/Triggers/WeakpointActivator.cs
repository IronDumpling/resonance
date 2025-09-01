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
    public class WeakpointActivator : MonoBehaviour
    {
        [Header("Weakpoint Configuration")]
        [SerializeField] private bool _debugMode = false;
        [Tooltip("Enable debug logging for weakpoint events")]

        // References
        private EnemyMonoBehaviour _enemyMono;
        private EnemyController _enemyController;
        private Collider[] _physicalWeakpoints;  // Head等物理弱点
        private Collider[] _mentalWeakpoints;    // Core等精神弱点
        
        // State
        private bool _isInitialized = false;

        #region Initialization

        /// <summary>
        /// Initialize the weakpoint activator (called by EnemyMonoBehaviour)
        /// </summary>
        /// <param name="enemyMono">Enemy MonoBehaviour reference</param>
        public void Initialize(EnemyMonoBehaviour enemyMono)
        {
            _enemyMono = enemyMono;
            _enemyController = enemyMono.Controller;
            _isInitialized = true;
            
            // Setup weakpoint colliders
            SetupWeakpointColliders();
            
            // Subscribe to enemy events
            _enemyController.OnPhysicalDeath    += HandlePhysicalDeath;
            _enemyController.OnRevivalStarted   += HandleRevivingStart;
            _enemyController.OnRevivalCompleted += HandleRevivingEnd;
            _enemyController.OnTrueDeath        += HandleTrueDeath;
            
            // 初始：活着时只开物理弱点
            SetPhysicalWeakpoints(true);
            SetMentalWeakpoints(false);
            
            if (_debugMode)
            {
                Debug.Log($"WeakpointActivator: Initialized with enemy controller from {enemyMono.name}");
            }
        }

        /// <summary>
        /// Setup weakpoint colliders and attach WeakpointHitbox components
        /// </summary>
        private void SetupWeakpointColliders()
        {
            // Find physical weakpoints (Head, etc.)
            Transform headTransform = transform.Find("Head");
            if (headTransform != null)
            {
                _physicalWeakpoints = new Collider[] { GetOrCreateCollider(headTransform.gameObject) };
                SetupWeakpointHitbox(headTransform.gameObject, WeakpointType.Physical);
            }
            else
            {
                _physicalWeakpoints = new Collider[0];
                if (_debugMode)
                {
                    Debug.LogWarning("WeakpointActivator: No Head child found for physical weakpoint");
                }
            }

            // Find mental weakpoints (Core, etc.)
            Transform coreTransform = transform.Find("Core");
            if (coreTransform != null)
            {
                _mentalWeakpoints = new Collider[] { GetOrCreateCollider(coreTransform.gameObject) };
                SetupWeakpointHitbox(coreTransform.gameObject, WeakpointType.Mental);
            }
            else
            {
                _mentalWeakpoints = new Collider[0];
                if (_debugMode)
                {
                    Debug.LogWarning("WeakpointActivator: No Core child found for mental weakpoint");
                }
            }
            
            if (_debugMode)
            {
                Debug.Log($"WeakpointActivator: Found {_physicalWeakpoints.Length} physical and {_mentalWeakpoints.Length} mental weakpoints");
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
                    Debug.Log($"WeakpointActivator: Created default collider for {weakpointObject.name}");
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
                        Debug.Log($"WeakpointActivator: Set {weakpointObject.name} collider to non-trigger");
                    }
                }
            }
            
            return collider;
        }

        /// <summary>
        /// Setup WeakpointHitbox component for a weakpoint GameObject
        /// </summary>
        private void SetupWeakpointHitbox(GameObject weakpointObject, WeakpointType type)
        {
            WeakpointHitbox existingHitbox = weakpointObject.GetComponent<WeakpointHitbox>();
            
            if (existingHitbox == null)
            {
                WeakpointHitbox newHitbox = weakpointObject.AddComponent<WeakpointHitbox>();
                newHitbox.type = type;
                
                // Physical
                if (type == WeakpointType.Physical)
                {
                    newHitbox.physicalMultiplier = 2f;
                    newHitbox.mentalMultiplier = 0f;
                    newHitbox.convertPhysicalToMental = 0f;
                }
                else // Mental
                {
                    newHitbox.physicalMultiplier = 0f;
                    newHitbox.mentalMultiplier = 1.5f;
                    newHitbox.convertPhysicalToMental = 0.01f; 
                }
                
                // Initialize the weakpoint hitbox with enemy reference
                newHitbox.Initialize(_enemyMono);
                
                if (_debugMode)
                {
                    Debug.Log($"WeakpointActivator: Added and initialized WeakpointHitbox ({type}) to {weakpointObject.name}");
                }
            }
            else
            {
                // Ensure existing hitbox has correct type and is initialized
                existingHitbox.type = type;
                existingHitbox.Initialize(_enemyMono);
                
                if (_debugMode)
                {
                    Debug.Log($"WeakpointActivator: Updated and initialized existing WeakpointHitbox on {weakpointObject.name}");
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
            SetPhysicalWeakpoints(false); 
            SetMentalWeakpoints(true);
            
            if (_debugMode)
            {
                Debug.Log("WeakpointActivator: Physical death - disabled physical, enabled mental weakpoints");
            }
        }
        
        void HandleRevivingStart()  
        { 
            SetPhysicalWeakpoints(false); 
            SetMentalWeakpoints(true);
            
            if (_debugMode)
            {
                Debug.Log("WeakpointActivator: Revival started - disabled physical, enabled mental weakpoints");
            }
        }
        
        void HandleRevivingEnd()    
        { 
            SetPhysicalWeakpoints(true);  
            SetMentalWeakpoints(false);
            
            if (_debugMode)
            {
                Debug.Log("WeakpointActivator: Revival ended - enabled physical, disabled mental weakpoints");
            }
        }
        
        void HandleTrueDeath()
        { 
            SetPhysicalWeakpoints(false); 
            SetMentalWeakpoints(false);
            
            if (_debugMode)
            {
                Debug.Log("WeakpointActivator: True death - disabled all weakpoints");
            }
        }

        #endregion

        #region Weakpoint Control

        /// <summary>
        /// Enable/disable all physical weakpoints
        /// </summary>
        void SetPhysicalWeakpoints(bool enabled) 
        { 
            if (_physicalWeakpoints != null)
            {
                foreach (var collider in _physicalWeakpoints)
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
        void SetMentalWeakpoints(bool enabled)   
        { 
            if (_mentalWeakpoints != null)
            {
                foreach (var collider in _mentalWeakpoints)
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
        public int PhysicalWeakpointCount => _physicalWeakpoints?.Length ?? 0;

        /// <summary>
        /// Get count of mental weakpoints
        /// </summary>
        public int MentalWeakpointCount => _mentalWeakpoints?.Length ?? 0;

        #endregion
    }
}