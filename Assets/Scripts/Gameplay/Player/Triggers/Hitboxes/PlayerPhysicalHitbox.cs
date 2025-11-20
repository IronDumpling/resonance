using UnityEngine;
using Resonance.Shared.Interfaces;
using Resonance.Shared.Types;

namespace Resonance.Gameplay.Player.Triggers
{
    /// <summary>
    /// Physical hitbox component for Player Body - acts as a damage receiver
    /// Receives damage from enemy attacks and forwards to player's damage handler
    /// </summary>
    public class PlayerPhysicalHitbox : MonoBehaviour, IHitbox
    {
        [Header("Damage Multipliers")]
        [Tooltip("Physical health damage multiplier")]
        public float physicalHealthMultiplier = 1f;
        
        [Header("Effects")]
        public GameObject hitVFX; 
        public AudioClip hitSFX;
        
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        
        // References
        private PlayerMonoBehaviour _playerMono;
        private bool _isInitialized = false;
        private Collider _collider;

        #region Unity Lifecycle

        void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the hitbox with player reference
        /// </summary>
        /// <param name="playerMono">Player MonoBehaviour reference</param>
        public void Initialize(PlayerMonoBehaviour playerMono)
        {
            _playerMono = playerMono;
            _collider = GetComponent<Collider>();
            _isInitialized = true;
            
            if (_debugMode)
            {
                Debug.Log($"PlayerPhysicalHitbox: Initialized on {gameObject.name}");
            }
        }

        #endregion

        #region IHitbox Implementation

        /// <summary>
        /// Process damage hit on this hitbox and forward to player
        /// Called by enemy attack systems when this collider is hit
        /// </summary>
        /// <param name="damageInfo">Damage information</param>
        /// <returns>Modified damage info after hitbox multipliers applied</returns>
        public DamageInfo ProcessDamageHit(DamageInfo damageInfo)
        {
            if (!_isInitialized || _playerMono == null)
            {
                if (_debugMode)
                {
                    Debug.LogWarning($"PlayerPhysicalHitbox: Cannot process damage - not initialized or no player reference");
                }
                return new DamageInfo(new Damages(), Vector3.zero);
            }

            if (_debugMode)
            {
                Debug.Log($"PlayerPhysicalHitbox: Processing damage - {damageInfo}");
            }

            // Modify damage based on hitbox multipliers
            DamageInfo modifiedDamage = ModifyDamage(damageInfo, damageInfo.sourcePosition);
            
            // Play hit effects
            PlayHitFX(damageInfo.sourcePosition);
            
            // Forward modified damage to player
            _playerMono.TakeDamage(modifiedDamage);
            
            if (_debugMode)
            {
                Debug.Log($"PlayerPhysicalHitbox: Forwarded modified damage - {modifiedDamage}");
            }
            
            return modifiedDamage;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Check if this hitbox is properly initialized
        /// </summary>
        public bool IsInitialized => _isInitialized && _playerMono != null;

        #endregion

        #region Damage Modification

        /// <summary>
        /// Modify damage based on hitbox multipliers
        /// For player, multipliers are typically 1.0 (no modification)
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
            
            // Physical hitboxes do NOT transmit CoreHealth damage
            // CoreHealth damage should only be applied to PlayerCrystalCoreHitbox

            // Create modified damage info
            string newDescription = string.IsNullOrEmpty(originalDamage.description)
                ? "Hitbox:PlayerBody"
                : $"{originalDamage.description}|Hitbox:PlayerBody";

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
                    Debug.Log($"PlayerPhysicalHitbox: Spawned hit VFX at {at}");
                }
            }
            
            if (hitSFX) 
            {
                AudioSource.PlayClipAtPoint(hitSFX, at);
                if (_debugMode)
                {
                    Debug.Log($"PlayerPhysicalHitbox: Played hit SFX at {at}");
                }
            }
        }

        #endregion
    }
}

