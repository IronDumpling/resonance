using UnityEngine;
using System.Collections.Generic;

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

        [Header("Inventory")]
        public List<ItemSaveData> inventory;
        public List<int> equippedItemIDs; // IDs of currently equipped items

        [Header("Progression")]
        public int playerLevel;
        public float experience;
        public List<string> unlockedAbilities;
        public Dictionary<string, bool> levelFlags; // Story/progression flags
        public Dictionary<string, float> gameVariables; // Numeric game variables

        [Header("Scene-specific Data")]
        public Dictionary<string, bool> collectedItems; // Items collected in each scene
        public Dictionary<string, bool> completedEvents; // Events completed in each scene

        public PlayerSaveData()
        {
            inventory = new List<ItemSaveData>();
            equippedItemIDs = new List<int>();
            unlockedAbilities = new List<string>();
            levelFlags = new Dictionary<string, bool>();
            gameVariables = new Dictionary<string, float>();
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
                saveTimestamp = Time.time
            };
        }
    }

    /// <summary>
    /// Serializable item data for saving inventory state
    /// </summary>
    [System.Serializable]
    public class ItemSaveData
    {
        public int itemID;
        public int quantity;
        public float durability;
        public Dictionary<string, object> customData; // For special item properties

        public ItemSaveData(int id, int qty, float dur = 1f)
        {
            itemID = id;
            quantity = qty;
            durability = dur;
            customData = new Dictionary<string, object>();
        }
    }
}
