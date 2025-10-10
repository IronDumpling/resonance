using UnityEngine;

namespace Resonance.Interfaces.Objects
{
    /// <summary>
    /// Interface for items that can be read to display information
    /// Extends IInteractable with reading-specific logic
    /// </summary>
    public interface IReadable : IInteractable
    {
        /// <summary>
        /// Get the information data to display
        /// Returns IInfoable interface for flexibility
        /// </summary>
        /// <returns>Information data provider</returns>
        IInfoable GetInfoable();
    }
}