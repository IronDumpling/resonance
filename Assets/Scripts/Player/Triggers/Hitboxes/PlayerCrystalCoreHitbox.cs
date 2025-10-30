using UnityEngine;
using Resonance.Interfaces;
using Resonance.Utilities.Types;
using Resonance.Utilities.Waves;

namespace Resonance.Player.Triggers
{
    /// <summary>
    /// Crystal Core hitbox component for Player - handles both physical damage and wave attacks
    /// Implements IHitbox for damage system integration and IWavable for wave attack system
    /// This is the only hitbox type that can receive CoreHealth damage and participate in wave attacks
    /// </summary>
    public class PlayerCrystalCoreHitbox : MonoBehaviour, IHitbox, IWavable
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
        private PlayerMonoBehaviour _playerMono;
        private Core.PlayerController _playerController;
        private bool _isInitialized = false;
        private Collider _collider;

        #region Properties

        /// <summary>
        /// Check if this hitbox is properly initialized
        /// </summary>
        public bool IsInitialized => _isInitialized && _playerMono != null && _playerController != null;

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the crystal core hitbox with player reference
        /// </summary>
        /// <param name="playerMono">Player MonoBehaviour reference</param>
        public void Initialize(PlayerMonoBehaviour playerMono)
        {
            _playerMono = playerMono;
            _playerController = playerMono?.Controller;
            _collider = GetComponent<Collider>();
            _isInitialized = true;
            
            if (_debugMode)
            {
                Debug.Log($"PlayerCrystalCoreHitbox: Initialized Core hitbox on {gameObject.name}");
            }
        }

        #endregion

        #region IHitbox Implementation

        /// <summary>
        /// Process damage hit on this Core hitbox
        /// Applies Core-specific damage multipliers and forwards to player
        /// </summary>
        /// <param name="damageInfo">Original damage information</param>
        /// <returns>Modified damage info after hitbox multipliers applied</returns>
        public DamageInfo ProcessDamageHit(DamageInfo damageInfo)
        {
            if (!IsInitialized)
            {
                if (_debugMode)
                {
                    Debug.LogWarning($"PlayerCrystalCoreHitbox: Cannot process damage - not initialized");
                }
                return new DamageInfo(new Damages(), Vector3.zero);
            }

            if (_debugMode)
            {
                Debug.Log($"PlayerCrystalCoreHitbox: Processing damage - {damageInfo}");
            }

            // Modify damage based on Core hitbox multipliers
            DamageInfo modifiedDamage = ModifyDamage(damageInfo, damageInfo.sourcePosition);
            
            // Play hit effects
            PlayHitFX(damageInfo.sourcePosition);
            
            // Forward modified damage to player
            _playerMono.TakeDamage(modifiedDamage);
            
            if (_debugMode)
            {
                Debug.Log($"PlayerCrystalCoreHitbox: Forwarded modified damage - {modifiedDamage}");
            }
            
            return modifiedDamage;
        }

        #endregion

        #region IWavable Implementation

        /// <summary>
        /// Get the Wave object from PlayerCrystalCore
        /// </summary>
        public Wave GetWave()
        {
            return IsInitialized && _playerController.Stats.crystalCore != null 
                ? _playerController.Stats.crystalCore.Wave 
                : null;
        }

        /// <summary>
        /// Get the base damages for wave attacks
        /// For player, this comes from waveAttackDamages
        /// </summary>
        public Damages GetWaveBaseDamages()
        {
            if (IsInitialized && _playerController.Stats.waveAttackDamages != null)
            {
                return _playerController.Stats.waveAttackDamages;
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
                Debug.LogError($"PlayerCrystalCoreHitbox: Cannot apply wave damages - not initialized");
                return false;
            }

            if (damages == null)
            {
                Debug.LogError($"PlayerCrystalCoreHitbox: Cannot apply wave damages - damages is null");
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

            // Apply damage through the player's damage system
            _playerMono.TakeDamage(damageInfo);

            Debug.Log($"PlayerCrystalCoreHitbox: Applied wave damages to {name} - " +
                      $"CoreHealth: {damages.GetDamage(DamageType.CoreHealth):F1}, " +
                      $"Chaos: {damages.GetDamage(DamageType.Chaos):F1}");
            
            return true;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Get player MonoBehaviour reference
        /// </summary>
        /// <returns>Player MonoBehaviour or null if not initialized</returns>
        public PlayerMonoBehaviour GetPlayerMonoBehaviour()
        {
            return _playerMono;
        }

        /// <summary>
        /// Get player controller reference
        /// </summary>
        /// <returns>Player controller or null if not initialized</returns>
        public Core.PlayerController GetPlayerController()
        {
            return _playerController;
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
                   _playerController != null &&
                   _playerController.Stats != null &&
                   _playerController.Stats.crystalCore != null &&
                   _playerController.Stats.crystalCore.Wave != null;
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
                ? "Hitbox:PlayerCore"
                : $"{originalDamage.description}|Hitbox:PlayerCore";

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
                    Debug.Log($"PlayerCrystalCoreHitbox: Spawned hit VFX at {at}");
                }
            }
            
            if (hitSFX) 
            {
                AudioSource.PlayClipAtPoint(hitSFX, at);
                if (_debugMode)
                {
                    Debug.Log($"PlayerCrystalCoreHitbox: Played hit SFX at {at}");
                }
            }
        }

        #endregion
    }
}
