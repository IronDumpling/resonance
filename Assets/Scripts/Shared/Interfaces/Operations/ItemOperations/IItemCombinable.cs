using UnityEngine;
using Resonance.Systems.GridSystem;

namespace Resonance.Shared.Interfaces.Operations
{
    /// <summary>
    /// Interface for items that can be combined with other items
    /// Example: Ammo (stack), Weapon parts, Crafting materials
    /// </summary>
    public interface IItemCombinable : IItemOperation
    {
        /// <summary>
        /// Check if this item can be combined with target item
        /// </summary>
        bool CanCombine(GridItem sourceItem, GridItem targetItem);
        
        /// <summary>
        /// Combine source item with target item
        /// </summary>
        void Combine(GridItem sourceItem, GridItem targetItem);
    }
}
