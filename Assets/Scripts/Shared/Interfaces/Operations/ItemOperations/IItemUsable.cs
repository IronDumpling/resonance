using UnityEngine;
using Resonance.Utilities.GridSystem;

namespace Resonance.Shared.Interfaces.Operations
{
    /// <summary>
    /// Interface for items that can be used
    /// Example: Weapon (Equip), Tool (Activate), Module (Activate)
    /// </summary>
    public interface IItemUsable : IItemOperation
    {
        /// <summary>
        /// Check if this item can be used in current context
        /// </summary>
        bool CanUse(GridItem item);
        
        /// <summary>
        /// Use the item (equip weapon, activate tool, etc.)
        /// </summary>
        void Use(GridItem item);
        
        /// <summary>
        /// Get the display text for Use button (e.g., "Equip", "Unequip", "Use")
        /// </summary>
        string GetUseButtonText(GridItem item);
    }
}