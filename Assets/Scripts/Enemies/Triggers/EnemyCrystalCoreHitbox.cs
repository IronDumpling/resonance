using UnityEngine;
using Resonance.Interfaces;
using Resonance.Utilities.Types;
using Resonance.Utilities.Waves;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Enemies.Triggers
{
    /// <summary>
    /// Crystal Core hitbox component - handles both shooting damage and wave attacks
    /// Implements IHitbox for shooting system integration and IWavable for wave attack system
    /// This is the only hitbox type that can receive CoreHealth damage and participate in wave attacks
    /// </summary>
    public class EnemyCrystalCoreHitbox : MonoBehaviour, IHitbox, IWavable
    {
        [Header("Damage Multipliers")]
        [Tooltip("Physical health damage multiplier")]
        public float physicalHealthMultiplier = 0f;
        
        [Tooltip("Core health damage multiplier")]
        public float coreHealthMultiplier = 1f;
        
        [Tooltip("Chaos damage multiplier")]
        public float chaosMultiplier = 1f;
        
        [Header("Effects")]
        public GameObject hitVFX; 
        public AudioClip hitSFX;
        
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        
        // References
        private EnemyMonoBehaviour _enemyMono;
        private Core.EnemyController _enemyController;
        private bool _isInitialized = false;
        private Collider _collider;
        private bool _lastColliderEnabled = false;
        
        // Events for collider state changes
        public System.Action<EnemyCrystalCoreHitbox> OnColliderEnabled;
        public System.Action<EnemyCrystalCoreHitbox> OnColliderDisabled;

        #region Properties

        /// <summary>
        /// Check if this hitbox is properly initialized
        /// </summary>
        public bool IsInitialized => _isInitialized && _enemyMono != null && _enemyController != null;

        #endregion

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
                    Debug.Log($"EnemyCrystalCoreHitbox: Core collider enabled on {gameObject.name}");
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
                    Debug.Log($"EnemyCrystalCoreHitbox: Core collider disabled on {gameObject.name}");
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
                            Debug.Log($"EnemyCrystalCoreHitbox: Core collider enabled on {gameObject.name}");
                        }
                    }
                    else
                    {
                        OnColliderDisabled?.Invoke(this);
                        if (_debugMode)
                        {
                            Debug.Log($"EnemyCrystalCoreHitbox: Core collider disabled on {gameObject.name}");
                        }
                    }
                    _lastColliderEnabled = currentEnabled;
                }
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the crystal core hitbox with enemy reference
        /// </summary>
        /// <param name="enemyMono">Enemy MonoBehaviour reference</param>
        public void Initialize(EnemyMonoBehaviour enemyMono)
        {
            _enemyMono = enemyMono;
            _enemyController = enemyMono?.Controller;
            _collider = GetComponent<Collider>();
            _isInitialized = true;
            
            // Initialize collider state tracking
            _lastColliderEnabled = _collider != null && _collider.enabled;
            
            if (_debugMode)
            {
                Debug.Log($"EnemyCrystalCoreHitbox: Initialized Core hitbox on {gameObject.name}");
            }
        }

        #endregion

        #region IHitbox Implementation

        /// <summary>
        /// Process damage hit on this Core hitbox from shooting system
        /// Applies Core-specific damage multipliers and forwards to enemy
        /// </summary>
        /// <param name="damageInfo">Original damage information</param>
        /// <returns>Modified damage info after hitbox multipliers applied</returns>
        public DamageInfo ProcessDamageHit(DamageInfo damageInfo)
        {
            if (!IsInitialized)
            {
                if (_debugMode)
                {
                    Debug.LogWarning($"EnemyCrystalCoreHitbox: Cannot process damage - not initialized");
                }
                return new DamageInfo(new Damages(), Vector3.zero);
            }

            if (_debugMode)
            {
                Debug.Log($"EnemyCrystalCoreHitbox: Processing damage - {damageInfo}");
            }

            // Modify damage based on Core hitbox multipliers
            DamageInfo modifiedDamage = ModifyDamage(damageInfo, damageInfo.sourcePosition);
            
            // Play hit effects
            PlayHitFX(damageInfo.sourcePosition);
            
            // Apply modified damage to enemy
            _enemyMono.TakeDamage(modifiedDamage);
            
            if (_debugMode)
            {
                Debug.Log($"EnemyCrystalCoreHitbox: Applied modified damage - {modifiedDamage}");
            }
            
            return modifiedDamage;
        }

        #endregion

        #region IWavable Implementation

        /// <summary>
        /// Get the Wave object from EnemyCrystalCore
        /// </summary>
        public Wave GetWave()
        {
            return IsInitialized && _enemyController.Stats.crystalCore != null 
                ? _enemyController.Stats.crystalCore.Wave 
                : null;
        }

        /// <summary>
        /// Get the base damage value for wave attacks
        /// </summary>
        public Damages GetWaveBaseDamages()
        {
            if (IsInitialized && _enemyController.Stats.waveAttackStats.damages != null)
            {
                return _enemyController.Stats.waveAttackStats.damages;
            }
            return new Damages();
        }

        /// <summary>
        /// Apply wave damages from a source wavable
        /// </summary>
        /// <param name="damages">Damages to apply</param>
        /// <param name="sourceWavable">The source of the wave attack</param>
        /// <param name="description">Description of the damage source</param>
        /// <returns>True if damage was successfully applied</returns>
        public bool ApplyWaveDamages(Damages damages, IWavable sourceWavable, string description = "Wave Damage")
        {
            if (!IsInitialized)
            {
                Debug.LogError($"EnemyCrystalCoreHitbox: Cannot apply wave damages - not initialized");
                return false;
            }

            if (damages == null)
            {
                Debug.LogError($"EnemyCrystalCoreHitbox: Cannot apply wave damages - damages is null");
                return false;
            }

            // Get source information
            Vector3 sourcePosition = Vector3.zero;
            GameObject sourceObject = null;

            if (sourceWavable != null)
            {
                if (sourceWavable is MonoBehaviour sourceMono)
                {
                    sourcePosition = sourceMono.transform.position;
                    sourceObject = sourceMono.gameObject;
                }
            }

            // Create damage info
            DamageInfo damageInfo = new DamageInfo(
                damages: damages,
                sourcePosition: sourcePosition,
                sourceObject: sourceObject,
                description: description
            );

            // Apply damage through the enemy's damage system
            _enemyMono.TakeDamage(damageInfo);

            Debug.Log($"EnemyCrystalCoreHitbox: Applied wave damages to {name} - " +
                      $"CoreHealth: {damages.GetDamage(DamageType.CoreHealth):F1}, " +
                      $"Chaos: {damages.GetDamage(DamageType.Chaos):F1}");
            
            return true;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Get enemy's runtime stats
        /// </summary>
        /// <returns>Enemy runtime stats or null if not initialized</returns>
        public Enemies.Data.EnemyRuntimeStats GetEnemyStats()
        {
            if (!IsInitialized) return null;
            return _enemyController?.Stats;
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
            return _enemyController;
        }

        /// <summary>
        /// Check if this hitbox is valid for wave attack operations
        /// </summary>
        /// <returns>True if wave attack can be performed on this hitbox</returns>
        public bool IsValidForWaveAttack()
        {
            return IsInitialized && 
                   _collider != null && 
                   _collider.enabled &&
                   _enemyController != null &&
                   _enemyController.Stats != null &&
                   _enemyController.Stats.crystalCore != null &&
                   _enemyController.Stats.crystalCore.Wave != null;
        }

        #endregion

        #region Damage Modification

        /// <summary>
        /// Modify damage based on Core hitbox multipliers
        /// Core hitboxes primarily take CoreHealth damage
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

            // Apply physical health multiplier (usually 0 for Core)
            if (originalDamage.damages.HasDamage(DamageType.PhysicalHealth))
            {
                float originalAmount = originalDamage.damages.GetDamage(DamageType.PhysicalHealth);
                float modifiedAmount = originalAmount * physicalHealthMultiplier;
                if (modifiedAmount > 0f) modifiedDamages.SetDamage(DamageType.PhysicalHealth, modifiedAmount);
            }
            
            // Apply core health multiplier (usually 1 for Core)
            if (originalDamage.damages.HasDamage(DamageType.CoreHealth))
            {
                float originalAmount = originalDamage.damages.GetDamage(DamageType.CoreHealth);
                float modifiedAmount = originalAmount * coreHealthMultiplier;
                if (modifiedAmount > 0f) modifiedDamages.SetDamage(DamageType.CoreHealth, modifiedAmount);
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
                ? "Hitbox:Core"
                : $"{originalDamage.description}|Hitbox:Core";

            return new DamageInfo(
                modifiedDamages,
                hitPoint,
                originalDamage.sourceObject,
                newDescription
            );
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
                    Debug.Log($"EnemyCrystalCoreHitbox: Spawned hit VFX at {at}");
                }
            }
            
            if (hitSFX) 
            {
                AudioSource.PlayClipAtPoint(hitSFX, at);
                if (_debugMode)
                {
                    Debug.Log($"EnemyCrystalCoreHitbox: Played hit SFX at {at}");
                }
            }
        }

        #endregion
    }
}