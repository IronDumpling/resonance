using UnityEngine;
using Resonance.Items;
using Resonance.Interfaces.Services;
using Resonance.Core;
using Resonance.Utilities;

namespace Resonance.Player.Inventory
{
    /// <summary>
    /// WeaponManager - Pure business logic layer for weapon management
    /// Responsibilities: equip/unequip weapons, shoot, reload
    /// Data is read from PlayerInventory, not stored here
    /// </summary>
    public class WeaponManager
    {
        private PlayerInventory _inventory;
        
        // Current equipped weapon ID (not the asset itself)
        private int _equippedWeaponID = -1;
        
        // Cached weapon data (loaded from inventory)
        private GunDataAsset _cachedWeaponAsset;
        
        // Events
        public System.Action<GunDataAsset> OnWeaponEquipped;
        public System.Action OnWeaponUnequipped;
        public System.Action<int> OnAmmoChanged;

        // Properties
        public bool HasEquippedWeapon => _equippedWeaponID != -1 && _cachedWeaponAsset != null;
        public GunDataAsset CurrentGun => _cachedWeaponAsset;
        public int CurrentAmmo => _cachedWeaponAsset?.CurrentAmmo ?? 0;
        public int MaxAmmo => _cachedWeaponAsset?.maxAmmo ?? 0;
        public string AmmoType => _cachedWeaponAsset?.ammoType ?? "None";
        public int EquippedWeaponID => _equippedWeaponID;
        
        public WeaponManager(PlayerInventory inventory)
        {
            _inventory = inventory;
            
            // Listen to inventory events
            _inventory.OnWeaponEquipped += OnInventoryWeaponEquipped;
            _inventory.OnWeaponUnequipped += OnInventoryWeaponUnequipped;
            
            Debug.Log("WeaponManager: Initialized with pure logic layer");
        }
        
        #region Weapon Equip/Unequip
        
        /// <summary>
        /// Equip weapon by ID (weapon must already be in inventory grid)
        /// </summary>
        public bool EquipWeapon(int weaponItemID)
        {
            // Check if weapon exists in inventory
            var weaponData = _inventory.GetItemByID(weaponItemID);
            if (weaponData == null || weaponData.ItemType != ItemType.Weapon)
            {
                Debug.LogWarning($"WeaponManager: Weapon {weaponItemID} not found in inventory or not a weapon");
                return false;
            }
            
            // Unequip current weapon first
            if (_equippedWeaponID != -1)
            {
                UnequipWeapon();
            }
            
            // Load weapon asset from inventory data
            _cachedWeaponAsset = LoadWeaponAssetFromData(weaponData);
            if (_cachedWeaponAsset == null)
            {
                Debug.LogError($"WeaponManager: Failed to load weapon asset for {weaponData.ItemName}");
                return false;
            }
            
            _equippedWeaponID = weaponItemID;
            
            // Update inventory
            _inventory.EquipWeapon(_equippedWeaponID);
            
            // Play equip audio
            PlayWeaponEquipAudio(_cachedWeaponAsset);
            
            // Trigger events
            OnWeaponEquipped?.Invoke(_cachedWeaponAsset);
            OnAmmoChanged?.Invoke(_cachedWeaponAsset.CurrentAmmo);
            
            Debug.Log($"WeaponManager: Equipped weapon {_cachedWeaponAsset.weaponName} (ID: {weaponItemID})");
            return true;
        }
        
        /// <summary>
        /// Unequip current weapon
        /// </summary>
        public void UnequipWeapon()
        {
            if (_equippedWeaponID == -1) return;
            
            Debug.Log($"WeaponManager: Unequipping weapon (ID: {_equippedWeaponID})");
            
            // Update inventory
            _inventory.UnequipCurrentWeapon();
            
            _equippedWeaponID = -1;
            _cachedWeaponAsset = null;
            
            // Trigger events
            OnWeaponUnequipped?.Invoke();
            OnAmmoChanged?.Invoke(0);
        }
        
        #endregion
        
        #region Combat Actions
        
        /// <summary>
        /// Check if can shoot
        /// </summary>
        public bool CanShoot()
        {
            return HasEquippedWeapon && _cachedWeaponAsset.HasAmmo();
        }
        
        /// <summary>
        /// Consume one bullet
        /// </summary>
        public bool ConsumeAmmo()
        {
            if (!HasEquippedWeapon) return false;
            
            bool consumed = _cachedWeaponAsset.ConsumeAmmo();
            if (consumed)
            {
                // Sync ammo count to inventory
                _inventory.UpdateWeaponAmmo(_equippedWeaponID, _cachedWeaponAsset.CurrentAmmo);
                
                OnAmmoChanged?.Invoke(_cachedWeaponAsset.CurrentAmmo);
                Debug.Log($"WeaponManager: Ammo consumed. Remaining: {_cachedWeaponAsset.CurrentAmmo}/{_cachedWeaponAsset.maxAmmo}");
            }
            
            return consumed;
        }
        
        /// <summary>
        /// Reload weapon (restore full ammo)
        /// </summary>
        public void Reload()
        {
            if (_cachedWeaponAsset == null) return;
            
            _cachedWeaponAsset.ResetAmmo();
            OnAmmoChanged?.Invoke(_cachedWeaponAsset.CurrentAmmo);
            
            Debug.Log($"WeaponManager: Reloaded. Ammo: {_cachedWeaponAsset.CurrentAmmo}/{_cachedWeaponAsset.maxAmmo}");
        }
        
        #endregion
        
        #region Info & Query
        
        /// <summary>
        /// Get weapon info (for debug and UI display)
        /// </summary>
        public string GetWeaponInfo()
        {
            if (!HasEquippedWeapon) return "No Weapon";
            
            return $"{_cachedWeaponAsset.weaponName} ({_cachedWeaponAsset.CurrentAmmo}/{_cachedWeaponAsset.maxAmmo} {_cachedWeaponAsset.ammoType})";
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnInventoryWeaponEquipped(int weaponID)
        {
            // Inventory notifies us that a weapon was equipped
            // This is for synchronization when equipping from UI
            Debug.Log($"WeaponManager: Received OnWeaponEquipped from inventory: {weaponID}");
        }
        
        private void OnInventoryWeaponUnequipped()
        {
            // Inventory notifies us that weapon was unequipped
            Debug.Log("WeaponManager: Received OnWeaponUnequipped from inventory");
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Load weapon asset from GridCellData
        /// </summary>
        private GunDataAsset LoadWeaponAssetFromData(GridCellData weaponData)
        {
            // Try to get from CustomData first
            if (weaponData.CustomData.ContainsKey("originalAsset") && 
                weaponData.CustomData["originalAsset"] is GunDataAsset originalAsset)
            {
                return originalAsset;
            }
            
            // Try to load from AssetPath
            if (!string.IsNullOrEmpty(weaponData.AssetPath))
            {
                return LoadAssetFromPath<GunDataAsset>(weaponData.AssetPath);
            }
            
            Debug.LogError($"WeaponManager: Cannot load weapon asset for {weaponData.ItemName}");
            return null;
        }
        
        /// <summary>
        /// Load ScriptableObject from path
        /// </summary>
        private T LoadAssetFromPath<T>(string path) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            #else
            // Load from Resources at runtime
            string resourcePath = path;
            if (path.StartsWith("Assets/Resources/"))
            {
                resourcePath = path.Substring("Assets/Resources/".Length);
            }
            if (resourcePath.EndsWith(".asset"))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - ".asset".Length);
            }
            
            return Resources.Load<T>(resourcePath);
            #endif
        }
        
        /// <summary>
        /// Play weapon equip audio based on weapon type
        /// </summary>
        private void PlayWeaponEquipAudio(GunDataAsset gunData)
        {
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService == null) return;
            
            AudioClipType audioClipType = AudioClipType.PistoArming;
            
            if (gunData.ammoType == "Pisto")
            {
                audioClipType = AudioClipType.PistoArming;
            }
            else if (gunData.ammoType == "Rifle")
            {
                audioClipType = AudioClipType.RifleArming;
            }
            
            audioService.PlaySFX2D(audioClipType, 0.8f, 1f);
            Debug.Log($"WeaponManager: Played equip audio for {gunData.weaponName}");
        }
        
        #endregion
        
        #region Save/Load System
        
        /// <summary>
        /// Get save data
        /// </summary>
        public WeaponManagerSaveData GetSaveData()
        {
            return new WeaponManagerSaveData
            {
                equippedWeaponID = _equippedWeaponID,
                weaponName = _cachedWeaponAsset?.weaponName ?? "",
                currentAmmo = _cachedWeaponAsset?.CurrentAmmo ?? 0,
                maxAmmo = _cachedWeaponAsset?.maxAmmo ?? 0,
                ammoType = _cachedWeaponAsset?.ammoType ?? "",
                assetPath = _cachedWeaponAsset != null ? GetAssetPath(_cachedWeaponAsset) : ""
            };
        }
        
        /// <summary>
        /// Load from save data
        /// </summary>
        public void LoadFromSaveData(WeaponManagerSaveData saveData)
        {
            Debug.Log($"WeaponManager: Loading weapon manager from save data...");
            
            if (saveData == null || saveData.equippedWeaponID == -1)
            {
                Debug.Log($"WeaponManager: No weapon to load");
                _equippedWeaponID = -1;
                _cachedWeaponAsset = null;
                return;
            }
            
            Debug.Log($"WeaponManager: Loading weapon ID: {saveData.equippedWeaponID}");
            
            // Get weapon from PlayerInventory
            var weaponData = _inventory.GetItemByID(saveData.equippedWeaponID);
            if (weaponData != null && weaponData.ItemType == ItemType.Weapon)
            {
                _cachedWeaponAsset = LoadWeaponAssetFromData(weaponData);
                if (_cachedWeaponAsset != null)
                {
                    _equippedWeaponID = saveData.equippedWeaponID;
                    
                    // Restore ammo state
                    _cachedWeaponAsset.SetCurrentAmmo(saveData.currentAmmo);
                    
                    // Set equipped status in inventory (this will set IsEquipped flag)
                    _inventory.EquipWeapon(_equippedWeaponID);
                    
                    OnWeaponEquipped?.Invoke(_cachedWeaponAsset);
                    OnAmmoChanged?.Invoke(_cachedWeaponAsset.CurrentAmmo);
                    
                    Debug.Log($"WeaponManager: Loaded and equipped weapon {_cachedWeaponAsset.weaponName} with {_cachedWeaponAsset.CurrentAmmo} ammo");
                }
            }
            else
            {
                Debug.LogWarning($"WeaponManager: Weapon {saveData.equippedWeaponID} not found in inventory");
            }
        }
        
        /// <summary>
        /// Get ScriptableObject asset path
        /// </summary>
        private string GetAssetPath(ScriptableObject asset)
        {
            if (asset == null) return "";
            
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(asset);
            #else
            return asset.name;
            #endif
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Cleanup()
        {
            // Unsubscribe from inventory events
            if (_inventory != null)
            {
                _inventory.OnWeaponEquipped -= OnInventoryWeaponEquipped;
                _inventory.OnWeaponUnequipped -= OnInventoryWeaponUnequipped;
            }
            
            OnWeaponEquipped = null;
            OnWeaponUnequipped = null;
            OnAmmoChanged = null;
            _cachedWeaponAsset = null;
            _inventory = null;
        }
        
        #endregion
    }
    
    /// <summary>
    /// WeaponManager save data structure
    /// </summary>
    [System.Serializable]
    public class WeaponManagerSaveData
    {
        public int equippedWeaponID;
        public string weaponName;
        public int currentAmmo;
        public int maxAmmo;
        public string ammoType;
        public string assetPath;
        
        public WeaponManagerSaveData()
        {
            equippedWeaponID = -1;
            weaponName = "";
            currentAmmo = 0;
            maxAmmo = 0;
            ammoType = "";
            assetPath = "";
        }
    }
}
