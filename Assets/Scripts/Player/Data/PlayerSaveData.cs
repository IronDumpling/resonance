using UnityEngine;
using System.Collections.Generic;
using Resonance.Player.Core;
using Resonance.Player.Inventory;

namespace Resonance.Player.Data
{
    /// <summary>
    /// Serializable data structure for saving player state.
    /// Contains all information needed to restore player state from a save point.
    /// </summary>
    [System.Serializable]
    public class PlayerSaveData
    {
        [Header("Save Info")]
        public string saveID;
        public string sceneName;
        public float saveTimestamp;
        public Vector3 savePosition;
        public Vector3 saveRotation; // Euler angles

        [Header("Player Stats")]
        public PlayerRuntimeStats stats;

        [Header("Inventory System - Grid Based")]
        public GridInventorySaveData gridInventory; // 新的Grid背包数据
        public WeaponManagerSaveData weaponManager; // 武器管理器数据

        [Header("Scene-specific Data")]
        public Dictionary<string, bool> collectedItems; // Items collected in each scene
        public Dictionary<string, bool> completedEvents; // Events completed in each scene

        public PlayerSaveData()
        {
            gridInventory = new GridInventorySaveData();
            weaponManager = new WeaponManagerSaveData();
            collectedItems = new Dictionary<string, bool>();
            completedEvents = new Dictionary<string, bool>();
            saveTimestamp = Time.time;
        }

        /// <summary>
        /// Create save data with basic information.
        /// Additional data should be filled by the PlayerController.
        /// </summary>
        public static PlayerSaveData CreateBasicSaveData(string savePointID, string sceneName)
        {
            return new PlayerSaveData
            {
                saveID = savePointID,
                sceneName = sceneName,
                saveTimestamp = Time.time,
                gridInventory = new GridInventorySaveData(),
                weaponManager = new WeaponManagerSaveData()
            };
        }
    }

    /// <summary>
    /// Grid-based inventory save data structure
    /// </summary>
    [System.Serializable]
    public class GridInventorySaveData
    {
        public int gridWidth;
        public int gridHeight;
        public List<GridCellSaveData> items; // All items in the grid
        
        public GridInventorySaveData()
        {
            items = new List<GridCellSaveData>();
        }
    }

    /// <summary>
    /// Save data for a single grid cell item
    /// </summary>
    [System.Serializable]
    public class GridCellSaveData
    {
        // Basic info
        public int itemID;
        public string itemName;
        public string itemType; // Store as string for serialization
        
        // Stack info
        public int quantity;
        public int maxStackQuantity;
        
        // Grid info
        public int gridWidth;
        public int gridHeight;
        public int rotation;
        public Vector2Int gridPosition;
        
        // Equip status
        public bool isEquipped;
        
        // Weapon-specific data
        public int currentAmmo;
        public string ammoType;
        public int maxAmmo;
        
        // Additional data
        public string assetPath;
        public float durability;
        public SerializableDictionary customData; // Custom key-value pairs
        
        public GridCellSaveData()
        {
            customData = new SerializableDictionary();
        }
    }

    /// <summary>
    /// Simple serializable dictionary for custom data
    /// </summary>
    [System.Serializable]
    public class SerializableDictionary
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();
        
        public void Add(string key, object value)
        {
            keys.Add(key);
            values.Add(value?.ToString() ?? "");
        }
        
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();
            for (int i = 0; i < keys.Count && i < values.Count; i++)
            {
                dict[keys[i]] = values[i];
            }
            return dict;
        }
        
        public static SerializableDictionary FromDictionary(Dictionary<string, object> dict)
        {
            var result = new SerializableDictionary();
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    result.Add(kvp.Key, kvp.Value);
                }
            }
            return result;
        }
    }
}
