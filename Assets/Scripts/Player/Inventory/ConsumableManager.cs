using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Items;
using Resonance.Utilities;
using Resonance.Utilities.Types;
using Resonance.Utilities.GridSystem;

namespace Resonance.Player.Inventory
{
    /// <summary>
    /// ConsumableManager - Manage consumable items (ammo, etc.)
    /// Responsibilities: add/consume ammo, stack same type ammo, manage consumable usage
    /// </summary>
    public class ConsumableManager
    {
        private PlayerInventory _inventory;
        
        // Events
        public System.Action<string, int> OnAmmoAdded; // ammoType, amount
        public System.Action<string, int> OnAmmoConsumed; // ammoType, amount
        public System.Action<string, int, int> OnAmmoCountChanged; // ammoType, oldCount, newCount
        
        public ConsumableManager(PlayerInventory inventory)
        {
            _inventory = inventory;
            Debug.Log("ConsumableManager: Initialized");
        }
        
        #region Ammo Management
        
        /// <summary>
        /// Add ammo (smart stacking)
        /// </summary>
        public bool AddAmmo(string ammoType, GridItem gridItem)
        {
            if (string.IsNullOrEmpty(ammoType) || gridItem.Quantity <= 0)
            {
                Debug.LogWarning($"ConsumableManager: Invalid ammo parameters - type: {ammoType}, amount: {gridItem.Quantity}");
                return false;
            }
            
            Debug.Log($"ConsumableManager: Adding {gridItem.Quantity} {ammoType} ammo (Icon={gridItem.ItemIcon != null}, Prefab={gridItem.ItemPrefab != null}, AssetPath={gridItem.AssetPath})");
            
            // Find existing ammo of the same type
            var existingAmmo = _inventory.GetItemsByType(ItemType.Consumable)
                .Where(item => item.CustomData.ContainsKey("ammoType") && 
                              item.CustomData["ammoType"].ToString() == ammoType)
                .ToList();
            
            int remainingAmount = gridItem.Quantity;
            
            // Try to stack onto existing ammo
            foreach (var ammo in existingAmmo)
            {
                if (ammo.Quantity < ammo.MaxStackQuantity)
                {
                    int canAdd = Mathf.Min(remainingAmount, ammo.MaxStackQuantity - ammo.Quantity);
                    int newQuantity = ammo.Quantity + canAdd;
                    
                    _inventory.UpdateItemQuantity(ammo.ItemID, newQuantity);
                    remainingAmount -= canAdd;
                    
                    Debug.Log($"ConsumableManager: Stacked {canAdd} to existing ammo. New quantity: {newQuantity}");
                    
                    if (remainingAmount <= 0) break;
                }
            }
            
            // If there's remaining, create a new stack
            while (remainingAmount > 0)
            {
                int newStackAmount = Mathf.Min(remainingAmount, gridItem.MaxStackQuantity);
                
                if (!CreateNewAmmoStack(ammoType, gridItem))
                {
                    Debug.LogWarning($"ConsumableManager: Failed to create new ammo stack. Remaining: {remainingAmount}");
                    break;
                }
                
                remainingAmount -= newStackAmount;
                Debug.Log($"ConsumableManager: Created new ammo stack with {newStackAmount}. Remaining: {remainingAmount}");
            }
            
            int totalAdded = gridItem.Quantity - remainingAmount;
            if (totalAdded > 0)
            {
                OnAmmoAdded?.Invoke(ammoType, totalAdded);
                int newTotal = GetTotalAmmoCount(ammoType);
                OnAmmoCountChanged?.Invoke(ammoType, newTotal - totalAdded, newTotal);
                Debug.Log($"ConsumableManager: Successfully added {totalAdded} {ammoType} ammo. Total: {newTotal}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Consume ammo
        /// </summary>
        public bool ConsumeAmmo(string ammoType, int amount)
        {
            if (string.IsNullOrEmpty(ammoType) || amount <= 0)
                return false;
            
            int totalAvailable = GetTotalAmmoCount(ammoType);
            if (totalAvailable < amount)
            {
                Debug.LogWarning($"ConsumableManager: Not enough {ammoType} ammo - need {amount}, have {totalAvailable}");
                return false;
            }
            
            Debug.Log($"ConsumableManager: Consuming {amount} {ammoType} ammo");
            
            // Get all ammo of the same type, sorted by quantity
            var ammoStacks = _inventory.GetItemsByType(ItemType.Consumable)
                .Where(item => item.CustomData.ContainsKey("ammoType") && 
                              item.CustomData["ammoType"].ToString() == ammoType)
                .OrderBy(item => item.Quantity)
                .ToList();
            
            int remainingToConsume = amount;
            
            foreach (var ammo in ammoStacks)
            {
                int consumeFromThis = Mathf.Min(remainingToConsume, ammo.Quantity);
                int newQuantity = ammo.Quantity - consumeFromThis;
                
                _inventory.UpdateItemQuantity(ammo.ItemID, newQuantity); // If it reaches zero, it will be automatically removed
                remainingToConsume -= consumeFromThis;
                
                Debug.Log($"ConsumableManager: Consumed {consumeFromThis} from stack. New quantity: {newQuantity}");
                
                if (remainingToConsume <= 0) break;
            }
            
            OnAmmoConsumed?.Invoke(ammoType, amount);
            int newTotal = GetTotalAmmoCount(ammoType);
            OnAmmoCountChanged?.Invoke(ammoType, totalAvailable, newTotal);
            
            Debug.Log($"ConsumableManager: Consumed {amount} {ammoType} ammo. Remaining: {newTotal}");
            return true;
        }
        
        /// <summary>
        /// Get total ammo count
        /// </summary>
        public int GetTotalAmmoCount(string ammoType)
        {
            if (string.IsNullOrEmpty(ammoType))
                return 0;
            
            return _inventory.GetItemsByType(ItemType.Consumable)
                .Where(item => item.CustomData.ContainsKey("ammoType") && 
                              item.CustomData["ammoType"].ToString() == ammoType)
                .Sum(item => item.Quantity);
        }
        
        /// <summary>
        /// Check if there's enough ammo
        /// </summary>
        public bool HasAmmo(string ammoType, int amount = 1)
        {
            return GetTotalAmmoCount(ammoType) >= amount;
        }
        
        /// <summary>
        /// Get all ammo types and quantities
        /// </summary>
        public Dictionary<string, int> GetAllAmmo()
        {
            var ammoDict = new Dictionary<string, int>();
            
            var allAmmo = _inventory.GetItemsByType(ItemType.Consumable)
                .Where(item => item.CustomData.ContainsKey("ammoType"));
            
            foreach (var ammo in allAmmo)
            {
                string ammoType = ammo.CustomData["ammoType"].ToString();
                if (!ammoDict.ContainsKey(ammoType))
                {
                    ammoDict[ammoType] = 0;
                }
                ammoDict[ammoType] += ammo.Quantity;
            }
            
            return ammoDict;
        }
        
        #endregion
        
        #region Stacking Operations
        
        /// <summary>
        /// Try to stack two items
        /// </summary>
        public bool TryStackItems(int sourceItemID, int targetItemID)
        {
            var sourceItem = _inventory.GetItemByID(sourceItemID);
            var targetItem = _inventory.GetItemByID(targetItemID);
            
            if (sourceItem == null || targetItem == null)
            {
                Debug.LogWarning("ConsumableManager: Source or target item not found");
                return false;
            }
            
            // Check if they can be stacked
            if (!CanStackItems(sourceItem, targetItem))
            {
                Debug.LogWarning($"ConsumableManager: Cannot stack {sourceItem.ItemName} with {targetItem.ItemName}");
                return false;
            }
            
            // Calculate the total quantity after stacking
            int totalQuantity = sourceItem.Quantity + targetItem.Quantity;
            int targetMaxStack = targetItem.MaxStackQuantity;
            
            if (totalQuantity <= targetMaxStack)
            {
                // Fully stack to target
                _inventory.UpdateItemQuantity(targetItemID, totalQuantity);
                _inventory.RemoveItemFromGrid(sourceItemID);
                
                Debug.Log($"ConsumableManager: Fully stacked {sourceItem.ItemName}. New quantity: {totalQuantity}");
                return true;
            }
            else
            {
                // Partially stack
                int amountToTransfer = targetMaxStack - targetItem.Quantity;
                _inventory.UpdateItemQuantity(targetItemID, targetMaxStack);
                _inventory.UpdateItemQuantity(sourceItemID, sourceItem.Quantity - amountToTransfer);
                
                Debug.Log($"ConsumableManager: Partially stacked {amountToTransfer}. Target: {targetMaxStack}, Source: {sourceItem.Quantity - amountToTransfer}");
                return true;
            }
        }
        
        /// <summary>
        /// Check if two items can be stacked
        /// </summary>
        public bool CanStackItems(GridItem sourceItem, GridItem targetItem)
        {
            if (sourceItem == null || targetItem == null)
                return false;
            
            // Must be the same type
            if (sourceItem.ItemType != targetItem.ItemType)
                return false;
            
            // Must be a consumable
            if (sourceItem.ItemType != ItemType.Consumable)
                return false;
            
            // Must be the same type of ammo
            if (sourceItem.CustomData.ContainsKey("ammoType") && targetItem.CustomData.ContainsKey("ammoType"))
            {
                return sourceItem.CustomData["ammoType"].ToString() == targetItem.CustomData["ammoType"].ToString();
            }
            
            // Other consumables check ItemName
            return sourceItem.ItemName == targetItem.ItemName;
        }
        
        #endregion
        
        #region Internal Helper Methods
        
        /// <summary>
        /// Create a new ammo stack
        /// </summary>
        private bool CreateNewAmmoStack(string ammoType, GridItem gridItem)
        {
            // Find empty space
            Vector2Int emptyPos = _inventory.FindEmptySpace(1, 1); // Ammo takes 1x1 grid
            if (emptyPos.x < 0 || emptyPos.y < 0)
            {
                Debug.LogWarning("ConsumableManager: No empty space for new ammo stack");
                return false;
            }
            
            // Create new ammo data
            var ammoData = new GridItem
            {
                ItemID = GenerateUniqueItemID(),
                ItemName = gridItem.ItemName,
                ItemType = ItemType.Consumable,
                Quantity = gridItem.Quantity,
                MaxStackQuantity = gridItem.MaxStackQuantity,
                GridWidth = gridItem.GridWidth,
                GridHeight = gridItem.GridHeight,
                GridPosition = emptyPos,
                Rotation = 0,
                ItemIcon = gridItem.ItemIcon,      
                ItemPrefab = gridItem.ItemPrefab,
                AssetPath = gridItem.AssetPath 
            };
            
            ammoData.CustomData["ammoType"] = ammoType;
            
            // Add to inventory
            return _inventory.AddItemToGrid(ammoData, emptyPos);
        }
        
        /// <summary>
        /// Generate unique item ID
        /// </summary>
        private int GenerateUniqueItemID()
        {
            // Use timestamp + random number to generate unique ID
            return (int)(System.DateTime.Now.Ticks & 0x7FFFFFFF) + Random.Range(0, 10000);
        }
        
        #endregion
        
        #region Cleanup
        
        public void Cleanup()
        {
            OnAmmoAdded = null;
            OnAmmoConsumed = null;
            OnAmmoCountChanged = null;
        }
        
        #endregion
    }
}

