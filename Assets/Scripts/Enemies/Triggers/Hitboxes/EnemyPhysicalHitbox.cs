using UnityEngine;
using System.Collections.Generic;
using Resonance.Interfaces;
using Resonance.Utilities.Types;
using Resonance.Utilities.Waves;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Enemies.Triggers
{
    /// <summary>
    /// Physical hitbox component for Head, Body, Knee - acts as a damage modifier
    /// Modifies damage when hit by shooting system,
    /// then forwards the modified damage to the enemy's main damage handler
    /// Note: This class should NOT be used for Core hitboxes (use EnemyCrystalCoreHitbox instead)
    /// </summary>
    public class EnemyPhysicalHitbox : MonoBehaviour, IHitbox
    {
        [Header("Hitbox Type")]
        [Tooltip("Type of hitbox (Head, Body, Knee only - NOT Core)")]
        public HitboxType type;
        
        [Header("Damage Multipliers")]
        [Tooltip("Physical health damage multiplier")]
        public float physicalHealthMultiplier = 1f;
        
        [Tooltip("Chaos damage multiplier")]
        public float chaosMultiplier = 1f;
        
        [Header("Effects")]
        public GameObject hitVFX; 
        public AudioClip hitSFX;
        
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        
        // References
        private EnemyMonoBehaviour _enemyMono;
        private bool _isInitialized = false;
        private Collider _collider;
        private bool _lastColliderEnabled = false;
        
        // Events for collider state changes
        public System.Action<EnemyPhysicalHitbox> OnColliderEnabled;
        public System.Action<EnemyPhysicalHitbox> OnColliderDisabled;

        #region Unity Lifecycle

        void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        void OnEnable()
        {
            if (_isInitialized && _collider != null && _collider.enabled)
            {
                OnColliderEnabled?.Invoke(this);
                if (_debugMode)
                {
                    Debug.Log($"EnemyPhysicalHitbox: {type} collider enabled on {gameObject.name}");
                }
            }
        }

        void OnDisable()
        {
            if (_isInitialized)
            {
                OnColliderDisabled?.Invoke(this);
                if (_debugMode)
                {
                    Debug.Log($"EnemyPhysicalHitbox: {type} collider disabled on {gameObject.name}");
                }
            }
        }

        void Update()
        {
            // Monitor collider enabled state changes
            if (_isInitialized && _collider != null)
            {
                bool currentEnabled = _collider.enabled;
                if (currentEnabled != _lastColliderEnabled)
                {
                    if (currentEnabled)
                    {
                        OnColliderEnabled?.Invoke(this);
                        if (_debugMode)
                        {
                            Debug.Log($"EnemyPhysicalHitbox: {type} collider enabled on {gameObject.name}");
                        }
                    }
                    else
                    {
                        OnColliderDisabled?.Invoke(this);
                        if (_debugMode)
                        {
                            Debug.Log($"EnemyPhysicalHitbox: {type} collider disabled on {gameObject.name}");
                        }
                    }
                    _lastColliderEnabled = currentEnabled;
                }
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the weakpoint hitbox with enemy reference
        /// </summary>
        /// <param name="enemyMono">Enemy MonoBehaviour reference</param>
        public void Initialize(EnemyMonoBehaviour enemyMono)
        {
            _enemyMono = enemyMono;
            _collider = GetComponent<Collider>();
            _isInitialized = true;
            
            // Initialize collider state tracking
            _lastColliderEnabled = _collider != null && _collider.enabled;
            
            if (_debugMode)
            {
                Debug.Log($"EnemyPhysicalHitbox: Initialized {type} weakpoint on {gameObject.name}");
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Process damage hit on this hitbox and apply to enemy
        /// Called by ShootingSystem when this collider is hit
        /// </summary>
        /// <param name="damageInfo">Original damage information</param>
        /// <returns>Modified damage info after hitbox multipliers applied</returns>
        public DamageInfo ProcessDamageHit(DamageInfo damageInfo)
        {
            if (!_isInitialized || _enemyMono == null)
            {
                if (_debugMode)
                {
                    Debug.LogWarning($"EnemyPhysicalHitbox: Cannot process damage - not initialized or no enemy reference");
                }
                return new DamageInfo(new List<KeyValuePair<DamageType, float>>(), Vector3.zero);
            }

            if (_debugMode)
            {
                Debug.Log($"EnemyPhysicalHitbox ({type}): Processing damage - {damageInfo}");
            }

            // Modify damage based on hitbox multipliers
            DamageInfo modifiedDamage = ModifyDamage(damageInfo, damageInfo.sourcePosition);
            
            // Play hit effects
            PlayHitFX(damageInfo.sourcePosition);
            
            // Apply modified damage to enemy
            _enemyMono.TakeDamage(modifiedDamage);
            
            if (_debugMode)
            {
                Debug.Log($"EnemyPhysicalHitbox ({type}): Applied modified damage - {modifiedDamage}");
            }
            
            return modifiedDamage;
        }

        /// <summary>
        /// Check if this weakpoint is properly initialized
        /// </summary>
        public bool IsInitialized => _isInitialized && _enemyMono != null;

        /// <summary>
        /// Get enemy's runtime stats
        /// </summary>
        /// <returns>Enemy runtime stats or null if not initialized</returns>
        public Enemies.Data.EnemyRuntimeStats GetEnemyStats()
        {
            if (!_isInitialized || _enemyMono == null) return null;
            return _enemyMono.Controller?.Stats;
        }

        /// <summary>
        /// Get enemy MonoBehaviour reference for damage application
        /// </summary>
        /// <returns>Enemy MonoBehaviour or null if not initialized</returns>
        public EnemyMonoBehaviour GetEnemyMonoBehaviour()
        {
            return _enemyMono;
        }

        /// <summary>
        /// Get enemy controller reference for advanced operations
        /// </summary>
        /// <returns>Enemy controller or null if not initialized</returns>
        public Core.EnemyController GetEnemyController()
        {
            if (!_isInitialized || _enemyMono == null) return null;
            return _enemyMono.Controller;
        }

        #endregion

        #region Damage Modification

        /// <summary>
        /// Modify damage based on hitbox multipliers
        /// Applies specific multipliers for PhysicalHealth and Chaos damage types
        /// Note: CoreHealth damage is blocked (multiplier = 0) as this is a physical hitbox
        /// </summary>
        /// <param name="originalDamage">Original damage info</param>
        /// <param name="hitPoint">Hit point position</param>
        /// <returns>Modified damage info</returns>
        private DamageInfo ModifyDamage(DamageInfo originalDamage, Vector3 hitPoint)
        {
            if (originalDamage.damages == null || originalDamage.damages.GetCount() == 0)
            {
                return originalDamage;
            }

            // Create new damage dictionary with modified values
            Damages modifiedDamages = new Damages();

            // Apply physical health multiplier
            if (originalDamage.damages.HasDamage(DamageType.PhysicalHealth))
            {
                float originalAmount = originalDamage.damages.GetDamage(DamageType.PhysicalHealth);
                float modifiedAmount = originalAmount * physicalHealthMultiplier;
                if (modifiedAmount > 0f) modifiedDamages.SetDamage(DamageType.PhysicalHealth, modifiedAmount);
            }
            
            // Apply chaos multiplier
            if (originalDamage.damages.HasDamage(DamageType.Chaos))
            {
                float originalAmount = originalDamage.damages.GetDamage(DamageType.Chaos);
                float modifiedAmount = originalAmount * chaosMultiplier;
                if (modifiedAmount > 0f) modifiedDamages.SetDamage(DamageType.Chaos, modifiedAmount);
            }

            // Create modified damage info
            string newDescription = string.IsNullOrEmpty(originalDamage.description)
                ? $"Hitbox:{type}"
                : $"{originalDamage.description}|Hitbox:{type}";

            return new DamageInfo(
                modifiedDamages,
                hitPoint,
                originalDamage.sourceObject,
                newDescription
            );
        }

        /// <summary>
        /// Get multiplier for specific damage type
        /// Physical hitboxes only support PhysicalHealth and Chaos damage
        /// </summary>
        private float GetMultiplier(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.PhysicalHealth:
                    return physicalHealthMultiplier;
                case DamageType.CoreHealth:
                    return 0f; // Physical hitboxes do not transmit CoreHealth damage
                case DamageType.Chaos:
                    return chaosMultiplier;
                default:
                    return 1f;
            }
        }

        #endregion

        #region Visual and Audio Effects

        /// <summary>
        /// Play hit visual and audio effects
        /// </summary>
        /// <param name="at">Effect position</param>
        private void PlayHitFX(Vector3 at)
        {
            if (hitVFX) 
            {
                Instantiate(hitVFX, at, Quaternion.identity);
                if (_debugMode)
                {
                    Debug.Log($"EnemyPhysicalHitbox: Spawned hit VFX at {at}");
                }
            }
            
            if (hitSFX) 
            {
                AudioSource.PlayClipAtPoint(hitSFX, at);
                if (_debugMode)
                {
                    Debug.Log($"EnemyPhysicalHitbox: Played hit SFX at {at}");
                }
            }
        }

        #endregion
    }
}