using UnityEngine;
using Resonance.Utilities;
using Resonance.Shared.Types;
using Resonance.Systems.GridSystem;
using Resonance.Shared.Interfaces.Operations;

namespace Resonance.Gameplay.Player.Inventory.Operations
{
    /// <summary>
    /// Operation handler for Ammo items (Consumable type)
    /// - Use: NOT ALLOWED (ammo is consumed automatically when shooting)
    /// - Drop: Permanently destroy ammo
    /// - Combine: Stack with other ammo of same type
    /// </summary>
    public class AmmoOperationHandler : BaseItemOperationHandler, IItemDroppable, IItemCombinable
    {
        public AmmoOperationHandler(
            PlayerInventory inventory,
            WeaponManager weaponManager,
            ConsumableManager consumableManager)
            : base(inventory, weaponManager, consumableManager)
        {
        }
        
        #region IItemDroppable Implementation
        
        public bool CanDrop(GridItem item)
        {
            // Ammo can always be dropped (permanently destroyed)
            if (item == null || item.ItemType != ItemType.Consumable)
            {
                return false;
            }
            
            // Check if it's actually ammo (not other consumables)
            if (!item.CustomData.ContainsKey("ammoType"))
            {
                return false;
            }
            
            return true;
        }
        
        public void Drop(GridItem item)
        {
            if (!CanDrop(item))
            {
                LogWarning($"Cannot drop item {item?.ItemName ?? "null"}");
                return;
            }
            
            string ammoType = item.CustomData["ammoType"].ToString();
            int quantity = item.Quantity;
            
            // Remove from inventory (this will trigger all necessary events)
            bool removed = Inventory.RemoveItemFromGrid(item.ItemID);
            
            if (removed)
            {
                Log($"Dropped {quantity} {ammoType} ammo (permanently destroyed)");
                
                // Play drop audio
                if (AudioService != null)
                {
                    // AudioService.PlaySFX2D(AudioClipType.ItemPickup, 0.5f, 0.8f); // Lower pitch for drop
                }
            }
            else
            {
                LogError($"Failed to drop ammo: {item.ItemName}");
            }
        }
        
        #endregion
        
        #region IItemCombinable Implementation
        
        public bool CanCombine(GridItem sourceItem, GridItem targetItem)
        {
            // Use ConsumableManager's stack logic
            return ConsumableManager.CanStackItems(sourceItem, targetItem);
        }
        
        public void Combine(GridItem sourceItem, GridItem targetItem)
        {
            if (!CanCombine(sourceItem, targetItem))
            {
                LogWarning($"Cannot combine {sourceItem?.ItemName} with {targetItem?.ItemName}");
                return;
            }
            
            // Use ConsumableManager's stack logic
            bool success = ConsumableManager.TryStackItems(sourceItem.ItemID, targetItem.ItemID);
            
            if (success)
            {
                Log($"Combined {sourceItem.ItemName} with {targetItem.ItemName}");
            }
            else
            {
                LogError($"Failed to combine items");
            }
        }
        
        #endregion
    }
}