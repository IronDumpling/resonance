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
    /// Player Resonance Action - triggered by short press F when dead enemies are in mental attack range
    /// Conditions: PlayerNormalState, MentalHealth >= 1 slot, EnemyPhysicalDeathState enemy in MentalAttackRange
    /// Behavior: Player cannot move, is invulnerable to physical damage, consumes 1 MentalHealth slot
    /// End condition: Enemy's EnemyPhysicalDeathState ends
    /// </summary>
    public class PlayerResonanceAction : IPlayerAction
    {
        // Action properties
        public string Name => "Resonance";
        public bool BlocksMovement => true;
        public bool ProvidesInvulnerability => true;
        public bool CanInterrupt => false; // Cannot be interrupted

        // Runtime state
        private bool _isActive = false;
        private bool _isFinished = false;
        private EnemyMonoBehaviour _targetEnemy = null;
        private float _actionStartTime = 0f;

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

            // Must have dead enemies in mental attack range
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null) return false;

            if (!playerService.CurrentPlayer.HasDeadEnemiesInMentalAttackRange()) return false;

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

            // Find target enemy
            _targetEnemy = FindTargetEnemy();
            if (_targetEnemy == null)
            {
                Debug.LogWarning("PlayerResonanceAction: No valid target enemy found");
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

            // Subscribe to target enemy events
            if (_targetEnemy != null)
            {
                // TODO: Subscribe to enemy state change events when enemy system is available
                // For now we'll poll the enemy state in Update
            }

            // Play resonance audio/effects
            PlayResonanceEffects(player);

            Debug.Log($"PlayerResonanceAction: Started with target enemy {_targetEnemy.name}");
        }

        /// <summary>
        /// Update the resonance action each frame
        /// </summary>
        /// <param name="player">Player controller reference</param>
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
                return;
            }

            // Check if target enemy is still in physical death state
            if (_targetEnemy == null || !IsEnemyStillValidTarget(_targetEnemy))
            {
                // Enemy no longer in physical death state or no longer in range
                if (actionDuration >= MIN_ACTION_DURATION)
                {
                    Debug.Log("PlayerResonanceAction: Target enemy left physical death state, ending action");
                    _isFinished = true;
                    return;
                }
                // If minimum duration not met, continue until minimum time is reached
            }

            // Update resonance effects (visual feedback, QTE UI placeholder, etc.)
            UpdateResonanceEffects(player, deltaTime);
        }

        /// <summary>
        /// Cancel the resonance action (should not be called since it cannot be interrupted)
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void Cancel(PlayerController player)
        {
            if (_isActive)
            {
                Debug.Log("PlayerResonanceAction: Cancelled");
                CleanupAction(player);
            }
        }

        /// <summary>
        /// Called when player takes damage (should not interrupt this action)
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void OnDamageTaken(PlayerController player)
        {
            // This action provides invulnerability and cannot be interrupted
            // Log for debugging purposes
            Debug.Log("PlayerResonanceAction: Damage taken but action is invulnerable");
        }

        /// <summary>
        /// Find the target enemy for resonance action
        /// </summary>
        /// <returns>The target enemy or null if none found</returns>
        private EnemyMonoBehaviour FindTargetEnemy()
        {
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null) return null;

            // Get the closest dead enemy from MentalAttackTrigger
            var playerMono = playerService.CurrentPlayer;
            
            // Try to get the closest dead enemy
            // We need to access the MentalAttackTrigger somehow
            // For now, we'll implement a simple version that checks if there are dead enemies
            if (playerMono.HasDeadEnemiesInMentalAttackRange() && playerMono.GetDeadEnemyCount() > 0)
            {
                // In a real implementation, we would get the actual enemy reference
                // For now, we'll return a placeholder (this needs to be improved when we have access to the trigger)
                Debug.Log("PlayerResonanceAction: Found target enemy (placeholder implementation)");
                
                // TODO: Get actual enemy reference from MentalAttackTrigger
                // This is a temporary implementation that assumes we have a target
                return null; // This will be replaced with actual enemy reference
            }

            return null;
        }

        /// <summary>
        /// Check if the target enemy is still a valid target
        /// </summary>
        /// <param name="enemy">Enemy to check</param>
        /// <returns>True if enemy is still in physical death state and in range</returns>
        private bool IsEnemyStillValidTarget(EnemyMonoBehaviour enemy)
        {
            if (enemy == null) return false;

            // Check if enemy is still in physical death state
            if (!enemy.IsInitialized) return false;

            var enemyController = enemy.Controller;
            if (enemyController == null) return false;

            // Check state machine
            string currentState = enemyController.CurrentState;
            bool isStillInPhysicalDeath = currentState == "PhysicalDeath";

            // Alternative health check
            if (!isStillInPhysicalDeath)
            {
                var stats = enemyController.Stats;
                if (stats != null)
                {
                    isStillInPhysicalDeath = stats.currentPhysicalHealth <= 0f && stats.currentMentalHealth > 0f;
                }
            }

            // Check if still in range (through PlayerService)
            var playerService = ServiceRegistry.Get<IPlayerService>();
            bool isStillInRange = playerService?.CurrentPlayer?.HasDeadEnemiesInMentalAttackRange() ?? false;

            return isStillInPhysicalDeath && isStillInRange;
        }

        /// <summary>
        /// Play resonance visual and audio effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void PlayResonanceEffects(PlayerController player)
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
        /// <param name="player">Player controller reference</param>
        /// <param name="deltaTime">Time since last frame</param>
        private void UpdateResonanceEffects(PlayerController player, float deltaTime)
        {
            // TODO: Update visual effects intensity
            // TODO: Update QTE UI
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
        /// <param name="player">Player controller reference</param>
        private void CleanupAction(PlayerController player)
        {
            _isActive = false;
            _isFinished = true;

            // Unsubscribe from enemy events
            if (_targetEnemy != null)
            {
                // TODO: Unsubscribe from enemy state change events
            }

            // Stop effects
            StopResonanceEffects(player);

            // Clear target reference
            _targetEnemy = null;

            Debug.Log("PlayerResonanceAction: Cleaned up");
        }

        /// <summary>
        /// Stop resonance effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void StopResonanceEffects(PlayerController player)
        {
            // TODO: Stop visual effects
            // TODO: Hide QTE UI
            // TODO: Stop audio effects

            Debug.Log("PlayerResonanceAction: Stopped resonance effects");
        }
    }
}
