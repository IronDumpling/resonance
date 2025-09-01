using UnityEngine;
using System.Collections.Generic;
using Resonance.Enemies.Core;
using Resonance.Interfaces;

namespace Resonance.Enemies
{
    /// <summary>
    /// Enemy damage hitbox component - handles collision detection and damage dealing
    /// Should be attached to the DamageHitbox GameObject (child of enemy)
    /// Only active during attack animation windows controlled by EnableHitbox/DisableHitbox
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnemyDamageHitbox : MonoBehaviour
    {
        [Header("Hitbox Configuration")]
        [SerializeField] private LayerMask _targetLayers = (1 << 3);
        [Tooltip("Which layers can be damaged by this hitbox")]
        
        [SerializeField] private string _targetTag = "Player";
        [Tooltip("Required tag for damage targets")]
        
        [SerializeField] private bool _debugMode = false;
        [Tooltip("Enable debug logging for hitbox events")]

        // References
        private EnemyMonoBehaviour _enemyMono;
        private EnemyController _enemyController;
        private Collider _hitboxCollider;
        private HashSet<IDamageable> _hitTargetsThisFrame = new HashSet<IDamageable>();

        // State
        private bool _isInitialized = false;

        #region Unity Lifecycle

        void Awake()
        {
            // Get collider component
            _hitboxCollider = GetComponent<Collider>();
            if (_hitboxCollider == null)
            {
                Debug.LogError($"EnemyDamageHitbox: No Collider found on {gameObject.name}!");
                return;
            }

            // Ensure it's a trigger
            if (!_hitboxCollider.isTrigger)
            {
                Debug.LogWarning($"EnemyDamageHitbox: Collider on {gameObject.name} is not a trigger! Setting to trigger.");
                _hitboxCollider.isTrigger = true;
            }
        }

        void Start()
        {
            if (!_isInitialized)
            {
                Debug.LogError($"EnemyDamageHitbox: Failed to initialize on {gameObject.name}");
                enabled = false;
                return;
            }

            // Start disabled - will be enabled by animation events
            gameObject.SetActive(false);
        }

        void OnEnable()
        {
            _hitTargetsThisFrame.Clear();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized || !_enemyController.IsHitboxEnabled)
            {
                return;
            }

            ProcessCollision(other);
        }

        void OnTriggerStay(Collider other)
        {
            if (!_isInitialized || !_enemyController.IsHitboxEnabled)
            {
                return;
            }

            ProcessCollision(other);
        }

        void LateUpdate()
        {
            _hitTargetsThisFrame.Clear();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the damage hitbox (called by EnemyMonoBehaviour, similar to EnemyDetectionTrigger)
        /// </summary>
        /// <param name="enemyMono">Enemy MonoBehaviour reference</param>
        public void Initialize(EnemyMonoBehaviour enemyMono)
        {
            _enemyMono = enemyMono;
            _enemyController = enemyMono.Controller;
            _isInitialized = true;
            
            if (_debugMode)
            {
                Debug.Log($"EnemyDamageHitbox: Initialized with enemy controller from {enemyMono.name}");
            }
        }

        /// <summary>
        /// Initialize with specific enemy controller (alternative method)
        /// </summary>
        public void Initialize(EnemyController enemyController)
        {
            _enemyController = enemyController;
            _isInitialized = true;
            
            if (_debugMode)
            {
                Debug.Log($"EnemyDamageHitbox: Manually initialized with enemy controller");
            }
        }

        #endregion

        #region Collision Processing

        /// <summary>
        /// Process collision with potential damage target
        /// </summary>
        private void ProcessCollision(Collider other)
        {
            // Check layer mask
            if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                if (_debugMode)
                {
                    Debug.Log($"EnemyDamageHitbox: Ignoring {other.name} - not in target layers");
                }
                return;
            }

            // Check tag
            if (!string.IsNullOrEmpty(_targetTag) && !other.CompareTag(_targetTag))
            {
                if (_debugMode)
                {
                    Debug.Log($"EnemyDamageHitbox: Ignoring {other.name} - wrong tag (expected: {_targetTag}, got: {other.tag})");
                }
                return;
            }

            // Find IDamageable component
            IDamageable damageable = FindDamageableComponent(other);
            if (damageable == null)
            {
                if (_debugMode)
                {
                    Debug.Log($"EnemyDamageHitbox: No IDamageable found on {other.name}");
                }
                return;
            }

            // Check if we've already hit this target this frame (prevent multiple hits per frame)
            if (_hitTargetsThisFrame.Contains(damageable))
            {
                return;
            }

            // Attempt to deal damage
            bool damageDealt = AttemptDamage(damageable, other);
            
            if (damageDealt)
            {
                _hitTargetsThisFrame.Add(damageable);
                
                if (_debugMode)
                {
                    Debug.Log($"EnemyDamageHitbox: Successfully damaged {other.name}");
                }
            }
        }

        /// <summary>
        /// Find IDamageable component on target or its parents/children
        /// </summary>
        private IDamageable FindDamageableComponent(Collider target)
        {
            // Try the collider's GameObject first
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null) return damageable;

            // Try parent hierarchy
            damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null) return damageable;

            // Try children (less common, but possible)
            damageable = target.GetComponentInChildren<IDamageable>();
            return damageable;
        }

        /// <summary>
        /// Attempt to deal damage to the target
        /// </summary>
        private bool AttemptDamage(IDamageable target, Collider targetCollider)
        {
            // Create damage info
            DamageInfo damageInfo = new DamageInfo(
                amount: _enemyController.AttackDamageValue,
                type: DamageType.Physical,
                sourcePosition: transform.position,
                sourceObject: gameObject,
                description: "Enemy melee attack"
            );

            // Use the controller's damage system (handles modifiers, cooldowns, etc.)
            return _enemyController.TryApplyDamage(target, damageInfo);
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Check if hitbox is currently active
        /// </summary>
        public bool IsActive => gameObject.activeInHierarchy;

        #endregion

        #region Debug

        void OnDrawGizmos()
        {
            if (!_debugMode || _hitboxCollider == null) return;

            // Draw hitbox bounds
            Gizmos.color = IsActive ? Color.red : Color.yellow;
            
            if (_hitboxCollider is SphereCollider sphere)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (_hitboxCollider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (_hitboxCollider is CapsuleCollider capsule)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                // Simplified capsule representation
                Gizmos.DrawWireSphere(capsule.center + Vector3.up * capsule.height * 0.5f, capsule.radius);
                Gizmos.DrawWireSphere(capsule.center - Vector3.up * capsule.height * 0.5f, capsule.radius);
            }
            
            Gizmos.matrix = Matrix4x4.identity;
        }

        #endregion
    }
}
