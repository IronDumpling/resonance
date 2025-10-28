using UnityEngine;
using Resonance.Core;
using Resonance.Player.Core;
using Resonance.Player.Data;
using Resonance.Player.Triggers;
using Resonance.Interfaces;
using Resonance.Interfaces.Operations;
using Resonance.Interfaces.Services;
using Resonance.Enemies.Data;
using Resonance.Enemies.Triggers;
using Resonance.Utilities;

namespace Resonance.Player.Actions
{
    /// <summary>
    /// Player Core Attack Action - triggered by short press F when IWavable targets are in wave attack range
    /// Conditions: PlayerNormalState, CoreHealth >= 1 slot, IWavable (EnemyCrystalCoreHitbox) with enabled collider in WaveAttackRange
    /// Behavior: Player cannot move, is invulnerable to health damage, consumes 1 CoreHealth slot
    /// End condition: Target IWavable collider becomes disabled or exits range
    /// </summary>
    public class PlayerWaveAttackAction : IPlayerAction
    {
        // Static events for state machine integration
        public static event System.Action<IWavable, IWavable> OnWaveAttackActionStarted; // source, target
        public static event System.Action OnWaveAttackActionEnded;

        // Action properties
        public string Name => "WaveAttack";
        public bool BlocksMovement => true;
        public bool ProvidesInvulnerability => true;
        public bool CanInterrupt => false; // Cannot be interrupted

        // Runtime state
        private bool _isActive = false;
        private bool _isFinished = false;
        private float _actionStartTime = 0f;
        private IWavable _targetWavable = null; // Current target for wave attack

        private PlayerController _player;

        // Configuration
        private const float MIN_ACTION_DURATION = 0.5f; // Minimum action duration for feedback
        private const float MAX_ACTION_DURATION = 10f; // Safety timeout

        public bool IsFinished => _isFinished;

        /// <summary>
        /// Check if the WaveAttackAction can start
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <returns>True if all conditions are met</returns>
        public bool CanStart(PlayerController player)
        {
            if (player == null)
            {
                Debug.Log("PlayerWaveAttackAction: Cannot start - player is null");
                return false;
            }

            // Must be in Normal state (not in other actions or death states)
            if (player.CurrentState != "Normal")
            {
                Debug.Log($"PlayerWaveAttackAction: Cannot start - player not in Normal state (current: {player.CurrentState})");
                return false;
            }

            // Must have at least 1 core health slot available
            if (!player.CanConsumeSlot)
            {
                Debug.Log("PlayerWaveAttackAction: Cannot start - no core health slots available");
                return false;
            }

            // Must have IWavable targets in wave attack range
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null)
            {
                Debug.Log("PlayerWaveAttackAction: Cannot start - player service or current player is null");
                return false;
            }

            if (!playerService.CurrentPlayer.HasWavablesInWaveAttackRange())
            {
                Debug.Log("PlayerWaveAttackAction: Cannot start - no IWavable targets in wave attack range");
                return false;
            }

            // Additional check: verify IWavable targets are in valid states
            var waveAttackTrigger = playerService.CurrentPlayer.GetComponentInChildren<WaveAttackTrigger>();
            if (waveAttackTrigger != null)
            {
                var coreHitboxes = waveAttackTrigger.WavablesInRange;
                Debug.Log($"PlayerWaveAttackAction: Found {coreHitboxes.Count} wavable targets in range");
                
                bool hasValidTarget = false;
                
                foreach (var core in coreHitboxes)
                {
                    if (core != null && core is IWavable wavable)
                    {
                        bool isValid = IsValidTargetWavable(wavable);
                        Debug.Log($"PlayerWaveAttackAction: Wavable {(core as MonoBehaviour)?.name} validity check: {isValid}");
                        
                        if (isValid)
                        {
                            hasValidTarget = true;
                            break;
                        }
                    }
                    else
                    {
                        Debug.Log("PlayerWaveAttackAction: Found null or non-IWavable target in range list");
                    }
                }
                
                if (!hasValidTarget)
                {
                    Debug.Log("PlayerWaveAttackAction: Cannot start - no valid IWavable targets (targets may be in invalid states)");
                    return false;
                }
            }
            else
            {
                Debug.Log("PlayerWaveAttackAction: Cannot start - WaveAttackTrigger not found");
                return false;
            }

            Debug.Log("PlayerWaveAttackAction: All conditions met, can start");
            return true;
        }
        
        /// <summary>
        /// Check if a wavable target is in a valid state for wave attack
        /// </summary>
        /// <param name="wavable">IWavable target to check</param>
        /// <returns>True if wavable is valid for wave attack</returns>
        private bool IsValidTargetWavable(IWavable wavable)
        {
            if (wavable == null)
            {
                Debug.Log("PlayerWaveAttackAction: IsValidTargetWavable - wavable is null");
                return false;
            }
            
            // IWavable must be a MonoBehaviour (EnemyCrystalCoreHitbox)
            if (!(wavable is MonoBehaviour mono))
            {
                Debug.Log("PlayerWaveAttackAction: IsValidTargetWavable - wavable is not a MonoBehaviour");
                return false;
            }
            
            // Check if it's an EnemyCrystalCoreHitbox
            if (!(wavable is EnemyCrystalCoreHitbox coreHitbox))
            {
                Debug.Log($"PlayerWaveAttackAction: IsValidTargetWavable - {mono.name} is not an EnemyCrystalCoreHitbox");
                return false;
            }
                
            if (!coreHitbox.IsInitialized)
            {
                Debug.Log($"PlayerWaveAttackAction: IsValidTargetWavable - {mono.name} not initialized");
                return false;
            }
                
            // Check if the collider is enabled
            var collider = coreHitbox.GetComponent<Collider>();
            if (collider == null)
            {
                Debug.Log($"PlayerWaveAttackAction: IsValidTargetWavable - {mono.name} has no collider");
                return false;
            }
            
            if (!collider.enabled)
            {
                Debug.Log($"PlayerWaveAttackAction: IsValidTargetWavable - {mono.name} collider is disabled");
                return false;
            }
                
            // Check if the enemy is in a valid state for wave (not in attack state)
            var enemyMono = coreHitbox.GetEnemyMonoBehaviour();
            if (enemyMono == null)
            {
                Debug.Log($"PlayerWaveAttackAction: IsValidTargetWavable - {mono.name} has no EnemyMonoBehaviour");
                return false;
            }
                
            var enemyController = enemyMono.Controller;
            if (enemyController == null)
            {
                Debug.Log($"PlayerWaveAttackAction: IsValidTargetWavable - {mono.name} has no EnemyController");
                return false;
            }
                
            // Valid state for wave: Reviving (physically dead but core alive)
            // Enemy must be vulnerable (in revival state, not truly dead)
            var enemyState = enemyController.CurrentState;
            bool isValidState = enemyState == EnemyState.Reviving;
            
            return isValidState;
        }

        /// <summary>
        /// Start the wave action
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void Start(PlayerController player)
        {
            if (player == null)
            {
                Debug.LogError("PlayerWaveAttackAction: Cannot start with null player");
                return;
            }

            _player = player;

            // Find target IWavable
            _targetWavable = FindTargetWavable();
            if (_targetWavable == null)
            {
                Debug.LogError("PlayerWaveAttackAction: Cannot start - no valid IWavable target found");
                _isFinished = true;
                return;
            }

            // Consume core health slot
            if (!player.ConsumeSlot())
            {
                Debug.LogWarning("PlayerWaveAttackAction: Failed to consume core health slot");
                _isFinished = true;
                return;
            }

            // Initialize action state
            _isActive = true;
            _isFinished = false;
            _actionStartTime = Time.time;

            // Subscribe to collider events
            if (_targetWavable is EnemyCrystalCoreHitbox coreHitbox)
            {
                coreHitbox.OnColliderDisabled += OnTargetWavableColliderDisabled;
                Debug.Log("PlayerWaveAttackAction: Subscribed to target wavable collider events");
            }

            // Play wave audio/effects
            PlayWaveEffects();

            // Get IWavable references for event
            var playerService = ServiceRegistry.Get<IPlayerService>();
            IWavable sourceWavable = playerService?.CurrentPlayer as IWavable;
            
            // Trigger the wave started event for state machine and camera system
            OnWaveAttackActionStarted?.Invoke(sourceWavable, _targetWavable);

            Debug.Log($"PlayerWaveAttackAction: Started with source: {(sourceWavable != null ? "valid" : "null")}, " +
                     $"target: {(_targetWavable != null ? "valid" : "null")}");
        }

        /// <summary>
        /// Update the wave action each frame
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
                Debug.LogWarning("PlayerWaveAttackAction: Timed out after maximum duration");
                _isFinished = true;
                CleanupAction();
                return; 
            }

            // Check if target Wavable is still valid for wave
            bool targetWavableNull = _targetWavable == null;
            bool targetWavableValid = !targetWavableNull && IsValidTargetWavable(_targetWavable);
            bool targetWavableInRange = !targetWavableNull && IsTargetWavableStillInRange(_targetWavable);

            if (targetWavableNull || !targetWavableValid || !targetWavableInRange)
            {
                // Wavable no longer valid or in range
                if (actionDuration >= MIN_ACTION_DURATION)
                {
                    if (targetWavableNull)
                    {
                        Debug.Log("PlayerWaveAttackAction: Target Wavable is null, ending action");
                    }
                    else if (!targetWavableValid)
                    {
                        Debug.Log("PlayerWaveAttackAction: Target Wavable no longer in valid state, ending action");
                        
                        // Get detailed state info
                        if (_targetWavable is EnemyCrystalCoreHitbox coreHitbox)
                        {
                            var enemyController = coreHitbox.GetEnemyController();
                            if (enemyController != null)
                            {
                                Debug.Log($"PlayerWaveAttackAction: Enemy state: {enemyController.CurrentState}");
                            }
                        }
                    }
                    else if (!targetWavableInRange)
                    {
                        Debug.Log("PlayerWaveAttackAction: Target Wavable is no longer in range, ending action");
                    }
                    
                    _isFinished = true;
                    CleanupAction();
                    return;
                }
                else
                {
                    Debug.Log($"PlayerWaveAttackAction: Target invalid but minimum duration not met ({actionDuration:F2}s < {MIN_ACTION_DURATION}s), continuing");
                }
            }

            // Update wave effects (visual feedback, QTE UI placeholder, etc.)
            UpdateWaveEffects(deltaTime);
        }

        /// <summary>
        /// Cancel the wave action (should not be called since it cannot be interrupted)
        /// </summary>
        public void Cancel(PlayerController player)
        {
            if (_isActive)
            {
                Debug.Log("PlayerWaveAttackAction: Cancelled");
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
            Debug.Log("PlayerWaveAttackAction: Damage taken but action is invulnerable");
        }

        /// <summary>
        /// Find the target IWavable for wave action
        /// </summary>
        /// <returns>The target IWavable or null if none found</returns>
        private IWavable FindTargetWavable()
        {
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null) return null;

            // Get the closest IWavable target from WaveAttackTrigger
            var playerMono = playerService.CurrentPlayer;
            var waveAttackTrigger = playerMono.GetComponentInChildren<WaveAttackTrigger>();
            
            if (waveAttackTrigger == null)
            {
                Debug.Log("PlayerWaveAttackAction: WaveAttackTrigger not found");
                return null;
            }
            
            // Find the closest valid IWavable from the targets in range
            IWavable closestWavable = null;
            float closestDistance = float.MaxValue;
            
            foreach (var target in waveAttackTrigger.WavablesInRange)
            {
                if (target != null && target is IWavable wavable && IsValidTargetWavable(wavable))
                {
                    var targetMono = target as MonoBehaviour;
                    if (targetMono != null)
                    {
                        float distance = Vector3.Distance(playerMono.transform.position, targetMono.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestWavable = wavable;
                        }
                    }
                }
            }
            
            if (closestWavable != null)
            {
                var wavableMono = closestWavable as MonoBehaviour;
                Debug.Log($"PlayerWaveAttackAction: Found target IWavable {wavableMono?.name} at distance {closestDistance:F2}");
                return closestWavable;
            }

            Debug.Log("PlayerWaveAttackAction: No valid IWavable targets found in range");
            return null;
        }

        /// <summary>
        /// Check if the target IWavable is still in range (collider state is handled by events)
        /// </summary>
        /// <param name="wavable">IWavable target to check</param>
        /// <returns>True if IWavable is still in range</returns>
        private bool IsTargetWavableStillInRange(IWavable wavable)
        {
            if (wavable == null) return false;

            // Check if still in range (through PlayerService)
            var playerService = ServiceRegistry.Get<IPlayerService>();
            var playerMono = playerService?.CurrentPlayer;
            if (playerMono == null) return false;

            // Get WaveAttackTrigger to check current targets in range
            var waveAttackTrigger = playerMono.GetComponentInChildren<WaveAttackTrigger>();
            if (waveAttackTrigger == null) return false;

            // Check if this specific wavable is still being tracked
            foreach (var target in waveAttackTrigger.WavablesInRange)
            {
                if (target != null && target is IWavable targetWavable && targetWavable == wavable)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Handle target wavable collider disabled event
        /// </summary>
        /// <param name="hitbox">The IWavable hitbox that was disabled</param>
        private void OnTargetWavableColliderDisabled(EnemyCrystalCoreHitbox hitbox)
        {
            if (hitbox != null && hitbox == _targetWavable)
            {
                Debug.Log("PlayerWaveAttackAction: Target wavable collider disabled - ending wave action");
                
                // Check minimum duration before ending
                float actionDuration = Time.time - _actionStartTime;
                if (actionDuration >= MIN_ACTION_DURATION)
                {
                    _isFinished = true;
                    CleanupAction();
                }
                else
                {
                    Debug.Log($"PlayerWaveAttackAction: Minimum duration not met ({actionDuration:F2}s < {MIN_ACTION_DURATION}s), continuing");
                }
            }
        }

        /// <summary>
        /// Play wave visual and audio effects
        /// </summary>
        private void PlayWaveEffects()
        {
            // Play wave start audio
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService != null)
            {
                // TODO: Add specific wave audio clips
                audioService.PlaySFX2D(AudioClipType.PlayerHit, 0.6f, 0.8f); // Placeholder audio
            }

            // TODO: Add visual effects (particles, screen effects, etc.)
            Debug.Log("PlayerWaveAttackAction: Playing wave effects (placeholder)");
        }

        /// <summary>
        /// Update ongoing wave effects
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
                // Debug.Log($"PlayerWaveAttackAction: Wave active for {actionDuration:F1}s");
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

            // Unsubscribe from wavable collider events
            if (_targetWavable != null && _targetWavable is EnemyCrystalCoreHitbox coreHitbox)
            {
                coreHitbox.OnColliderDisabled -= OnTargetWavableColliderDisabled;
                Debug.Log("PlayerWaveAttackAction: Unsubscribed from wavable collider events");
            }

            // Stop effects
            StopWaveEffects();

            // Force refresh UI colors to fix BUG2 (second approach UI color not updating)
            var playerService = ServiceRegistry.Get<IPlayerService>();
            var playerMono = playerService?.CurrentPlayer;
            if (playerMono != null)
            {
                var waveAttackTrigger = playerMono.GetComponentInChildren<WaveAttackTrigger>();
                waveAttackTrigger?.ForceRefreshUIColors();
                Debug.Log("PlayerWaveAttackAction: Force refreshed UI colors after cleanup");
            }

            // Trigger the wave ended event for state machine and camera system
            OnWaveAttackActionEnded?.Invoke();

            // Clear target reference
            _targetWavable = null;

            Debug.Log("PlayerWaveAttackAction: Cleaned up - camera should switch back to fixed view");
        }

        /// <summary>
        /// Stop wave effects
        /// </summary>
        private void StopWaveEffects()
        {
            // TODO: Stop visual effects
            // TODO: Stop audio effects

            Debug.Log("PlayerWaveAttackAction: Stopped wave effects");
        }
    }
}
