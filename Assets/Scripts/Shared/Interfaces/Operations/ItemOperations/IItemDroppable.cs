using UnityEngine;
using Resonance.Utilities.GridSystem;

namespace Resonance.Shared.Interfaces.Operations
{
    /// <summary>
    /// Interface for items that can be dropped (permanently destroyed)
    /// Example: Ammo, Tool
    /// </summary>
    public interface IItemDroppable : IItemOperation
    {
        /// <summary>
        /// Check if this item can be dropped in current context
        /// </summary>
        bool CanDrop(GridItem item);
        
        /// <summary>
        /// Drop the item (permanently destroy it)
        /// </summary>
        void Drop(GridItem item);
    }
}
