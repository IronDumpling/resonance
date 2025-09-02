using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Player.Core;
using Resonance.Enemies;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Trigger component that detects Core type EnemyHitbox components with enabled colliders within mental attack range.
    /// Should be attached to the MentalAttackRange GameObject under the Player.
    /// </summary>
    public class MentalAttackTrigger : MonoBehaviour
    {
        // Core references
        private PlayerController _playerController;
        private bool _isInitialized = false;

        // Core hitbox tracking
        private List<EnemyHitbox> _coreHitboxesInRange = new List<EnemyHitbox>();

        // Events
        public System.Action<EnemyHitbox> OnCoreHitboxEntered;
        public System.Action<EnemyHitbox> OnCoreHitboxExited;
        public System.Action OnCoreHitboxesChanged; // General event for any change in core hitboxes

        // Properties
        public bool HasCoreHitboxesInRange => _coreHitboxesInRange.Count > 0;
        public int CoreHitboxCount => _coreHitboxesInRange.Count;
        public List<EnemyHitbox> CoreHitboxesInRange => new List<EnemyHitbox>(_coreHitboxesInRange);

        #region Unity Lifecycle

        void Awake()
        {
            // Ensure we have a SphereCollider trigger
            var collider = GetComponent<SphereCollider>();
            if (collider == null)
            {
                Debug.LogError("MentalAttackTrigger: No SphereCollider found! Please add a SphereCollider component.");
                return;
            }

            if (!collider.isTrigger)
            {
                Debug.LogWarning("MentalAttackTrigger: SphereCollider is not set as trigger. Setting it now.");
                collider.isTrigger = true;
            }

            Debug.Log($"MentalAttackTrigger: Initialized with range {collider.radius}");
        }

        void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the trigger with player controller reference and set the range
        /// </summary>
        /// <param name="playerController">Reference to the player controller</param>
        /// <param name="range">Detection range (will set the SphereCollider radius)</param>
        public void Initialize(PlayerController playerController, float range)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("MentalAttackTrigger: Already initialized");
                return;
            }

            _playerController = playerController;

            // Set the collider radius
            var collider = GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius = range;
                Debug.Log($"MentalAttackTrigger: Set detection range to {range}");
            }

            _isInitialized = true;
            Debug.Log("MentalAttackTrigger: Initialized successfully");
        }

        #endregion

        #region Trigger Events

        void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized) return;

            // Check if it's a Core type EnemyHitbox
            var hitbox = other.GetComponent<EnemyHitbox>();
            if (hitbox == null) return;

            // Only track Core type hitboxes with enabled colliders
            if (hitbox.type == EnemyHitboxType.Core && other.enabled && hitbox.IsInitialized)
            {
                if (!_coreHitboxesInRange.Contains(hitbox))
                {
                    _coreHitboxesInRange.Add(hitbox);
                    OnCoreHitboxEntered?.Invoke(hitbox);
                    OnCoreHitboxesChanged?.Invoke();
                    Debug.Log($"MentalAttackTrigger: Core hitbox {hitbox.name} entered range");
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!_isInitialized) return;

            // Check if it's a Core type EnemyHitbox
            var hitbox = other.GetComponent<EnemyHitbox>();
            if (hitbox == null) return;

            // Remove from tracking list if present
            if (_coreHitboxesInRange.Contains(hitbox))
            {
                _coreHitboxesInRange.Remove(hitbox);
                OnCoreHitboxExited?.Invoke(hitbox);
                OnCoreHitboxesChanged?.Invoke();
                Debug.Log($"MentalAttackTrigger: Core hitbox {hitbox.name} exited range");
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (!_isInitialized) return;

            // Check if Core hitbox collider state changed
            var hitbox = other.GetComponent<EnemyHitbox>();
            if (hitbox == null || hitbox.type != EnemyHitboxType.Core || !hitbox.IsInitialized) return;

            bool isInList = _coreHitboxesInRange.Contains(hitbox);
            bool shouldBeInList = other.enabled;

            if (shouldBeInList && !isInList)
            {
                // Collider became enabled, add to list
                _coreHitboxesInRange.Add(hitbox);
                OnCoreHitboxEntered?.Invoke(hitbox);
                OnCoreHitboxesChanged?.Invoke();
                Debug.Log($"MentalAttackTrigger: Core hitbox {hitbox.name} collider enabled");
            }
            else if (!shouldBeInList && isInList)
            {
                // Collider became disabled, remove from list
                _coreHitboxesInRange.Remove(hitbox);
                OnCoreHitboxExited?.Invoke(hitbox);
                OnCoreHitboxesChanged?.Invoke();
                Debug.Log($"MentalAttackTrigger: Core hitbox {hitbox.name} collider disabled");
            }
        }

        #endregion

        #region Core Hitbox Validation

        /// <summary>
        /// Validate if a hitbox should be tracked based on current criteria
        /// </summary>
        /// <param name="hitbox">The hitbox to validate</param>
        /// <param name="collider">The collider component</param>
        /// <returns>True if hitbox should be tracked</returns>
        private bool IsValidCoreHitbox(EnemyHitbox hitbox, Collider collider)
        {
            return hitbox != null && 
                   hitbox.IsInitialized && 
                   hitbox.type == EnemyHitboxType.Core && 
                   collider != null && 
                   collider.enabled;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Manually refresh the state of all core hitboxes in range
        /// Useful for ensuring accuracy when collider states change externally
        /// </summary>
        public void RefreshCoreHitboxStates()
        {
            if (!_isInitialized) return;

            // Get all colliders in range and check their state
            var collider = GetComponent<SphereCollider>();
            if (collider == null) return;

            var hitboxesToCheck = new List<EnemyHitbox>(_coreHitboxesInRange);
            
            foreach (var hitbox in hitboxesToCheck)
            {
                if (hitbox != null)
                {
                    var hitboxCollider = hitbox.GetComponent<Collider>();
                    if (!IsValidCoreHitbox(hitbox, hitboxCollider))
                    {
                        // Remove invalid hitboxes
                        _coreHitboxesInRange.Remove(hitbox);
                        OnCoreHitboxExited?.Invoke(hitbox);
                        OnCoreHitboxesChanged?.Invoke();
                        Debug.Log($"MentalAttackTrigger: Removed invalid core hitbox {hitbox.name}");
                    }
                }
            }

            Debug.Log($"MentalAttackTrigger: Refreshed states for {hitboxesToCheck.Count} core hitboxes");
        }

        /// <summary>
        /// Get the closest core hitbox in range
        /// </summary>
        /// <returns>Closest core hitbox or null if none</returns>
        public EnemyHitbox GetClosestCoreHitbox()
        {
            if (_coreHitboxesInRange.Count == 0) return null;

            Vector3 playerPosition = transform.position;
            EnemyHitbox closest = null;
            float closestDistance = float.MaxValue;

            foreach (var hitbox in _coreHitboxesInRange)
            {
                if (hitbox != null)
                {
                    float distance = Vector3.Distance(playerPosition, hitbox.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = hitbox;
                    }
                }
            }

            return closest;
        }

        /// <summary>
        /// Check if a specific core hitbox is in range and has an enabled collider
        /// </summary>
        /// <param name="hitbox">The hitbox to check</param>
        /// <returns>True if core hitbox is in range and has enabled collider</returns>
        public bool IsCoreHitboxInRange(EnemyHitbox hitbox)
        {
            return hitbox != null && _coreHitboxesInRange.Contains(hitbox);
        }

        #endregion

        #region Debug and Utility

        /// <summary>
        /// Get debug information about current detection state
        /// </summary>
        /// <returns>Debug info string</returns>
        public string GetDebugInfo()
        {
            if (!_isInitialized) return "Not initialized";

            return $"Core hitboxes in range: {_coreHitboxesInRange.Count}";
        }

        /// <summary>
        /// Clean up resources and events
        /// </summary>
        private void Cleanup()
        {
            OnCoreHitboxEntered = null;
            OnCoreHitboxExited = null;
            OnCoreHitboxesChanged = null;

            _coreHitboxesInRange.Clear();

            _isInitialized = false;
            Debug.Log("MentalAttackTrigger: Cleaned up");
        }

        #endregion

        #region Gizmos (for debugging)

        void OnDrawGizmosSelected()
        {
            var collider = GetComponent<SphereCollider>();
            if (collider != null)
            {
                // Draw the detection range
                Gizmos.color = HasCoreHitboxesInRange ? Color.red : Color.yellow;
                Gizmos.DrawWireSphere(transform.position, collider.radius);
                // Draw connections to core hitboxes
                if (_coreHitboxesInRange != null)
                {
                    Gizmos.color = Color.red;
                    foreach (var hitbox in _coreHitboxesInRange)
                    {
                        if (hitbox != null)
                        {
                            Gizmos.DrawLine(transform.position, hitbox.transform.position);
                            Gizmos.DrawWireCube(hitbox.transform.position, Vector3.one * 0.5f);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
