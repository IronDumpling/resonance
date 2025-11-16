using UnityEngine;
using Resonance.Interfaces;
using Resonance.Player.Core;
using Resonance.Utilities.Types;

namespace Resonance.Player.Triggers
{
    /// <summary>
    /// Hitbox manager component - manages player hitbox colliders
    /// Should be attached to the Visual GameObject (child of Player)
    /// Automatically finds and manages child hitbox colliders
    /// </summary>
    public class PlayerHitboxManager : MonoBehaviour
    {
        [SerializeField] private bool _debugMode = false;
        [Tooltip("Enable debug logging for hitbox events")]

        // References
        private PlayerMonoBehaviour _playerMono;
        private PlayerController _playerController;
        private PlayerCrystalCoreHitbox _crystalCoreHitbox;
        
        // State
        private bool _isInitialized = false;

        #region Initialization

        /// <summary>
        /// Initialize the hitbox manager (called by PlayerMonoBehaviour)
        /// </summary>
        /// <param name="playerMono">Player MonoBehaviour reference</param>
        public void Initialize(PlayerMonoBehaviour playerMono)
        {
            _playerMono = playerMono;
            _playerController = playerMono.Controller;
            _isInitialized = true;
            
            // Setup hitbox colliders
            SetupHitboxColliders();
            
            if (_debugMode)
            {
                Debug.Log($"PlayerHitboxManager: Initialized with player controller from {playerMono.name}");
            }
        }

        /// <summary>
        /// Setup hitbox colliders and attach hitbox components
        /// </summary>
        private void SetupHitboxColliders()
        {
            // Find Body hitbox (Physical)
            Transform bodyTransform = transform.Find("Body");
            if (bodyTransform != null)
            {
                GetOrCreateCollider(bodyTransform.gameObject);
                SetupPlayerPhysicalHitbox(bodyTransform.gameObject);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("PlayerHitboxManager: No Body child found for physical hitbox");
            }

            // Find Core hitbox
            Transform coreTransform = transform.Find("Core");
            if (coreTransform != null)
            {
                GetOrCreateCollider(coreTransform.gameObject);
                SetupPlayerCrystalCoreHitbox(coreTransform.gameObject);
            }
            else if (_debugMode)
            {
                Debug.LogWarning("PlayerHitboxManager: No Core child found for core hitbox");
            }
            
            if (_debugMode)
            {
                Debug.Log($"PlayerHitboxManager: Setup complete");
            }
        }

        /// <summary>
        /// Get or create collider for hitbox GameObject
        /// </summary>
        private Collider GetOrCreateCollider(GameObject hitboxObject)
        {
            Collider collider = hitboxObject.GetComponent<Collider>();
            
            if (collider == null)
            {
                // Create a default capsule collider if none exists
                CapsuleCollider capsuleCollider = hitboxObject.AddComponent<CapsuleCollider>();
                capsuleCollider.radius = 0.5f;
                capsuleCollider.height = 2f;
                capsuleCollider.center = Vector3.zero;
                collider = capsuleCollider;
                
                if (_debugMode)
                {
                    Debug.Log($"PlayerHitboxManager: Created default collider for {hitboxObject.name}");
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
                        Debug.Log($"PlayerHitboxManager: Set {hitboxObject.name} collider to non-trigger");
                    }
                }
            }
            
            return collider;
        }

        /// <summary>
        /// Setup PlayerPhysicalHitbox component for Body GameObject
        /// </summary>
        private void SetupPlayerPhysicalHitbox(GameObject hitboxObject)
        {
            PlayerPhysicalHitbox existingHitbox = hitboxObject.GetComponent<PlayerPhysicalHitbox>();
            
            if (existingHitbox == null)
            {
                PlayerPhysicalHitbox newHitbox = hitboxObject.AddComponent<PlayerPhysicalHitbox>();
                
                // Set multipliers (typically 1.0 for player)
                newHitbox.physicalHealthMultiplier = 1f;
                
                // Initialize the hitbox with player reference
                newHitbox.Initialize(_playerMono);
                
                if (_debugMode)
                {
                    Debug.Log($"PlayerHitboxManager: Added PlayerPhysicalHitbox with multipliers - " +
                             $"Physical: x{newHitbox.physicalHealthMultiplier:F1}");
                }
            }
            else
            {
                // Update existing hitbox
                existingHitbox.physicalHealthMultiplier = 1f;
                
                existingHitbox.Initialize(_playerMono);
                
                if (_debugMode)
                {
                    Debug.Log($"PlayerHitboxManager: Updated PlayerPhysicalHitbox with multipliers - " +
                             $"Physical: x{existingHitbox.physicalHealthMultiplier:F1}");
                }
            }
        }

        /// <summary>
        /// Setup PlayerCrystalCoreHitbox component for Core GameObject
        /// </summary>
        private void SetupPlayerCrystalCoreHitbox(GameObject hitboxObject)
        {
            PlayerCrystalCoreHitbox existingHitbox = hitboxObject.GetComponent<PlayerCrystalCoreHitbox>();
            
            if (existingHitbox == null)
            {
                PlayerCrystalCoreHitbox newHitbox = hitboxObject.AddComponent<PlayerCrystalCoreHitbox>();
                
                // Set multipliers for core hitbox
                newHitbox.physicalHealthMultiplier = 0f; // Core doesn't take physical damage
                newHitbox.coreHealthMultiplier = 1f;
                
                // Initialize the hitbox with player reference
                newHitbox.Initialize(_playerMono);
                
                if (_debugMode)
                {
                    Debug.Log($"PlayerHitboxManager: Added PlayerCrystalCoreHitbox with multipliers - " +
                             $"Physical: x{newHitbox.physicalHealthMultiplier:F1}, " +
                             $"Core: x{newHitbox.coreHealthMultiplier:F1}");
                }
            }
            else
            {
                // Update existing hitbox
                existingHitbox.physicalHealthMultiplier = 0f;
                existingHitbox.coreHealthMultiplier = 1f;
                
                existingHitbox.Initialize(_playerMono);
                
                if (_debugMode)
                {
                    Debug.Log($"PlayerHitboxManager: Updated PlayerCrystalCoreHitbox with multipliers - " +
                             $"Physical: x{existingHitbox.physicalHealthMultiplier:F1}, " +
                             $"Core: x{existingHitbox.coreHealthMultiplier:F1}");
                }
            }
            
            // Store reference to crystal core hitbox
            _crystalCoreHitbox = existingHitbox ?? hitboxObject.GetComponent<PlayerCrystalCoreHitbox>();
            
            // Ensure the collider is always enabled for player crystal core
            Collider coreCollider = hitboxObject.GetComponent<Collider>();
            if (coreCollider != null)
            {
                coreCollider.enabled = true;
                
                if (_debugMode)
                {
                    Debug.Log($"PlayerHitboxManager: Ensured PlayerCrystalCoreHitbox collider is enabled");
                }
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Check if hitbox manager is properly initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Get the player's crystal core hitbox
        /// </summary>
        /// <returns>PlayerCrystalCoreHitbox or null if not found</returns>
        public PlayerCrystalCoreHitbox GetCrystalCoreHitbox()
        {
            return _crystalCoreHitbox;
        }

        #endregion
    }
}

