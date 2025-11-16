using UnityEngine;
using Resonance.Shared.Interfaces.Operations;
using Resonance.Utilities;
using Resonance.Shared.Types;
using Resonance.Utilities.GridSystem;

namespace Resonance.Gameplay.Player.Inventory.Operations
{
    /// <summary>
    /// Operation handler for Weapon items
    /// - Use: Equip/Unequip weapon
    /// - Drop: NOT ALLOWED (weapons cannot be dropped)
    /// - Combine: Future feature (weapon modding/upgrading)
    /// </summary>
    public class WeaponOperationHandler : BaseItemOperationHandler, IItemUsable
    {
        public WeaponOperationHandler(
            PlayerInventory inventory,
            WeaponManager weaponManager,
            ConsumableManager consumableManager)
            : base(inventory, weaponManager, consumableManager)
        {
        }
        
        #region IItemUsable Implementation
        
        public bool CanUse(GridItem item)
        {
            // Weapons can always be used (equipped/unequipped)
            if (item == null || item.ItemType != ItemType.Weapon)
            {
                return false;
            }
            
            return true;
        }
        
        public void Use(GridItem item)
        {
            if (!CanUse(item))
            {
                LogWarning($"Cannot use item {item?.ItemName ?? "null"}");
                return;
            }
            
            // Check if weapon is currently equipped
            bool isEquipped = item.IsEquipped;
            
            if (isEquipped)
            {
                // Unequip weapon
                WeaponManager.UnequipWeapon();
                Log($"Unequipped weapon: {item.ItemName}");
            }
            else
            {
                // Equip weapon
                bool success = WeaponManager.EquipWeapon(item.ItemID);
                if (success)
                {
                    Log($"Equipped weapon: {item.ItemName}");
                }
                else
                {
                    LogError($"Failed to equip weapon: {item.ItemName}");
                }
            }
        }
        
        public string GetUseButtonText(GridItem item)
        {
            if (item == null || item.ItemType != ItemType.Weapon)
            {
                return "Use";
            }
            
            // Check if weapon is equipped
            bool isEquipped = item.IsEquipped;
            return isEquipped ? "Unequip" : "Equip";
        }
        
        #endregion
    }
}