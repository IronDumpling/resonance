using UnityEngine;
using Resonance.Player.Core;
using Resonance.Player.Data;
using Resonance.Interfaces.Objects;
using Resonance.Enemies;
using Resonance.Core;
using Resonance.Interfaces.Services;
using Resonance.Utilities;

namespace Resonance.Player.Actions
{
    /// <summary>
    /// Player Resonance Action - triggered by short press F when Core hitboxes are in mental attack range
    /// Conditions: PlayerNormalState, MentalHealth >= 1 slot, Core type EnemyHitbox with enabled collider in MentalAttackRange
    /// Behavior: Player cannot move, is invulnerable to physical damage, consumes 1 MentalHealth slot
    /// End condition: Target Core hitbox collider becomes disabled or exits range
    /// </summary>
    public class PlayerResonanceAction : IPlayerAction
    {
        // Static events for state machine integration
        public static event System.Action<EnemyHitbox> OnResonanceActionStarted;
        public static event System.Action OnResonanceActionEnded;

        // Action properties
        public string Name => "Resonance";
        public bool BlocksMovement => true;
        public bool ProvidesInvulnerability => true;
        public bool CanInterrupt => false; // Cannot be interrupted

        // Runtime state
        private bool _isActive = false;
        private bool _isFinished = false;
        private EnemyHitbox _targetCoreHitbox = null;
        private float _actionStartTime = 0f;

        private PlayerController _player;

        // Configuration
        private const float MIN_ACTION_DURATION = 0.5f; // Minimum action duration for feedback
        private const float MAX_ACTION_DURATION = 10f; // Safety timeout

        public bool IsFinished => _isFinished;

        /// <summary>
        /// Check if the ResonanceAction can start
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <returns>True if all conditions are met</returns>
        public bool CanStart(PlayerController player)
        {
            if (player == null) return false;

            // Must be in Normal state (not in other actions or death states)
            if (player.CurrentState != "Normal") return false;

            // Must have at least 1 mental health slot available
            if (!player.CanConsumeSlot) return false;

            // Must have Core hitboxes in mental attack range
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null) return false;

            if (!playerService.CurrentPlayer.HasCoreHitboxesInMentalAttackRange()) return false;

            Debug.Log("PlayerResonanceAction: All conditions met, can start");
            return true;
        }

        /// <summary>
        /// Start the resonance action
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void Start(PlayerController player)
        {
            if (player == null)
            {
                Debug.LogError("PlayerResonanceAction: Cannot start with null player");
                return;
            }

            _player = player;

            // Find target Core hitbox
            _targetCoreHitbox = FindTargetCoreHitbox();
            if (_targetCoreHitbox == null)
            {
                Debug.LogWarning("PlayerResonanceAction: No valid target Core hitbox found");
                _isFinished = true;
                return;
            }

            // Consume mental health slot
            if (!player.ConsumeSlot())
            {
                Debug.LogWarning("PlayerResonanceAction: Failed to consume mental health slot");
                _isFinished = true;
                return;
            }

            // Initialize action state
            _isActive = true;
            _isFinished = false;
            _actionStartTime = Time.time;

            // Subscribe to target Core hitbox events
            if (_targetCoreHitbox != null)
            {
                _targetCoreHitbox.OnColliderDisabled += OnTargetCoreColliderDisabled;
                Debug.Log($"PlayerResonanceAction: Subscribed to collider events for core hitbox {_targetCoreHitbox.name}");
            }

            // Play resonance audio/effects
            PlayResonanceEffects();

            // Trigger the resonance started event for state machine
            OnResonanceActionStarted?.Invoke(_targetCoreHitbox);

            Debug.Log($"PlayerResonanceAction: Started with target Core hitbox {_targetCoreHitbox.name}");
        }

        /// <summary>
        /// Update the resonance action each frame
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        public void Update(PlayerController player, float deltaTime)
        {
            if (!_isActive || _isFinished) return;

            float currentTime = Time.time;
            float actionDuration = currentTime - _actionStartTime;

            // Safety timeout
            if (actionDuration > MAX_ACTION_DURATION)
            {
                Debug.LogWarning("PlayerResonanceAction: Timed out after maximum duration");
                _isFinished = true;
                CleanupAction();
                return; 
            }

            // Check if target Core hitbox is still in range (collider state changes are handled by events)
            if (_targetCoreHitbox == null || !IsTargetCoreStillInRange(_targetCoreHitbox))
            {
                // Core hitbox no longer in range
                if (actionDuration >= MIN_ACTION_DURATION)
                {
                    Debug.Log("PlayerResonanceAction: Target Core hitbox is no longer in range, ending action");
                    _isFinished = true;
                    CleanupAction();
                    return;
                }
                // If minimum duration not met, continue until minimum time is reached
            }

            // Update resonance effects (visual feedback, QTE UI placeholder, etc.)
            UpdateResonanceEffects(deltaTime);
        }

        /// <summary>
        /// Cancel the resonance action (should not be called since it cannot be interrupted)
        /// </summary>
        public void Cancel(PlayerController player)
        {
            if (_isActive)
            {
                Debug.Log("PlayerResonanceAction: Cancelled");
                CleanupAction();
            }
        }

        /// <summary>
        /// Called when player takes damage (should not interrupt this action)
        /// </summary>
        public void OnDamageTaken(PlayerController player)
        {
            // This action provides invulnerability and cannot be interrupted
            // Log for debugging purposes
            Debug.Log("PlayerResonanceAction: Damage taken but action is invulnerable");
        }

        /// <summary>
        /// Find the target Core hitbox for resonance action
        /// </summary>
        /// <returns>The target Core hitbox or null if none found</returns>
        private EnemyHitbox FindTargetCoreHitbox()
        {
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null) return null;

            // Get the closest Core hitbox from MentalAttackTrigger
            var playerMono = playerService.CurrentPlayer;
            
            // Get the closest Core hitbox directly
            var closestCoreHitbox = playerMono.GetClosestCoreHitbox();
            if (closestCoreHitbox != null)
            {
                Debug.Log($"PlayerResonanceAction: Found target Core hitbox {closestCoreHitbox.name}");
                return closestCoreHitbox;
            }

            Debug.Log("PlayerResonanceAction: No Core hitboxes found in range");
            return null;
        }

        /// <summary>
        /// Check if the target Core hitbox is still in range (collider state is handled by events)
        /// </summary>
        /// <param name="hitbox">Core hitbox to check</param>
        /// <returns>True if Core hitbox is still in range</returns>
        private bool IsTargetCoreStillInRange(EnemyHitbox hitbox)
        {
            if (hitbox == null) return false;

            // Check if hitbox is still initialized and is Core type
            if (!hitbox.IsInitialized || hitbox.type != EnemyHitboxType.Core) return false;

            // Check if still in range (through PlayerService)
            var playerService = ServiceRegistry.Get<IPlayerService>();
            var playerMono = playerService?.CurrentPlayer;
            if (playerMono == null) return false;

            // Check if this specific hitbox is still being tracked
            var coreHitboxesInRange = playerMono.GetCoreHitboxesInRange();
            return coreHitboxesInRange.Contains(hitbox);
        }
        
        /// <summary>
        /// Handle target core hitbox collider disabled event
        /// </summary>
        /// <param name="hitbox">The hitbox that was disabled</param>
        private void OnTargetCoreColliderDisabled(EnemyHitbox hitbox)
        {
            if (hitbox == _targetCoreHitbox)
            {
                Debug.Log("PlayerResonanceAction: Target core collider disabled - ending resonance action");
                
                // Check minimum duration before ending
                float actionDuration = Time.time - _actionStartTime;
                if (actionDuration >= MIN_ACTION_DURATION)
                {
                    _isFinished = true;
                    CleanupAction();
                }
                else
                {
                    Debug.Log($"PlayerResonanceAction: Minimum duration not met ({actionDuration:F2}s < {MIN_ACTION_DURATION}s), continuing");
                }
            }
        }

        /// <summary>
        /// Play resonance visual and audio effects
        /// </summary>
        private void PlayResonanceEffects()
        {
            // Play resonance start audio
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService != null)
            {
                // TODO: Add specific resonance audio clips
                audioService.PlaySFX2D(AudioClipType.PlayerHit, 0.6f, 0.8f); // Placeholder audio
            }

            // TODO: Add visual effects (particles, screen effects, etc.)
            // TODO: Show QTE UI placeholder
            Debug.Log("PlayerResonanceAction: Playing resonance effects (placeholder)");
        }

        /// <summary>
        /// Update ongoing resonance effects
        /// </summary>  
        /// <param name="deltaTime">Time since last frame</param>
        private void UpdateResonanceEffects(float deltaTime)
        {
            // TODO: Update visual effects intensity
            // TODO: Update audio effects

            // Placeholder implementation
            float actionDuration = Time.time - _actionStartTime;
            if (actionDuration > 0.1f && Mathf.FloorToInt(actionDuration * 4) % 2 == 0)
            {
                // Simple feedback every 0.25 seconds
                // Debug.Log($"PlayerResonanceAction: Resonance active for {actionDuration:F1}s");
            }
        }

        /// <summary>
        /// Clean up the action when it ends
        /// </summary>
        private void CleanupAction()
        {
            // Prevent multiple cleanup calls
            if (!_isActive) return;
            
            _isActive = false;
            _isFinished = true;

            // Unsubscribe from Core hitbox events
            if (_targetCoreHitbox != null)
            {
                _targetCoreHitbox.OnColliderDisabled -= OnTargetCoreColliderDisabled;
                Debug.Log("PlayerResonanceAction: Unsubscribed from core hitbox collider events");
            }

            // Stop effects
            StopResonanceEffects();

            // Trigger the resonance ended event for state machine
            OnResonanceActionEnded?.Invoke();

            // Clear target reference
            _targetCoreHitbox = null;

            Debug.Log("PlayerResonanceAction: Cleaned up");
        }

        /// <summary>
        /// Stop resonance effects
        /// </summary>
        private void StopResonanceEffects()
        {
            // TODO: Stop visual effects
            // TODO: Hide QTE UI
            // TODO: Stop audio effects

            Debug.Log("PlayerResonanceAction: Stopped resonance effects");
        }
    }
}
