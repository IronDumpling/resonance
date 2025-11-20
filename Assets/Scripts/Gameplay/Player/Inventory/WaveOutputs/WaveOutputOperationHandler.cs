using UnityEngine;
using Resonance.Shared.Interfaces.Operations;
using Resonance.Utilities;
using Resonance.Shared.Types;
using Resonance.Systems.GridSystem;

namespace Resonance.Gameplay.Player.Inventory.Operations
{
    /// <summary>
    /// Operation handler for WaveOutput items
    /// - Use: Equip/Unequip weapon
    /// - Drop: NOT ALLOWED (weapons cannot be dropped)
    /// - Combine: Future feature (weapon modding/upgrading)
    /// </summary>
    public class WaveOutputOperationHandler : BaseItemOperationHandler, IItemUsable
    {
        public WaveOutputOperationHandler(
            PlayerInventory inventory,
            WaveOutputManager waveOutputManager,
            ConsumableManager consumableManager)
            : base(inventory, waveOutputManager, consumableManager)
        {
        }
        
        #region IItemUsable Implementation
        
        public bool CanUse(GridItem item)
        {
            // WaveOutputs can always be used (equipped/unequipped)
            if (item == null || item.ItemType != ItemType.WaveOutput)
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
                // Unequip output
                WaveOutputManager.UnequipOutput();
                Log($"Unequipped output: {item.ItemName}");
            }
            else
            {
                // Equip output
                bool success = WaveOutputManager.EquipOutput(item.ItemID);
                if (success)
                {
                    Log($"Equipped output: {item.ItemName}");
                }
                else
                {
                    LogError($"Failed to equip output: {item.ItemName}");
                }
            }
        }
        
        public string GetUseButtonText(GridItem item)
        {
            if (item == null || item.ItemType != ItemType.WaveOutput)
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