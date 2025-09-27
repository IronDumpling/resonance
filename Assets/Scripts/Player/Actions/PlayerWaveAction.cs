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
    /// Player Wave Action - triggered by short press F when Core hitboxes are in core attack range
    /// Conditions: PlayerNormalState, CoreHealth >= 1 slot, Core type EnemyHitbox with enabled collider in CoreAttackRange
    /// Behavior: Player cannot move, is invulnerable to health damage, consumes 1 CoreHealth slot
    /// End condition: Target Core hitbox collider becomes disabled or exits range
    /// </summary>
    public class PlayerWaveAction : IPlayerAction
    {
        // Static events for state machine integration
        public static event System.Action<EnemyHitbox> OnWaveActionStarted;
        public static event System.Action OnWaveActionEnded;

        // Action properties
        public string Name => "Wave";
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
        /// Check if the WaveAction can start
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <returns>True if all conditions are met</returns>
        public bool CanStart(PlayerController player)
        {
            if (player == null)
            {
                Debug.Log("PlayerWaveAction: Cannot start - player is null");
                return false;
            }

            // Must be in Normal state (not in other actions or death states)
            if (player.CurrentState != "Normal")
            {
                Debug.Log($"PlayerWaveAction: Cannot start - player not in Normal state (current: {player.CurrentState})");
                return false;
            }

            // Must have at least 1 core health slot available
            if (!player.CanConsumeSlot)
            {
                Debug.Log("PlayerWaveAction: Cannot start - no core health slots available");
                return false;
            }

            // Must have Core hitboxes in core attack range
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null)
            {
                Debug.Log("PlayerWaveAction: Cannot start - player service or current player is null");
                return false;
            }

            if (!playerService.CurrentPlayer.HasCoreHitboxesInCoreAttackRange())
            {
                Debug.Log("PlayerWaveAction: Cannot start - no Core hitboxes in core attack range");
                return false;
            }

            // Additional check: verify target cores are in valid states
            var coreAttackTrigger = playerService.CurrentPlayer.GetComponentInChildren<CoreAttackTrigger>();
            if (coreAttackTrigger != null)
            {
                var coreHitboxes = coreAttackTrigger.CoreHitboxesInRange;
                Debug.Log($"PlayerWaveAction: Found {coreHitboxes.Count} core hitboxes in range");
                
                bool hasValidCore = false;
                
                foreach (var core in coreHitboxes)
                {
                    if (core != null)
                    {
                        bool isValid = IsValidTargetCore(core);
                        Debug.Log($"PlayerWaveAction: Core {core.name} validity check: {isValid}");
                        
                        if (isValid)
                        {
                            hasValidCore = true;
                            break;
                        }
                    }
                    else
                    {
                        Debug.Log("PlayerWaveAction: Found null core in range list");
                    }
                }
                
                if (!hasValidCore)
                {
                    Debug.Log("PlayerWaveAction: Cannot start - no valid target cores (cores may be in invalid states)");
                    return false;
                }
            }
            else
            {
                Debug.Log("PlayerWaveAction: Cannot start - CoreAttackTrigger not found");
                return false;
            }

            Debug.Log("PlayerWaveAction: All conditions met, can start");
            return true;
        }
        
        /// <summary>
        /// Check if a core hitbox is in a valid state for resonance
        /// </summary>
        /// <param name="coreHitbox">Core hitbox to check</param>
        /// <returns>True if core is valid for resonance</returns>
        private bool IsValidTargetCore(EnemyHitbox coreHitbox)
        {
            if (coreHitbox == null)
            {
                Debug.Log("PlayerWaveAction: IsValidTargetCore - coreHitbox is null");
                return false;
            }
                
            if (!coreHitbox.IsInitialized)
            {
                Debug.Log($"PlayerWaveAction: IsValidTargetCore - {coreHitbox.name} not initialized");
                return false;
            }
                
            // Check if the collider is enabled
            var collider = coreHitbox.GetComponent<Collider>();
            if (collider == null)
            {
                Debug.Log($"PlayerWaveAction: IsValidTargetCore - {coreHitbox.name} has no collider");
                return false;
            }
            
            if (!collider.enabled)
            {
                Debug.Log($"PlayerWaveAction: IsValidTargetCore - {coreHitbox.name} collider is disabled");
                return false;
            }
                
            // Check if the enemy is in a valid state for resonance (not in attack state)
            var enemyMono = coreHitbox.GetEnemyMonoBehaviour();
            if (enemyMono == null)
            {
                Debug.Log($"PlayerWaveAction: IsValidTargetCore - {coreHitbox.name} has no EnemyMonoBehaviour");
                return false;
            }
                
            var enemyController = enemyMono.Controller;
            if (enemyController == null)
            {
                Debug.Log($"PlayerWaveAction: IsValidTargetCore - {coreHitbox.name} has no EnemyController");
                return false;
            }
                
            // Valid states for resonance: Reviving or health death (not Normal/Attack states)
            string enemyState = enemyController.CurrentState;
            bool isValidState = enemyState == "Reviving" || enemyState == "PhysicalDeath";
            
            // Debug.Log($"PlayerWaveAction: IsValidTargetCore - {coreHitbox.name} state: {enemyState}, valid: {isValidState}");
            
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
                Debug.LogError("PlayerWaveAction: Cannot start with null player");
                return;
            }

            _player = player;

            // Find target Core hitbox
            _targetCoreHitbox = FindTargetCoreHitbox();
            if (_targetCoreHitbox == null)
            {
                Debug.LogWarning("PlayerWaveAction: No valid target Core hitbox found");
                _isFinished = true;
                return;
            }

            // Consume core health slot
            if (!player.ConsumeSlot())
            {
                Debug.LogWarning("PlayerWaveAction: Failed to consume core health slot");
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
                Debug.Log($"PlayerWaveAction: Subscribed to collider events for core hitbox {_targetCoreHitbox.name}");
            }

            // Play resonance audio/effects
            PlayWaveEffects();

            // Trigger the resonance started event for state machine and camera system
            OnWaveActionStarted?.Invoke(_targetCoreHitbox);

            Debug.Log($"PlayerWaveAction: Started with target Core hitbox {_targetCoreHitbox.name} - camera should switch to player view");
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
                Debug.LogWarning("PlayerWaveAction: Timed out after maximum duration");
                _isFinished = true;
                CleanupAction();
                return; 
            }

            // Check if target Core hitbox is still valid for resonance
            bool targetCoreNull = _targetCoreHitbox == null;
            bool targetCoreValid = !targetCoreNull && IsValidTargetCore(_targetCoreHitbox);
            bool targetCoreInRange = !targetCoreNull && IsTargetCoreStillInRange(_targetCoreHitbox);
            
            // Debug log every few seconds to track state
            // if (Mathf.FloorToInt(actionDuration * 2) % 10 == 0) // Every 5 seconds
            // {
            //     Debug.Log($"PlayerWaveAction: Update - Duration: {actionDuration:F1}s, Target null: {targetCoreNull}, Valid: {targetCoreValid}, In range: {targetCoreInRange}");
            // }

            if (targetCoreNull || !targetCoreValid || !targetCoreInRange)
            {
                // Core hitbox no longer valid or in range
                if (actionDuration >= MIN_ACTION_DURATION)
                {
                    if (targetCoreNull)
                    {
                        Debug.Log("PlayerWaveAction: Target Core hitbox is null, ending action");
                    }
                    else if (!targetCoreValid)
                    {
                        Debug.Log("PlayerWaveAction: Target Core hitbox no longer in valid state, ending action");
                        
                        // Get detailed state info
                        var enemyController = _targetCoreHitbox.GetEnemyController();
                        if (enemyController != null)
                        {
                            Debug.Log($"PlayerWaveAction: Enemy state: {enemyController.CurrentState}");
                        }
                    }
                    else if (!targetCoreInRange)
                    {
                        Debug.Log("PlayerWaveAction: Target Core hitbox is no longer in range, ending action");
                    }
                    
                    _isFinished = true;
                    CleanupAction();
                    return;
                }
                else
                {
                    Debug.Log($"PlayerWaveAction: Target invalid but minimum duration not met ({actionDuration:F2}s < {MIN_ACTION_DURATION}s), continuing");
                }
            }

            // Update resonance effects (visual feedback, QTE UI placeholder, etc.)
            UpdateWaveEffects(deltaTime);
        }

        /// <summary>
        /// Cancel the resonance action (should not be called since it cannot be interrupted)
        /// </summary>
        public void Cancel(PlayerController player)
        {
            if (_isActive)
            {
                Debug.Log("PlayerWaveAction: Cancelled");
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
            Debug.Log("PlayerWaveAction: Damage taken but action is invulnerable");
        }

        /// <summary>
        /// Find the target Core hitbox for resonance action
        /// </summary>
        /// <returns>The target Core hitbox or null if none found</returns>
        private EnemyHitbox FindTargetCoreHitbox()
        {
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null) return null;

            // Get the closest Core hitbox from CoreAttackTrigger
            var playerMono = playerService.CurrentPlayer;
            
            // Get the closest Core hitbox directly
            var closestCoreHitbox = playerMono.GetClosestCoreHitbox();
            if (closestCoreHitbox != null)
            {
                Debug.Log($"PlayerWaveAction: Found target Core hitbox {closestCoreHitbox.name}");
                return closestCoreHitbox;
            }

            Debug.Log("PlayerWaveAction: No Core hitboxes found in range");
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
                Debug.Log("PlayerWaveAction: Target core collider disabled - ending resonance action");
                
                // Check minimum duration before ending
                float actionDuration = Time.time - _actionStartTime;
                if (actionDuration >= MIN_ACTION_DURATION)
                {
                    _isFinished = true;
                    CleanupAction();
                }
                else
                {
                    Debug.Log($"PlayerWaveAction: Minimum duration not met ({actionDuration:F2}s < {MIN_ACTION_DURATION}s), continuing");
                }
            }
        }

        /// <summary>
        /// Play resonance visual and audio effects
        /// </summary>
        private void PlayWaveEffects()
        {
            // Play resonance start audio
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService != null)
            {
                // TODO: Add specific resonance audio clips
                audioService.PlaySFX2D(AudioClipType.PlayerHit, 0.6f, 0.8f); // Placeholder audio
            }

            // TODO: Add visual effects (particles, screen effects, etc.)
            Debug.Log("PlayerWaveAction: Playing resonance effects (placeholder)");
        }

        /// <summary>
        /// Update ongoing resonance effects
        /// </summary>  
        /// <param name="deltaTime">Time since last frame</param>
        private void UpdateWaveEffects(float deltaTime)
        {
            // TODO: Update visual effects intensity
            // TODO: Update audio effects

            // Placeholder implementation
            float actionDuration = Time.time - _actionStartTime;
            if (actionDuration > 0.1f && Mathf.FloorToInt(actionDuration * 4) % 2 == 0)
            {
                // Simple feedback every 0.25 seconds
                // Debug.Log($"PlayerWaveAction: Wave active for {actionDuration:F1}s");
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
                Debug.Log("PlayerWaveAction: Unsubscribed from core hitbox collider events");
            }

            // Stop effects
            StopWaveEffects();

            // Force refresh UI colors to fix BUG2 (second approach UI color not updating)
            var playerService = ServiceRegistry.Get<IPlayerService>();
            var playerMono = playerService?.CurrentPlayer;
            if (playerMono != null)
            {
                var coreAttackTrigger = playerMono.GetComponentInChildren<CoreAttackTrigger>();
                coreAttackTrigger?.ForceRefreshUIColors();
                Debug.Log("PlayerWaveAction: Force refreshed UI colors after cleanup");
            }

            // Trigger the resonance ended event for state machine and camera system
            OnWaveActionEnded?.Invoke();

            // Clear target reference
            _targetCoreHitbox = null;

            Debug.Log("PlayerWaveAction: Cleaned up - camera should switch back to fixed view");
        }

        /// <summary>
        /// Stop resonance effects
        /// </summary>
        private void StopWaveEffects()
        {
            // TODO: Stop visual effects
            // TODO: Stop audio effects

            Debug.Log("PlayerWaveAction: Stopped resonance effects");
        }
    }
}
