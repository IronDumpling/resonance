using UnityEngine;
using Resonance.Player.Core;
using Resonance.Player.Data;
using Resonance.Interfaces.Objects;
using Resonance.Interfaces.Services;
using Resonance.Core;
using Resonance.Utilities;

namespace Resonance.Player.Actions
{
    /// <summary>
    /// Player Interact Action - triggered by E key for environmental interactions
    /// This replaces the functionality from PlayerInteractingState.cs
    /// Conditions: PlayerNormalState, valid interactable object in range
    /// Behavior: Player cannot move, performs interaction with target object
    /// End condition: Interaction completes or is cancelled
    /// </summary>
    public class PlayerInteractAction : IPlayerAction
    {
        // Action properties
        public string Name => "Interact";
        public bool BlocksMovement => true;
        public bool ProvidesInvulnerability => false;
        public bool CanInterrupt => false; // Cannot be interrupted once started

        // Runtime state
        private bool _isActive = false;
        private bool _isFinished = false;
        private IInteractable _targetInteractable = null;
        private float _actionStartTime = 0f;
        private float _interactionDuration = 0f;

        // Configuration
        private const float DEFAULT_INTERACTION_DURATION = 1.0f;
        private const float MAX_INTERACTION_DURATION = 10f;
        private const float INTERACTION_RANGE = 2f; // Maximum interaction distance

        public bool IsFinished => _isFinished;

        /// <summary>
        /// Check if the InteractAction can start
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <returns>True if all conditions are met</returns>
        public bool CanStart(PlayerController player)
        {
            if (player == null) return false;

            // Must be in Normal state (not in other actions or death states)
            if (player.CurrentState != "Normal") return false;

            // Must be physically alive to interact
            if (!player.IsPhysicallyAlive) return false;

            // Find a valid interactable object in range
            _targetInteractable = FindNearestInteractable();
            if (_targetInteractable == null)
            {
                Debug.Log("PlayerInteractAction: No interactable objects in range");
                return false;
            }

            // Check if the interactable can be used
            if (!_targetInteractable.CanInteract())
            {
                Debug.Log($"PlayerInteractAction: Interactable {_targetInteractable.GetInteractableName()} cannot be used");
                return false;
            }

            Debug.Log($"PlayerInteractAction: Can interact with {_targetInteractable.GetInteractableName()}");
            return true;
        }

        /// <summary>
        /// Start the interact action
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void Start(PlayerController player)
        {
            if (player == null)
            {
                Debug.LogError("PlayerInteractAction: Cannot start with null player");
                return;
            }

            if (_targetInteractable == null)
            {
                Debug.LogError("PlayerInteractAction: No target interactable found");
                _isFinished = true;
                return;
            }

            // Initialize action state
            _isActive = true;
            _isFinished = false;
            _actionStartTime = Time.time;
            
            // Get interaction duration from the interactable
            _interactionDuration = _targetInteractable.GetInteractionDuration();
            if (_interactionDuration <= 0f)
            {
                _interactionDuration = DEFAULT_INTERACTION_DURATION;
            }

            // Start the interaction
            _targetInteractable.StartInteraction();

            // Play interaction start effects
            PlayInteractionStartEffects(player);

            Debug.Log($"PlayerInteractAction: Started interaction with {_targetInteractable.GetInteractableName()} (duration: {_interactionDuration}s)");
        }

        /// <summary>
        /// Update the interact action each frame
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <param name="deltaTime">Time since last frame</param>
        public void Update(PlayerController player, float deltaTime)
        {
            if (!_isActive || _isFinished) 
            {
                if (!_isActive) Debug.Log("PlayerInteractAction: Update skipped - not active");
                if (_isFinished) Debug.Log("PlayerInteractAction: Update skipped - finished");
                return;
            }

            float currentTime = Time.time;
            float actionDuration = currentTime - _actionStartTime;
            
            Debug.Log($"PlayerInteractAction: Update - duration: {actionDuration:F2}s / {_interactionDuration:F2}s");

            // Safety timeout
            if (actionDuration > MAX_INTERACTION_DURATION)
            {
                Debug.LogWarning("PlayerInteractAction: Timed out after maximum duration");
                _isFinished = true;
                return;
            }

            // Check if interaction duration has been reached
            if (actionDuration >= _interactionDuration)
            {
                Debug.Log($"PlayerInteractAction: Duration reached! Completing interaction...");
                // Complete the interaction
                CompleteInteraction(player);
                _isFinished = true;
                return;
            }

            // Check if target is still valid
            if (_targetInteractable == null || !_targetInteractable.CanInteract())
            {
                Debug.Log("PlayerInteractAction: Target interactable is no longer valid");
                _isFinished = true;
                return;
            }

            // Check if player moved too far away
            if (!IsInInteractionRange(_targetInteractable))
            {
                Debug.Log("PlayerInteractAction: Player moved out of interaction range");
                _isFinished = true;
                return;
            }

            // Update interaction progress
            // float progress = actionDuration / _interactionDuration;
            // _targetInteractable.UpdateInteractionProgress(progress);

            // Update visual effects
            // UpdateInteractionEffects(player, deltaTime, progress);
        }

        /// <summary>
        /// Cancel the interact action
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void Cancel(PlayerController player)
        {
            if (_isActive)
            {
                Debug.Log("PlayerInteractAction: Cancelled");
                
                // Cancel the interaction on the target
                _targetInteractable?.CancelInteraction();
                
                CleanupAction(player);
            }
        }

        /// <summary>
        /// Called when player takes damage (should not interrupt this action)
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void OnDamageTaken(PlayerController player)
        {
            // This action cannot be interrupted by damage
            Debug.Log("PlayerInteractAction: Damage taken but interaction continues");
        }

        /// <summary>
        /// Complete the interaction successfully
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void CompleteInteraction(PlayerController player)
        {
            if (_targetInteractable != null)
            {
                // Trigger the interaction effect
                _targetInteractable.CompleteInteraction();
                
                Debug.Log($"PlayerInteractAction: Completed interaction with {_targetInteractable.GetInteractableName()}");
            }

            // Play completion effects
            PlayInteractionCompleteEffects(player);
        }

        /// <summary>
        /// Find the nearest interactable object in range
        /// </summary>
        /// <returns>The nearest interactable or null if none found</returns>
        private IInteractable FindNearestInteractable()
        {
            // Get interaction service
            var interactionService = ServiceRegistry.Get<IInteractionService>();
            if (interactionService == null) return null;

            return interactionService.GetNearestInteractable();
        }

        /// <summary>
        /// Check if the player is still in range of the interactable
        /// </summary>
        /// <param name="interactable">The interactable to check</param>
        /// <returns>True if in range</returns>
        private bool IsInInteractionRange(IInteractable interactable)
        {
            if (interactable == null) return false;

            // Get player position through PlayerService
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null) return false;

            Vector3 playerPosition = playerService.CurrentPlayer.transform.position;
            Vector3 interactablePosition = interactable.GetPosition();
            
            float distance = Vector3.Distance(playerPosition, interactablePosition);
            return distance <= INTERACTION_RANGE;
        }

        /// <summary>
        /// Play interaction start effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void PlayInteractionStartEffects(PlayerController player)
        {
            // Play interaction start audio
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService != null)
            {
                // TODO: Add specific interaction audio clips
                audioService.PlaySFX2D(AudioClipType.PlayerHit, 0.5f, 1.2f); // Placeholder audio
            }

            // TODO: Add visual effects (interaction progress UI, highlight, etc.)
            Debug.Log("PlayerInteractAction: Playing interaction start effects (placeholder)");
        }

        /// <summary>
        /// Play interaction complete effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void PlayInteractionCompleteEffects(PlayerController player)
        {
            // Play completion audio
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService != null)
            {
                // TODO: Add specific completion audio clips
                audioService.PlaySFX2D(AudioClipType.PlayerHit, 0.7f, 1.0f); // Placeholder audio
            }

            // TODO: Add completion visual effects
            Debug.Log("PlayerInteractAction: Playing interaction complete effects (placeholder)");
        }

        /// <summary>
        /// Update ongoing interaction effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <param name="deltaTime">Time since last frame</param>
        /// <param name="progress">Interaction progress (0-1)</param>
        private void UpdateInteractionEffects(PlayerController player, float deltaTime, float progress)
        {
            // TODO: Update interaction progress UI
            // TODO: Update visual effects based on progress
            // TODO: Update audio effects

            // Placeholder implementation
            if (Mathf.FloorToInt(progress * 10) % 2 == 0)
            {
                // Simple progress feedback
                // Debug.Log($"PlayerInteractAction: Interaction progress: {progress:P0}");
            }
        }

        /// <summary>
        /// Clean up the action when it ends
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void CleanupAction(PlayerController player)
        {
            _isActive = false;
            _isFinished = true;

            // Stop effects
            StopInteractionEffects(player);

            // Clear target reference
            _targetInteractable = null;

            Debug.Log("PlayerInteractAction: Cleaned up");
        }

        /// <summary>
        /// Stop interaction effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void StopInteractionEffects(PlayerController player)
        {
            // TODO: Stop visual effects
            // TODO: Stop audio effects
            // TODO: Hide interaction UI

            Debug.Log("PlayerInteractAction: Stopped interaction effects");
        }
    }
}
