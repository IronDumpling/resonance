using UnityEngine;
using Resonance.Gameplay.Items;
using Resonance.Shared.Interfaces.Services;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Shared.Types;
using Resonance.Systems.GridSystem;

namespace Resonance.Gameplay.Player.Inventory
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
        private WeaponDataAsset _cachedWeaponAsset;
        
        // Events
        public System.Action<WeaponDataAsset> OnWeaponEquipped;
        public System.Action OnWeaponUnequipped;

        // Properties
        public bool HasEquippedWeapon => _equippedWeaponID != -1 && _cachedWeaponAsset != null;
        public WeaponDataAsset CurrentWeapon => _cachedWeaponAsset;
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
        }
        
        #endregion
        
        #region Info & Query
        
        /// <summary>
        /// Get weapon info (for debug and UI display)
        /// </summary>
        public string GetWeaponInfo()
        {
            if (!HasEquippedWeapon) return "No Weapon";
            
            return $"{_cachedWeaponAsset.weaponName} (Energy Cost: {_cachedWeaponAsset.energyCostPerShot})";
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
        /// Load weapon asset from GridItem
        /// </summary>
        private WeaponDataAsset LoadWeaponAssetFromData(GridItem weaponData)
        {
            Debug.Log($"WeaponManager: LoadWeaponAssetFromData called for '{weaponData.ItemName}'");
            Debug.Log($"WeaponManager: - AssetPath: '{weaponData.AssetPath}'");
            Debug.Log($"WeaponManager: - CustomData keys: {string.Join(", ", weaponData.CustomData.Keys)}");
            
            // Try to get from CustomData first
            if (weaponData.CustomData.ContainsKey("originalAsset"))
            {
                Debug.Log($"WeaponManager: Found 'originalAsset' in CustomData, type: {weaponData.CustomData["originalAsset"]?.GetType().Name ?? "null"}");
                
                if (weaponData.CustomData["originalAsset"] is WeaponDataAsset originalAsset)
                {
                    Debug.Log($"WeaponManager: Successfully retrieved WeaponDataAsset from CustomData: {originalAsset.weaponName}");
                    return originalAsset;
                }
            }
            
            // Try to load from AssetPath
            if (!string.IsNullOrEmpty(weaponData.AssetPath))
            {
                Debug.Log($"WeaponManager: Attempting to load from AssetPath: '{weaponData.AssetPath}'");
                var asset = LoadAssetFromPath<WeaponDataAsset>(weaponData.AssetPath);
                if (asset != null)
                {
                    Debug.Log($"WeaponManager: Successfully loaded weapon: {asset.weaponName}");
                    return asset;
                }
                else
                {
                    Debug.LogError($"WeaponManager: LoadAssetFromPath returned null for path: '{weaponData.AssetPath}'");
                }
            }
            else
            {
                Debug.LogError($"WeaponManager: AssetPath is null or empty for {weaponData.ItemName}");
            }
            
            Debug.LogError($"WeaponManager: Cannot load weapon asset for {weaponData.ItemName}");
            return null;
        }
        
        /// <summary>
        /// Load ScriptableObject from path
        /// </summary>
        private T LoadAssetFromPath<T>(string path) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"WeaponManager: LoadAssetFromPath called with empty path");
                return null;
            }
            
            Debug.Log($"WeaponManager: [EDITOR] Attempting to load asset from path: {path}");
            
            #if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                Debug.Log($"WeaponManager: [EDITOR] ✓ Successfully loaded '{asset.name}' from Editor AssetDatabase");
                
                // Store asset back to CustomData if possible (for next time)
                // This won't help with serialization but good for in-editor session
            }
            else
            {
                Debug.LogError($"WeaponManager: [EDITOR] ✗ Failed to load from Editor path: {path}");
            }
            return asset;
            #else
            // Runtime: try multiple approaches
            T asset = null;
            
            // Approach 1: Standard Resources path conversion
            string resourcePath = path;
            if (path.StartsWith("Assets/Resources/"))
            {
                resourcePath = path.Substring("Assets/Resources/".Length);
            }
            if (resourcePath.EndsWith(".asset"))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - ".asset".Length);
            }
            
            Debug.Log($"WeaponManager: Trying Resources path: {resourcePath}");
            asset = Resources.Load<T>(resourcePath);
            
            // Approach 2: Try just the filename
            if (asset == null)
            {
                string filename = System.IO.Path.GetFileNameWithoutExtension(path);
                Debug.Log($"WeaponManager: Resources path failed, trying filename: {filename}");
                asset = Resources.Load<T>(filename);
            }
            
            // Approach 3: Try common prefixes
            if (asset == null)
            {
                string filename = System.IO.Path.GetFileNameWithoutExtension(path);
                string[] commonPaths = new string[]
                {
                    $"Data/Items/{filename}",
                    $"Items/{filename}",
                    $"Weapons/{filename}"
                };
                
                foreach (var tryPath in commonPaths)
                {
                    Debug.Log($"WeaponManager: Trying common path: {tryPath}");
                    asset = Resources.Load<T>(tryPath);
                    if (asset != null)
                    {
                        Debug.Log($"WeaponManager: Success with path: {tryPath}");
                        break;
                    }
                }
            }
            
            if (asset == null)
            {
                Debug.LogError($"WeaponManager: Failed to load asset from all attempted paths. Original path: {path}");
            }
            else
            {
                Debug.Log($"WeaponManager: Successfully loaded {asset.name} at runtime");
            }
            
            return asset;
            #endif
        }
        
        /// <summary>
        /// Play weapon equip audio based on weapon type
        /// </summary>
        private void PlayWeaponEquipAudio(WeaponDataAsset gunData)
        {
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService == null) return;
            
            AudioClipType audioClipType = AudioClipType.PistoArming;
            
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
                    
                    // Set equipped status in inventory (this will set IsEquipped flag)
                    _inventory.EquipWeapon(_equippedWeaponID);
                    
                    OnWeaponEquipped?.Invoke(_cachedWeaponAsset);
                    
                    Debug.Log($"WeaponManager: Loaded and equipped weapon {_cachedWeaponAsset.weaponName}");
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
        public string assetPath;
        
        public WeaponManagerSaveData()
        {
            equippedWeaponID = -1;
            weaponName = "";
            assetPath = "";
        }
    }
}
