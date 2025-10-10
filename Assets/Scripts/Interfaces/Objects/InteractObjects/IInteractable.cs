using UnityEngine;

namespace Resonance.Interfaces.Objects
{
    /// <summary>
    /// Base interface for objects that can be interacted with by the player
    /// Simplified version - interaction flow is handled by InteractionService
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Check if this object can currently be interacted with
        /// </summary>
        /// <returns>True if interaction is possible</returns>
        bool CanInteract();

        /// <summary>
        /// Get the interaction duration in seconds
        /// </summary>
        /// <returns>Duration of the interaction</returns>
        float GetInteractionDuration();

        /// <summary>
        /// Get the world position of this interactable object
        /// </summary>
        /// <returns>World position</returns>
        Vector3 GetPosition();

        /// <summary>
        /// Get a descriptive name for this interactable
        /// Used for debugging and UI
        /// </summary>
        /// <returns>Name or description</returns>
        string GetInteractableName();
    }
}
