using UnityEngine;
using Resonance.Player.Core;
using Resonance.Player.Data;
using Resonance.Interfaces.Objects;
using Resonance.Core;
using Resonance.Interfaces.Services;
using Resonance.Utilities;

namespace Resonance.Player.Actions
{
    /// <summary>
    /// Player Recover Action - triggered by long press F when no dead enemies are in mental attack range
    /// Conditions: PlayerNormalState, MentalHealth >= 1 slot, NO EnemyPhysicalDeathState enemy in MentalAttackRange
    /// Behavior: Player cannot move, consumes 1 MentalHealth slot, restores a certain amount of PhysicalHealth
    /// End condition: Release F key, or interrupted by damage
    /// </summary>
    public class PlayerRecoverAction : IPlayerAction
    {
        // Action properties
        public string Name => "Recover";
        public bool BlocksMovement => true;
        public bool ProvidesInvulnerability => false;
        public bool CanInterrupt => true; // Can be interrupted by damage

        // Runtime state
        private bool _isActive = false;
        private bool _isFinished = false;
        private bool _slotConsumed = false;
        private float _actionStartTime = 0f;
        private float _lastHealTime = 0f;

        // Configuration
        private const float HEAL_INTERVAL = 0.5f; // Heal every 0.5 seconds
        private const float HEAL_AMOUNT_PER_TICK = 5f; // Amount to heal per tick
        private const float MIN_ACTION_DURATION = 0.3f; // Minimum duration before slot consumption
        private const float MAX_ACTION_DURATION = 10f; // Maximum action duration
        private const float SLOT_CONSUMPTION_DELAY = 0.5f; // Delay before consuming slot

        public bool IsFinished => _isFinished;

        /// <summary>
        /// Check if the RecoverAction can start
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

            // Must NOT have dead enemies in mental attack range (ResonanceAction has priority)
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer != null)
            {
                if (playerService.CurrentPlayer.HasDeadEnemiesInMentalAttackRange())
                {
                    Debug.Log("PlayerRecoverAction: Cannot start - dead enemies in range (ResonanceAction has priority)");
                    return false;
                }
            }

            // Must not be at full physical health (no point in recovering if already full)
            if (player.Stats.PhysicalHealthPercentage >= 1.0f)
            {
                Debug.Log("PlayerRecoverAction: Cannot start - already at full physical health");
                return false;
            }

            Debug.Log("PlayerRecoverAction: All conditions met, can start");
            return true;
        }

        /// <summary>
        /// Start the recover action
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void Start(PlayerController player)
        {
            if (player == null)
            {
                Debug.LogError("PlayerRecoverAction: Cannot start with null player");
                return;
            }

            // Initialize action state
            _isActive = true;
            _isFinished = false;
            _slotConsumed = false;
            _actionStartTime = Time.time;
            _lastHealTime = Time.time;

            // Play recover start effects
            PlayRecoverStartEffects(player);

            Debug.Log("PlayerRecoverAction: Started recovery process");
        }

        /// <summary>
        /// Update the recover action each frame
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
                Debug.LogWarning("PlayerRecoverAction: Timed out after maximum duration");
                _isFinished = true;
                return;
            }

            // Consume slot after delay (to allow for early cancellation)
            if (!_slotConsumed && actionDuration >= SLOT_CONSUMPTION_DELAY)
            {
                if (!player.ConsumeSlot())
                {
                    Debug.LogWarning("PlayerRecoverAction: Failed to consume mental health slot");
                    _isFinished = true;
                    return;
                }
                _slotConsumed = true;
                Debug.Log("PlayerRecoverAction: Mental health slot consumed");
            }

            // Only start healing after slot is consumed
            if (_slotConsumed)
            {
                // Periodic healing
                if (currentTime - _lastHealTime >= HEAL_INTERVAL)
                {
                    PerformHeal(player);
                    _lastHealTime = currentTime;
                }
            }

            // Check if at full health
            if (player.Stats.PhysicalHealthPercentage >= 1.0f)
            {
                Debug.Log("PlayerRecoverAction: Reached full health, ending action");
                _isFinished = true;
                return;
            }

            // Check for input release (this will be handled by ActionController input system)
            // For now, we rely on external cancellation

            // Update visual effects
            UpdateRecoverEffects(player, deltaTime);
        }

        /// <summary>
        /// Cancel the recover action
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void Cancel(PlayerController player)
        {
            if (_isActive)
            {
                Debug.Log("PlayerRecoverAction: Cancelled");
                CleanupAction(player);
            }
        }

        /// <summary>
        /// Called when player takes damage - this action can be interrupted
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void OnDamageTaken(PlayerController player)
        {
            if (_isActive)
            {
                Debug.Log("PlayerRecoverAction: Interrupted by damage");
                _isFinished = true; // Will be cleaned up by ActionController
            }
        }

        /// <summary>
        /// Perform healing on the player
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void PerformHeal(PlayerController player)
        {
            if (player == null) return;

            // Calculate heal amount (could be modified by tiers, equipment, etc.)
            float healAmount = HEAL_AMOUNT_PER_TICK;

            // Apply tier modifiers
            switch (player.MentalTier)
            {
                case MentalHealthTier.High:
                    healAmount *= 1.2f; // 20% bonus when mental health is high
                    break;
                case MentalHealthTier.Low:
                    healAmount *= 0.8f; // 20% penalty when mental health is low
                    break;
                case MentalHealthTier.Empty:
                    healAmount *= 0.5f; // 50% penalty when mental health is empty
                    break;
            }

            // Heal the player
            player.HealPhysical(healAmount);

            // Play heal effect
            PlayHealTickEffect(player);

            Debug.Log($"PlayerRecoverAction: Healed {healAmount:F1} physical health");
        }

        /// <summary>
        /// Play recover start effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void PlayRecoverStartEffects(PlayerController player)
        {
            // Play recovery start audio
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService != null)
            {
                // TODO: Add specific recovery audio clips
                audioService.PlaySFX2D(AudioClipType.PlayerHit, 0.4f, 0.9f); // Placeholder audio
            }

            // TODO: Add visual effects (healing particles, screen glow, etc.)
            Debug.Log("PlayerRecoverAction: Playing recovery start effects (placeholder)");
        }

        /// <summary>
        /// Play heal tick effect
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void PlayHealTickEffect(PlayerController player)
        {
            // TODO: Play healing tick audio
            // TODO: Show healing numbers/effect

            Debug.Log("PlayerRecoverAction: Playing heal tick effect (placeholder)");
        }

        /// <summary>
        /// Update ongoing recover effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <param name="deltaTime">Time since last frame</param>
        private void UpdateRecoverEffects(PlayerController player, float deltaTime)
        {
            // TODO: Update visual effects intensity based on mental tier
            // TODO: Update UI feedback showing recovery progress
            // TODO: Update audio effects

            // Placeholder implementation
            float actionDuration = Time.time - _actionStartTime;
            if (actionDuration > 0.1f && Mathf.FloorToInt(actionDuration * 2) % 4 == 0)
            {
                // Simple feedback every 0.5 seconds
                // Debug.Log($"PlayerRecoverAction: Recovery active for {actionDuration:F1}s");
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
            StopRecoverEffects(player);

            Debug.Log("PlayerRecoverAction: Cleaned up");
        }

        /// <summary>
        /// Stop recover effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void StopRecoverEffects(PlayerController player)
        {
            // TODO: Stop visual effects
            // TODO: Stop audio effects

            Debug.Log("PlayerRecoverAction: Stopped recovery effects");
        }

        /// <summary>
        /// Check if the action should be cancelled due to external conditions
        /// Called by ActionController for input-based cancellation
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <returns>True if action should be cancelled</returns>
        public bool ShouldCancel(PlayerController player)
        {
            // This action continues until:
            // 1. Player releases F key (handled by input system)
            // 2. Player takes damage (handled by OnDamageTaken)
            // 3. Player reaches full health (handled in Update)
            // 4. Dead enemies enter range (priority check)

            // Check if dead enemies entered range (ResonanceAction gets priority)
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer?.HasDeadEnemiesInMentalAttackRange() == true)
            {
                Debug.Log("PlayerRecoverAction: Dead enemies entered range, should cancel for ResonanceAction priority");
                return true;
            }

            return false;
        }
    }
}
