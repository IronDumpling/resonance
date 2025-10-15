using UnityEngine;
using System.Collections.Generic;
using Resonance.Interfaces;
using Resonance.Interfaces.Objects;
using Resonance.Items.Core;
using Resonance.Utilities;

namespace Resonance.Items
{
    /// <summary>
    /// Weapon Accuracy Configuration
    /// Controls crosshair size, shrinking/expanding behavior, and damage bonuses
    /// </summary>
    [System.Serializable]
    public class WeaponAccuracyConfig
    {
        [Header("Crosshair Size (World Space Units)")]
        [Tooltip("Base crosshair radius when starting to aim")]
        public float baseRadius = 50f;
        
        [Tooltip("Minimum crosshair radius (perfect aim)")]
        public float minRadius = 20f;
        
        [Tooltip("Maximum crosshair radius")]
        public float maxRadius = 80f;
        
        [Header("Crosshair Shrinking")]

        [Tooltip("Lerp speed for shrinking crosshair")]
        public float shrinkLerpSpeed = 0.5f;
        
        [Header("Crosshair Expansion Penalties")]
        [Tooltip("Lerp speed for expanding crosshair")]
        public float expandLerpSpeed = 3.0f;

        [Tooltip("Radius increase when moving")]
        public float movementRadiusPenalty = 1.0f;
        
        [Tooltip("Radius increase when rotating aim point rapidly")]
        public float rotationRadiusPenalty = 0.5f;
        
        [Tooltip("Radius increase after each shot")]
        public float shootRadiusIncrease = 1.5f;
        
        [Tooltip("Delay before crosshair starts shrinking after shooting (seconds)")]
        public float shootRecoveryDelay = 0.3f;

        [Header("Rotation Detection")]
        [Tooltip("Mouse movement distance threshold to trigger rotation penalty (screen pixels per second)")]
        public float rotationThreshold = 100.0f;
        
        [Header("Accuracy Damage Bonus")]
        [Tooltip("Damage multiplier at perfect aim (min radius)")]
        public float perfectAimDamageMultiplier = 2.0f;
        
        [Tooltip("Damage multiplier at base aim")]
        public float baseAimDamageMultiplier = 1.0f;
        
        [Tooltip("Damage multiplier curve (X: 0=worst aim, 1=perfect aim; Y: damage multiplier)")]
        public AnimationCurve damageMultiplierCurve = AnimationCurve.Linear(0, 1.0f, 1, 2.0f);
    }
    
    /// <summary>
    /// Weapon Recoil Configuration
    /// Controls recoil offset and recovery behavior
    /// </summary>
    [System.Serializable]
    public class WeaponRecoilConfig
    {
        [Header("Recoil Offset (World Space)")]
        [Tooltip("Base recoil offset per shot - affected by consecutive shots and aiming (X: horizontal, Y: vertical, Z: backward)")]
        public Vector3 recoilOffset = new Vector3(0f, 1f, 0f);
        
        [Tooltip("Random variance range for recoil - affected by accuracy (perfect aim = 0.1x, worst aim = 1.0x)")]
        public Vector3 recoilVariance = new Vector3(0.5f, 0.5f, 0.5f);
        
        [Header("Consecutive Shot Recoil")]
        [Tooltip("Recoil multiplier based on consecutive shots (X: shot number, Y: multiplier)")]
        public AnimationCurve recoilMultiplierCurve = AnimationCurve.Linear(0, 1.0f, 5, 2.0f);
        
        [Header("Recoil Recovery")]
        [Tooltip("Delay before recoil starts recovering (seconds)")]
        public float recoveryDelay = 0.3f;
        
        [Tooltip("Recoil recovery speed (units per second)")]
        public float recoverySpeed = 3.0f;
    }

    /// <summary>
    /// Weapon data ScriptableObject asset
    /// Used to create and edit Weapon configurations in Unity Editor
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon Data", menuName = "Resonance/Items/Weapon Data", order = 1)]
    public class WeaponDataAsset : ScriptableObject, IInfoable
    {
        [Header("Basic Info")]
        public string weaponName = "Basic Weapon";
        [TextArea(2, 4)]
        public string weaponDescription = "A basic firearm";
        
        [Header("Ammo")]
        public int maxAmmo = 8;
        public string ammoType = "Pisto";
        
        // Runtime state - not serialized, reset each time
        [System.NonSerialized] private int _currentAmmo = -1; // -1 means not initialized
        
        [Header("Combat Stats")]
        public float range = 30f;
        public float fireRate = 1f; // shots per second
        
        [Header("Damage Configuration")]
        public Damages damages;
        
        [Header("Accuracy Settings")]
        [Tooltip("Weapon accuracy configuration (required)")]
        public WeaponAccuracyConfig accuracyConfig = new WeaponAccuracyConfig();
        
        [Header("Recoil Settings")]
        [Tooltip("Weapon recoil configuration (required)")]
        public WeaponRecoilConfig recoilConfig = new WeaponRecoilConfig();
        
        [Header("Visual")]
        public Sprite weaponIcon;
        public GameObject itemPrefab;
        
        [Header("Inventory")]
        public int gridWidth = 3;
        public int gridHeight = 2;

        // Runtime Properties
        public int CurrentAmmo 
        { 
            get 
            { 
                if (_currentAmmo == -1) _currentAmmo = maxAmmo; // Initialize on first access
                return _currentAmmo; 
            } 
            set { _currentAmmo = value; } 
        }

        /// <summary>
        /// Validate Weapon data
        /// </summary>
        /// <returns>Validation result</returns>
        public bool ValidateData()
        {
            if (string.IsNullOrEmpty(weaponName))
            {
                Debug.LogError($"WeaponDataAsset: {name} has empty weapon name");
                return false;
            }

            if (maxAmmo <= 0)
            {
                Debug.LogError($"WeaponDataAsset: {weaponName} has invalid max ammo: {maxAmmo}");
                return false;
            }

            if (damages.GetCount() == 0)
            {
                Debug.LogError($"WeaponDataAsset: {weaponName} has no valid damage (all damage types are 0)");
                return false;
            }

            if (range <= 0)
            {
                Debug.LogError($"WeaponDataAsset: {weaponName} has invalid range: {range}");
                return false;
            }

            if (fireRate <= 0)
            {
                Debug.LogError($"WeaponDataAsset: {weaponName} has invalid fire rate: {fireRate}");
                return false;
            }

            return true;
        }

        #region Runtime Methods

        /// <summary>
        /// Check if there is ammo
        /// </summary>
        /// <returns>是否有弹药</returns>
        public bool HasAmmo()
        {
            return CurrentAmmo > 0;
        }

        /// <summary>
        /// Check if there is full ammo
        /// </summary>
        /// <returns>Is full ammo</returns>
        public bool IsFullAmmo()
        {
            return CurrentAmmo >= maxAmmo;
        }

        /// <summary>
        /// Get ammo percentage
        /// </summary>
        /// <returns>Ammo percentage (0-1)</returns>
        public float GetAmmoPercentage()
        {
            if (maxAmmo <= 0) return 0f;
            return (float)CurrentAmmo / maxAmmo;
        }

        /// <summary>
        /// Reset ammo to full ammo state
        /// </summary>
        public void ResetAmmo()
        {
            CurrentAmmo = maxAmmo;
        }

        /// <summary>
        /// Consume one ammo
        /// </summary>
        /// <returns>Is success consume</returns>
        public bool ConsumeAmmo()
        {
            if (CurrentAmmo > 0)
            {
                CurrentAmmo--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set current ammo count
        /// </summary>
        /// <param name="ammoCount">Ammo count</param>
        public void SetCurrentAmmo(int ammoCount)
        {
            CurrentAmmo = Mathf.Clamp(ammoCount, 0, maxAmmo);
        }

        /// <summary>
        /// Create damage info structure with all configured damage types
        /// </summary>
        /// <param name="sourcePosition">Damage source position</param>
        /// <param name="sourceObject">Damage source object</param>
        /// <param name="damageMultiplier">Optional damage multiplier (from accuracy system)</param>
        /// <returns>Damage info</returns>
        public DamageInfo CreateDamageInfo(Vector3 sourcePosition, GameObject sourceObject = null, float damageMultiplier = 1f)
        {            
            // Add each damage type if > 0
            if (damages.HasDamage(DamageType.PhysicalHealth))
            {
                float physicalHealthDamage = damages.GetDamage(DamageType.PhysicalHealth);
                damages.SetDamage(DamageType.PhysicalHealth, physicalHealthDamage * damageMultiplier);
            }
            
            if (damages.HasDamage(DamageType.CoreHealth))
            {
                float coreHealthDamage = damages.GetDamage(DamageType.CoreHealth);
                damages.SetDamage(DamageType.CoreHealth, coreHealthDamage * damageMultiplier);
            }
            
            if (damages.HasDamage(DamageType.Chaos))
            {
                float chaosDamage = damages.GetDamage(DamageType.Chaos);
                damages.SetDamage(DamageType.Chaos, chaosDamage * damageMultiplier);
            }
            
            return new DamageInfo(
                damages: damages,
                sourcePosition: sourcePosition,
                sourceObject: sourceObject,
                description: $"{weaponName} shot"
            );
        }
        
        /// <summary>
        /// Get damage type description text (lists all active damage types)
        /// </summary>
        /// <returns>Damage type description</returns>
        public string GetDamageTypeDescription()
        {
            List<string> damageTypes = new List<string>();
            
            if (damages.HasDamage(DamageType.PhysicalHealth))
                damageTypes.Add($"Physical: {damages.GetDamage(DamageType.PhysicalHealth):F0}");
            if (damages.HasDamage(DamageType.CoreHealth))
                damageTypes.Add($"Core: {damages.GetDamage(DamageType.CoreHealth):F0}");
            if (damages.HasDamage(DamageType.Chaos))
                damageTypes.Add($"Chaos: {damages.GetDamage(DamageType.Chaos):F0}");
            
            return damageTypes.Count > 0 ? string.Join(", ", damageTypes) : "No Damage";
        }
        
        /// <summary>
        /// Get total damage (sum of all damage types)
        /// </summary>
        /// <returns>Total damage</returns>
        public float GetTotalDamage()
        {
            return damages.GetTotalDamage();
        }

        /// <summary>
        /// Create a runtime copy of this weapon (for pickup)
        /// Note: This will create a new ScriptableObject instance, for runtime independent weapon state
        /// </summary>
        /// <returns>Weapon copy</returns>
        public WeaponDataAsset CreateRuntimeCopy()
        {
            var copy = ScriptableObject.CreateInstance<WeaponDataAsset>();
            
            // Copy all properties
            copy.weaponName = this.weaponName;
            copy.weaponDescription = this.weaponDescription;
            copy.maxAmmo = this.maxAmmo;
            copy.ammoType = this.ammoType;
            copy.range = this.range;
            copy.fireRate = this.fireRate;
            copy.damages = this.damages;
            copy.accuracyConfig = this.accuracyConfig;
            copy.recoilConfig = this.recoilConfig;
            copy.weaponIcon = this.weaponIcon;
            copy.itemPrefab = this.itemPrefab;
            copy.gridWidth = this.gridWidth;
            copy.gridHeight = this.gridHeight;
            
            // Initialize runtime state
            copy._currentAmmo = this.maxAmmo; // Start with full ammo
            
            return copy;
        }

        #endregion

        #region IInfoable Implementation

        /// <summary>
        /// Get information data to display in InfoPanel
        /// </summary>
        public InfoData GetInfoData()
        {
            return new InfoData(
                name: weaponName,
                content: weaponDescription,
                image: weaponIcon
            );
        }

        /// <summary>
        /// Check if there is valid information to display
        /// </summary>
        public bool HasValidInfo()
        {
            var info = GetInfoData();
            return info.IsValid();
        }

        #endregion

        #region Unity Editor

        void OnValidate()
        {
            // Ensure values are within reasonable range
            maxAmmo = Mathf.Max(1, maxAmmo);
            range = Mathf.Max(1f, range);
            fireRate = Mathf.Max(0.1f, fireRate);
            gridWidth = Mathf.Clamp(gridWidth, 1, 10);
            gridHeight = Mathf.Clamp(gridHeight, 1, 10);
        }

        #endregion
    }
}
