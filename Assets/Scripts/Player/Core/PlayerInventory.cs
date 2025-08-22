using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Resonance.Player.Data;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Player inventory system that manages items, equipment, and carry weight.
    /// </summary>
    public class PlayerInventory
    {
        private List<InventoryItem> _items;
        private List<int> _equippedItemIDs;
        private int _maxSlots;
        private float _maxCarryWeight;

        // Events
        public System.Action OnInventoryChanged;
        public System.Action<int> OnItemEquipped; // itemID
        public System.Action<int> OnItemUnequipped; // itemID

        // Properties
        public int MaxSlots => _maxSlots;
        public float MaxCarryWeight => _maxCarryWeight;
        public int UsedSlots => _items.Count;
        public float CurrentWeight => _items.Sum(item => item.Weight * item.Quantity);
        public bool IsFull => UsedSlots >= _maxSlots;
        public bool IsOverWeight => CurrentWeight > _maxCarryWeight;

        public PlayerInventory(int maxSlots, float maxCarryWeight)
        {
            _maxSlots = maxSlots;
            _maxCarryWeight = maxCarryWeight;
            _items = new List<InventoryItem>();
            _equippedItemIDs = new List<int>();
        }

        #region Item Management

        public bool AddItem(int itemID, int quantity = 1, float durability = 1f)
        {
            // Check if we have space
            var existingItem = _items.FirstOrDefault(item => item.ItemID == itemID);
            
            if (existingItem != null)
            {
                // Stack with existing item
                existingItem.Quantity += quantity;
                Debug.Log($"PlayerInventory: Added {quantity} of item {itemID} (stacked)");
            }
            else
            {
                // Check slot limit
                if (IsFull)
                {
                    Debug.LogWarning("PlayerInventory: Cannot add item - inventory full");
                    return false;
                }

                // Add new item
                var newItem = new InventoryItem(itemID, quantity, durability);
                _items.Add(newItem);
                Debug.Log($"PlayerInventory: Added new item {itemID} x{quantity}");
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(int itemID, int quantity = 1)
        {
            var item = _items.FirstOrDefault(i => i.ItemID == itemID);
            if (item == null)
            {
                Debug.LogWarning($"PlayerInventory: Cannot remove item {itemID} - not found");
                return false;
            }

            if (item.Quantity < quantity)
            {
                Debug.LogWarning($"PlayerInventory: Cannot remove {quantity} of item {itemID} - only have {item.Quantity}");
                return false;
            }

            item.Quantity -= quantity;
            if (item.Quantity <= 0)
            {
                _items.Remove(item);
                
                // Unequip if it was equipped
                if (_equippedItemIDs.Contains(itemID))
                {
                    UnequipItem(itemID);
                }
            }

            OnInventoryChanged?.Invoke();
            Debug.Log($"PlayerInventory: Removed {quantity} of item {itemID}");
            return true;
        }

        public bool HasItem(int itemID, int quantity = 1)
        {
            var item = _items.FirstOrDefault(i => i.ItemID == itemID);
            return item != null && item.Quantity >= quantity;
        }

        public int GetItemQuantity(int itemID)
        {
            var item = _items.FirstOrDefault(i => i.ItemID == itemID);
            return item?.Quantity ?? 0;
        }

        public List<InventoryItem> GetAllItems()
        {
            return new List<InventoryItem>(_items);
        }

        #endregion

        #region Equipment System

        public bool EquipItem(int itemID)
        {
            if (!HasItem(itemID))
            {
                Debug.LogWarning($"PlayerInventory: Cannot equip item {itemID} - not in inventory");
                return false;
            }

            if (_equippedItemIDs.Contains(itemID))
            {
                Debug.LogWarning($"PlayerInventory: Item {itemID} already equipped");
                return false;
            }

            _equippedItemIDs.Add(itemID);
            OnItemEquipped?.Invoke(itemID);
            Debug.Log($"PlayerInventory: Equipped item {itemID}");
            return true;
        }

        public bool UnequipItem(int itemID)
        {
            if (!_equippedItemIDs.Contains(itemID))
            {
                Debug.LogWarning($"PlayerInventory: Cannot unequip item {itemID} - not equipped");
                return false;
            }

            _equippedItemIDs.Remove(itemID);
            OnItemUnequipped?.Invoke(itemID);
            Debug.Log($"PlayerInventory: Unequipped item {itemID}");
            return true;
        }

        public bool IsItemEquipped(int itemID)
        {
            return _equippedItemIDs.Contains(itemID);
        }

        public List<int> GetEquippedItemIDs()
        {
            return new List<int>(_equippedItemIDs);
        }

        #endregion

        #region Save/Load System

        public List<ItemSaveData> GetSaveData()
        {
            return _items.Select(item => new ItemSaveData(item.ItemID, item.Quantity, item.Durability)).ToList();
        }

        public void LoadFromSaveData(List<ItemSaveData> saveData, List<int> equippedItems)
        {
            _items.Clear();
            _equippedItemIDs.Clear();

            foreach (var itemData in saveData)
            {
                var item = new InventoryItem(itemData.itemID, itemData.quantity, itemData.durability);
                _items.Add(item);
            }

            _equippedItemIDs.AddRange(equippedItems);

            OnInventoryChanged?.Invoke();
            Debug.Log($"PlayerInventory: Loaded {_items.Count} items from save data");
        }

        #endregion
    }

    /// <summary>
    /// Represents an item in the player's inventory
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        public int ItemID { get; set; }
        public int Quantity { get; set; }
        public float Durability { get; set; }
        public float Weight { get; set; } // This would typically come from item database

        public InventoryItem(int itemID, int quantity, float durability = 1f)
        {
            ItemID = itemID;
            Quantity = quantity;
            Durability = durability;
            Weight = 1f; // Default weight, should be loaded from item database
        }
    }
}
