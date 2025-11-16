using UnityEngine;
using Resonance.Utilities.GridSystem;

namespace Resonance.Shared.Interfaces.Objects
{
    /// <summary>
    /// Interface for items that can be picked up and added to inventory
    /// Extends IInteractable with pickup-specific logic
    /// </summary>
    public interface IPickupable : IInteractable
    {
        /// <summary>
        /// Try to add this item to player inventory
        /// Returns true if successfully added, false if inventory is full
        /// </summary>
        /// <param name="gridItem">The GridItem data to add to inventory</param>
        /// <param name="failureReason">Reason why pickup failed (e.g., "No space in inventory")</param>
        /// <returns>True if item was added to inventory, false otherwise</returns>
        bool TryAddToInventory(out GridItem gridItem, out string failureReason);
        
        /// <summary>
        /// Called when pickup fails due to full inventory
        /// This allows the item to stay in the world and wait for player to free up space
        /// </summary>
        void OnInventoryFull();
        
        /// <summary>
        /// Clean up this item from the world (called after successful pickup)
        /// </summary>
        void DestroyPickupItem();
    }
}