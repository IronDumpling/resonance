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
            if (player == null)
            {
                Debug.Log("PlayerResonanceAction: Cannot start - player is null");
                return false;
            }

            // Must be in Normal state (not in other actions or death states)
            if (player.CurrentState != "Normal")
            {
                Debug.Log($"PlayerResonanceAction: Cannot start - player not in Normal state (current: {player.CurrentState})");
                return false;
            }

            // Must have at least 1 mental health slot available
            if (!player.CanConsumeSlot)
            {
                Debug.Log("PlayerResonanceAction: Cannot start - no mental health slots available");
                return false;
            }

            // Must have Core hitboxes in mental attack range
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null)
            {
                Debug.Log("PlayerResonanceAction: Cannot start - player service or current player is null");
                return false;
            }

            if (!playerService.CurrentPlayer.HasCoreHitboxesInMentalAttackRange())
            {
                Debug.Log("PlayerResonanceAction: Cannot start - no Core hitboxes in mental attack range");
                return false;
            }

            // Additional check: verify target cores are in valid states
            var mentalAttackTrigger = playerService.CurrentPlayer.GetComponentInChildren<MentalAttackTrigger>();
            if (mentalAttackTrigger != null)
            {
                var coreHitboxes = mentalAttackTrigger.CoreHitboxesInRange;
                bool hasValidCore = false;
                
                foreach (var core in coreHitboxes)
                {
                    if (core != null && IsValidTargetCore(core))
                    {
                        hasValidCore = true;
                        break;
                    }
                }
                
                if (!hasValidCore)
                {
                    Debug.Log("PlayerResonanceAction: Cannot start - no valid target cores (cores may be in invalid states)");
                    return false;
                }
            }

            Debug.Log("PlayerResonanceAction: All conditions met, can start");
            return true;
        }
        
        /// <summary>
        /// Check if a core hitbox is in a valid state for resonance
        /// </summary>
        /// <param name="coreHitbox">Core hitbox to check</param>
        /// <returns>True if core is valid for resonance</returns>
        private bool IsValidTargetCore(EnemyHitbox coreHitbox)
        {
            if (coreHitbox == null || !coreHitbox.IsInitialized)
                return false;
                
            // Check if the collider is enabled
            var collider = coreHitbox.GetComponent<Collider>();
            if (collider == null || !collider.enabled)
                return false;
                
            // Check if the enemy is in a valid state for resonance (not in attack state)
            var enemyMono = coreHitbox.GetEnemyMonoBehaviour();
            if (enemyMono == null)
                return false;
                
            var enemyController = enemyMono.Controller;
            if (enemyController == null)
                return false;
                
            // Valid states for resonance: Reviving or physical death (not Normal/Attack states)
            string enemyState = enemyController.CurrentState;
            bool isValidState = enemyState == "Reviving" || enemyState == "PhysicalDeath";
            
            if (!isValidState)
            {
                Debug.Log($"PlayerResonanceAction: Core {coreHitbox.name} in invalid state for resonance: {enemyState}");
            }
            
            return isValidState;
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

            // Check if target Core hitbox is still valid for resonance
            if (_targetCoreHitbox == null || !IsValidTargetCore(_targetCoreHitbox) || !IsTargetCoreStillInRange(_targetCoreHitbox))
            {
                // Core hitbox no longer valid or in range
                if (actionDuration >= MIN_ACTION_DURATION)
                {
                    if (_targetCoreHitbox == null)
                    {
                        Debug.Log("PlayerResonanceAction: Target Core hitbox is null, ending action");
                    }
                    else if (!IsValidTargetCore(_targetCoreHitbox))
                    {
                        Debug.Log("PlayerResonanceAction: Target Core hitbox no longer in valid state, ending action");
                    }
                    else
                    {
                        Debug.Log("PlayerResonanceAction: Target Core hitbox is no longer in range, ending action");
                    }
                    
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

            // Force refresh UI colors to fix BUG2 (second approach UI color not updating)
            var playerService = ServiceRegistry.Get<IPlayerService>();
            var playerMono = playerService?.CurrentPlayer;
            if (playerMono != null)
            {
                var mentalAttackTrigger = playerMono.GetComponentInChildren<MentalAttackTrigger>();
                mentalAttackTrigger?.ForceRefreshUIColors();
                Debug.Log("PlayerResonanceAction: Force refreshed UI colors after cleanup");
            }

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
            // TODO: Stop audio effects

            Debug.Log("PlayerResonanceAction: Stopped resonance effects");
        }
    }
}
