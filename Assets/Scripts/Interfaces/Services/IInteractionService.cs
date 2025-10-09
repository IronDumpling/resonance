using UnityEngine;

namespace Resonance.Interfaces.Services
{
    /// <summary>
    /// Interaction service interface
    /// Manage the interaction between the player and the interactable objects in the scene
    /// </summary>
    public interface IInteractionService : IGameService
    {
        /// <summary>
        /// Current interactable object
        /// </summary>
        GameObject CurrentInteractable { get; }
        
        /// <summary>
        /// Whether there is an interactable object
        /// </summary>
        bool HasInteractable { get; }
        
        /// <summary>
        /// Current interaction text
        /// </summary>
        string InteractionText { get; }
        
        /// <summary>
        /// Register the interactable object
        /// </summary>
        /// <param name="interactable">Interactable object</param>
        void RegisterInteractable(GameObject interactable);
        
        /// <summary>
        /// Remove the interactable object
        /// </summary>
        /// <param name="interactable">Interactable object</param>
        void UnregisterInteractable(GameObject interactable);
        
        /// <summary>
        /// Set the current interactable object
        /// </summary>
        /// <param name="interactable">Interactable object</param>
        /// <param name="interactionText">Interaction text</param>
        void SetCurrentInteractable(GameObject interactable, string interactionText = "");
        
        /// <summary>
        /// Clear the current interactable object
        /// </summary>
        void ClearCurrentInteractable();

        /// <summary>
        /// Get the nearest interactable object
        /// </summary>
        /// <returns>The nearest interactable object, or null if none</returns>
        Interfaces.Objects.IInteractable GetNearestInteractable();

        /// <summary>
        /// Handle interactable object entering range
        /// </summary>
        /// <param name="gameObject">Game object</param>
        /// <param name="interactable">Interactable object</param>
        void OnInteractableEnteredRange(GameObject gameObject, Interfaces.Objects.IInteractable interactable);

        /// <summary>
        /// Handle interactable object leaving range
        /// </summary>
        /// <param name="gameObject">Game object</param>
        /// <param name="interactable">Interactable object</param>
        void OnInteractableExitedRange(GameObject gameObject, Interfaces.Objects.IInteractable interactable);
        
        /// <summary>
        /// Complete interaction with the specified interactable object
        /// </summary>
        /// <param name="interactable">The interactable object to interact with</param>
        void CompleteInteraction(Interfaces.Objects.IInteractable interactable);
        
        // Events
        event System.Action<GameObject, string> OnInteractableChanged; // Interactable object, interaction text
    }
}
