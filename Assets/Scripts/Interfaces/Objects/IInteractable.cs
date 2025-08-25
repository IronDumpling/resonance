using UnityEngine;

namespace Resonance.Interfaces.Objects
{
    /// <summary>
    /// Interface for objects that can be interacted with by the player
    /// Used by the new PlayerInteractAction system
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
        /// Start the interaction process
        /// Called when the player begins interacting
        /// </summary>
        void StartInteraction();

        /// <summary>
        /// Complete the interaction successfully
        /// Called when the interaction duration is reached
        /// </summary>
        void CompleteInteraction();

        /// <summary>
        /// Cancel the interaction
        /// Called when the interaction is interrupted
        /// </summary>
        void CancelInteraction();

        /// <summary>
        /// Get a descriptive name for this interactable
        /// Used for debugging and UI
        /// </summary>
        /// <returns>Name or description</returns>
        string GetInteractableName();
    }
}
