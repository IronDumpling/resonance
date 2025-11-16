using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Gameplay.Player.Core;
using Resonance.Gameplay.Enemies;
using Resonance.Gameplay.Enemies.Triggers;
using Resonance.Shared.Interfaces;
using Resonance.Utilities.Types;

namespace Resonance.Gameplay.Player.Triggers
{
    /// <summary>
    /// Trigger component that detects IWavable targets (EnemyCrystalCoreHitbox) with enabled colliders within wave attack range.
    /// Should be attached to the WaveAttackRange GameObject under the Player.
    /// </summary>
    public class WaveAttackTrigger : MonoBehaviour
    {
        // Core references
        private PlayerController _playerController;
        private bool _isInitialized = false;

        // Layer mask for filtering
        private LayerMask _waveInteractionLayerMask = LayerDict.GetLayer("Enemy");

        // IWavable target tracking (EnemyCrystalCoreHitbox components)
        private List<IWavable> _coreHitboxesInRange = new List<IWavable>();
        private IWavable _lastClosestCore = null;

        // Events
        public System.Action<IWavable> OnWavableEntered;
        public System.Action<IWavable> OnWavableExited;
        public System.Action OnWavablesChanged; // General event for any change in core hitboxes

        // Properties
        public bool HasWavablesInRange => _coreHitboxesInRange.Count > 0;
        public int WavableCount => _coreHitboxesInRange.Count;
        public List<IWavable> WavablesInRange => new List<IWavable>(_coreHitboxesInRange);

        #region Unity Lifecycle

        void Awake()
        {
            // Ensure we have a SphereCollider trigger
            var collider = GetComponent<SphereCollider>();
            if (collider == null)
            {
                Debug.LogError("WaveAttackTrigger: No SphereCollider found! Please add a SphereCollider component.");
                return;
            }

            if (!collider.isTrigger)
            {
                Debug.LogWarning("WaveAttackTrigger: SphereCollider is not set as trigger. Setting it now.");
                collider.isTrigger = true;
            }

            Debug.Log($"WaveAttackTrigger: Initialized with range {collider.radius}");
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
        /// <param name="layerMask">Layer mask for filtering collisions (optional, defaults to layer 8)</param>
        public void Initialize(PlayerController playerController, float range, LayerMask? layerMask = null)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("WaveAttackTrigger: Already initialized");
                return;
            }

            _playerController = playerController;
            
            // Set layer mask if provided
            if (layerMask.HasValue)
            {
                _waveInteractionLayerMask = layerMask.Value;
                Debug.Log($"WaveAttackTrigger: Set layer mask to {_waveInteractionLayerMask.value}");
            }

            // Set the collider radius
            var collider = GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius = range;
                Debug.Log($"WaveAttackTrigger: Set detection range to {range}");
            }

            _isInitialized = true;
            Debug.Log("WaveAttackTrigger: Initialized successfully");
        }

        #endregion

        #region Trigger Events

        void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized) return;

            // Check layer mask filter
            if ((_waveInteractionLayerMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return; // Not on the correct layer
            }

            // Check if it's an IWavable (EnemyCrystalCoreHitbox)
            var wavable = other.GetComponent<IWavable>();
            if (wavable == null) return;
            
            // Must be EnemyCrystalCoreHitbox
            var coreHitbox = wavable as EnemyCrystalCoreHitbox;
            if (coreHitbox == null) return;

            // Only track IWavable targets with enabled colliders and initialized state
            if (other.enabled && coreHitbox.IsInitialized)
            {
                if (!_coreHitboxesInRange.Contains(wavable))
                {
                    _coreHitboxesInRange.Add(wavable);
                    OnWavableEntered?.Invoke(wavable);
                    OnWavablesChanged?.Invoke();
                    UpdateClosestCoreNotification();
                    Debug.Log($"WaveAttackTrigger: IWavable target {coreHitbox.name} entered range");
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!_isInitialized) return;

            // Check layer mask filter
            if ((_waveInteractionLayerMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return; // Not on the correct layer
            }

            // Check if it's an IWavable (EnemyCrystalCoreHitbox)
            var wavable = other.GetComponent<IWavable>();
            if (wavable == null) return;
            
            var coreHitbox = wavable as EnemyCrystalCoreHitbox;
            if (coreHitbox == null) return;

            // Remove from tracking list if present
            if (_coreHitboxesInRange.Contains(wavable))
            {
                _coreHitboxesInRange.Remove(wavable);
                OnWavableExited?.Invoke(wavable);
                OnWavablesChanged?.Invoke();
                UpdateClosestCoreNotification();
                Debug.Log($"WaveAttackTrigger: IWavable target {coreHitbox.name} exited range");
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (!_isInitialized) return;

            // Check layer mask filter
            if ((_waveInteractionLayerMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return; // Not on the correct layer
            }

            // Check if IWavable target collider state changed
            var wavable = other.GetComponent<IWavable>();
            if (wavable == null) return;
            
            var coreHitbox = wavable as EnemyCrystalCoreHitbox;
            if (coreHitbox == null || !coreHitbox.IsInitialized) return;

            bool isInList = _coreHitboxesInRange.Contains(wavable);
            bool shouldBeInList = other.enabled;

            if (shouldBeInList && !isInList)
            {
                // Collider became enabled, add to list
                _coreHitboxesInRange.Add(wavable);
                OnWavableEntered?.Invoke(wavable);
                OnWavablesChanged?.Invoke();
                UpdateClosestCoreNotification();
                Debug.Log($"WaveAttackTrigger: IWavable target {coreHitbox.name} collider enabled");
            }
            else if (!shouldBeInList && isInList)
            {
                // Collider became disabled, remove from list
                _coreHitboxesInRange.Remove(wavable);
                OnWavableExited?.Invoke(wavable);
                OnWavablesChanged?.Invoke();
                UpdateClosestCoreNotification();
                Debug.Log($"WaveAttackTrigger: IWavable target {coreHitbox.name} collider disabled");
            }
        }

        #endregion

        #region Core Hitbox Validation

        /// <summary>
        /// Validate if an IWavable target should be tracked based on current criteria
        /// </summary>
        /// <param name="wavable">The IWavable to validate</param>
        /// <param name="collider">The collider component</param>
        /// <returns>True if IWavable should be tracked</returns>
        private bool IsValidWavableTarget(IWavable wavable, Collider collider)
        {
            if (wavable == null || collider == null || !collider.enabled)
                return false;
                
            var coreHitbox = wavable as EnemyCrystalCoreHitbox;
            return coreHitbox != null && coreHitbox.IsInitialized;
        }

        #endregion

        #region Closest Core Notification

        /// <summary>
        /// Update closest IWavable target notification when targets list changes
        /// </summary>
        private void UpdateClosestCoreNotification()
        {
            var currentClosest = GetClosestWavable();
            
            if (currentClosest != _lastClosestCore)
            {
                // Notify old closest target to change to white
                if (_lastClosestCore != null)
                {
                    var oldEnemyMono = GetEnemyMonoFromWavable(_lastClosestCore);
                    oldEnemyMono?.SetWaveUIColor(Color.white);
                    
                    if (_lastClosestCore is MonoBehaviour oldMono)
                    {
                        Debug.Log($"WaveAttackTrigger: {oldMono.name} is no longer closest target, set to white");
                    }
                }
                
                // Notify new closest target to change to red
                if (currentClosest != null)
                {
                    var newEnemyMono = GetEnemyMonoFromWavable(currentClosest);
                    newEnemyMono?.SetWaveUIColor(Color.red);
                    
                    if (currentClosest is MonoBehaviour newMono)
                    {
                        Debug.Log($"WaveAttackTrigger: {newMono.name} is now closest target, set to red");
                    }
                }
                
                _lastClosestCore = currentClosest;
            }
        }
        
        /// <summary>
        /// Force refresh all UI colors - useful after wave actions end
        /// Also cleans up invalid IWavable targets from tracking list
        /// </summary>
        public void ForceRefreshUIColors()
        {
            // First, validate and clean up the tracking list
            var targetsToRemove = new List<IWavable>();
            
            foreach (var wavable in _coreHitboxesInRange)
            {
                if (wavable != null && wavable is MonoBehaviour mono)
                {
                    var collider = mono.GetComponent<Collider>();
                    
                    // Check if wavable is still valid for tracking
                    if (!IsValidWavableTarget(wavable, collider))
                    {
                        targetsToRemove.Add(wavable);
                        Debug.Log($"WaveAttackTrigger: Removing invalid IWavable target {mono.name} from tracking list");
                    }
                    else
                    {
                        // Reset UI color for valid targets
                        var enemyMono = GetEnemyMonoFromWavable(wavable);
                        enemyMono?.SetWaveUIColor(Color.white);
                    }
                }
                else
                {
                    targetsToRemove.Add(wavable);
                }
            }
            
            // Remove invalid targets
            foreach (var wavable in targetsToRemove)
            {
                _coreHitboxesInRange.Remove(wavable);
                OnWavableExited?.Invoke(wavable);
            }
            
            // Trigger change event if we removed any targets
            if (targetsToRemove.Count > 0)
            {
                OnWavablesChanged?.Invoke();
            }
            
            // Clear last closest and force update
            _lastClosestCore = null;
            UpdateClosestCoreNotification();
            
            Debug.Log($"WaveAttackTrigger: Force refreshed all UI colors, removed {targetsToRemove.Count} invalid targets");
        }

        /// <summary>
        /// Get EnemyMonoBehaviour from IWavable (EnemyCrystalCoreHitbox) by traversing up the hierarchy
        /// </summary>
        /// <param name="wavable">The IWavable to find the parent EnemyMonoBehaviour for</param>
        /// <returns>EnemyMonoBehaviour if found, null otherwise</returns>
        private EnemyMonoBehaviour GetEnemyMonoFromWavable(IWavable wavable)
        {
            if (wavable == null) return null;
            
            // Try to get from EnemyCrystalCoreHitbox directly
            if (wavable is EnemyCrystalCoreHitbox coreHitbox)
            {
                var enemyMono = coreHitbox.GetEnemyMonoBehaviour();
                if (enemyMono != null)
                {
                    return enemyMono;
                }
            }

            // Fallback: traverse up hierarchy
            if (wavable is MonoBehaviour mono)
            {
                Transform current = mono.transform;
                
                while (current != null)
                {
                    var enemyMono = current.GetComponentInParent<EnemyMonoBehaviour>();
                    if (enemyMono != null)
                    {
                        return enemyMono;
                    }
                    current = current.parent;
                }
                
                Debug.LogWarning($"WaveAttackTrigger: Could not find EnemyMonoBehaviour for IWavable {mono.name}");
            }
            
            return null;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Manually refresh the state of all IWavable targets in range
        /// Useful for ensuring accuracy when collider states change externally
        /// </summary>
        public void RefreshWavableStates()
        {
            if (!_isInitialized) return;

            // Get all colliders in range and check their state
            var collider = GetComponent<SphereCollider>();
            if (collider == null) return;

            var targetsToCheck = new List<IWavable>(_coreHitboxesInRange);
            
            foreach (var wavable in targetsToCheck)
            {
                if (wavable != null && wavable is MonoBehaviour mono)
                {
                    var wavableCollider = mono.GetComponent<Collider>();
                    if (!IsValidWavableTarget(wavable, wavableCollider))
                    {
                        // Remove invalid targets
                        _coreHitboxesInRange.Remove(wavable);
                        OnWavableExited?.Invoke(wavable);
                        OnWavablesChanged?.Invoke();
                        Debug.Log($"WaveAttackTrigger: Removed invalid IWavable target {mono.name}");
                    }
                }
            }

            Debug.Log($"WaveAttackTrigger: Refreshed states for {targetsToCheck.Count} IWavable targets");
        }

        /// <summary>
        /// Get the closest IWavable target in range
        /// </summary>
        /// <returns>Closest IWavable target or null if none</returns>
        public IWavable GetClosestWavable()
        {
            if (_coreHitboxesInRange.Count == 0) return null;

            Vector3 playerPosition = transform.position;
            IWavable closest = null;
            float closestDistance = float.MaxValue;

            foreach (var wavable in _coreHitboxesInRange)
            {
                if (wavable != null && wavable is MonoBehaviour mono)
                {
                    float distance = Vector3.Distance(playerPosition, mono.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = wavable;
                    }
                }
            }

            return closest;
        }

        /// <summary>
        /// Check if a specific IWavable target is in range and has an enabled collider
        /// </summary>
        /// <param name="wavable">The IWavable to check</param>
        /// <returns>True if IWavable is in range and has enabled collider</returns>
        public bool IsWavableInRange(IWavable wavable)
        {
            return wavable != null && _coreHitboxesInRange.Contains(wavable);
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
            // Reset closest core tracking before clearing
            if (_lastClosestCore != null && _lastClosestCore is EnemyCrystalCoreHitbox coreHitbox)
            {
                var enemyMono = coreHitbox.GetEnemyMonoBehaviour();
                enemyMono?.SetWaveUIColor(Color.white);
            }
            
            OnWavableEntered = null;
            OnWavableExited = null;
            OnWavablesChanged = null;

            _coreHitboxesInRange.Clear();
            _lastClosestCore = null;

            _isInitialized = false;
            Debug.Log("WaveAttackTrigger: Cleaned up");
        }

        #endregion

        #region Gizmos (for debugging)

        void OnDrawGizmosSelected()
        {
            var collider = GetComponent<SphereCollider>();
            if (collider != null)
            {
                // Draw the detection range
                Gizmos.color = HasWavablesInRange ? Color.red : Color.yellow;
                Gizmos.DrawWireSphere(transform.position, collider.radius);
                // Draw connections to IWavable targets
                if (_coreHitboxesInRange != null)
                {
                    Gizmos.color = Color.red;
                    foreach (var wavable in _coreHitboxesInRange)
                    {
                        if (wavable != null && wavable is MonoBehaviour mono)
                        {
                            Gizmos.DrawLine(transform.position, mono.transform.position);
                            Gizmos.DrawWireCube(mono.transform.position, Vector3.one * 0.5f);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
