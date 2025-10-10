using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Player.Core;
using Resonance.Player.Data;
using Resonance.Interfaces.Services;
using Resonance.Interfaces.Operations;

namespace Resonance.Player.Actions
{
    /// <summary>
    /// Player Heal Action - triggered by holding F key when no Core hitboxes are in core attack range
    /// Conditions: PlayerNormalState, CoreHealth >= 1 slot, NO Core type EnemyHitbox with enabled collider in CoreAttackRange
    /// Behavior: Player cannot move, consumes 1 CoreHealth slot every 1s, restores Health
    /// End condition: Release F key, or interrupted by damage, or reach full health, or no more core health
    /// </summary>
    public class PlayerHealAction : IPlayerAction
    {
        // Action properties
        public string Name => "Heal";
        public bool BlocksMovement => true;
        public bool ProvidesInvulnerability => false;
        public bool CanInterrupt => true; // Can be interrupted by damage

        // Runtime state
        private bool _isActive = false;
        private bool _isFinished = false;
        private float _actionStartTime = 0f;
        private float _lastSlotConsumedTime = 0f;

        // Configuration
        private const float SLOT_CONSUMPTION_INTERVAL = 2f; // Consume slot every 2 seconds
        private const float HEAL_AMOUNT_PER_CONSUMPTION = 25f; // Amount to heal per slot consumption

        public bool IsFinished => _isFinished;

        /// <summary>
        /// Check if the HealAction can start
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <returns>True if all conditions are met</returns>
        public bool CanStart(PlayerController player)
        {
            if (player == null) return false;

            // Must be in Normal state (not in other actions or death states)
            if (player.CurrentState != "Normal") return false;

            // Must not be moving (healing requires standing still)
            if (player.Movement.IsMoving)
            {
                Debug.Log("PlayerHealAction: Cannot start - player is moving (must stand still to heal)");
                return false;
            }

            // Must have at least 1 core health slot available
            if (!player.CanConsumeSlot) return false;

            // Must NOT have Core hitboxes in core attack range (WaveAction has priority)
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer != null)
            {
                if (playerService.CurrentPlayer.HasCoreHitboxesInCoreAttackRange())
                {
                    Debug.Log("PlayerHealAction: Cannot start - Core hitboxes in range (WaveAction has priority)");
                    return false;
                }
            }

            // Must not be at full health health (no point in recovering if already full)
            if (player.Stats.HealthPercentage >= 1.0f)
            {
                Debug.Log("PlayerHealAction: Cannot start - already at full health health");
                return false;
            }

            Debug.Log("PlayerHealAction: All conditions met, can start");
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
                Debug.LogError("PlayerHealAction: Cannot start with null player");
                return;
            }

            // Initialize action state
            _isActive = true;
            _isFinished = false;
            _actionStartTime = Time.time;
            _lastSlotConsumedTime = Time.time;

            // Force player to stop moving during healing
            player.Movement.SetMovementInput(Vector2.zero);
            Debug.Log("PlayerHealAction: Forced player to stop moving for healing");

            // Play recover start effects
            PlayHealStartEffects(player);

            Debug.Log("PlayerHealAction: Started recovery process");
        }

        /// <summary>
        /// Update the recover action each frame
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <param name="deltaTime">Time since last frame</param>
        public void Update(PlayerController player, float deltaTime)
        {
            if (!_isActive || _isFinished) return;

            // Continuously ensure player doesn't move during healing
            if (player.Movement.IsMoving)
            {
                player.Movement.SetMovementInput(Vector2.zero);
            }

            float currentTime = Time.time;
            float timeSinceLastConsumption = currentTime - _lastSlotConsumedTime;

            // Check if it's time to consume another slot and heal
            if (timeSinceLastConsumption >= SLOT_CONSUMPTION_INTERVAL)
            {
                // Check if player can still consume slots
                if (!player.CanConsumeSlot)
                {
                    Debug.Log("PlayerHealAction: No more core health slots available, ending action");
                    _isFinished = true;
                    return;
                }

                // Consume slot and heal
                if (player.ConsumeSlot())
                {
                    PerformHeal(player);
                    _lastSlotConsumedTime = currentTime;
                    Debug.Log("PlayerHealAction: Consumed core health slot and healed");
                }
                else
                {
                    Debug.LogWarning("PlayerHealAction: Failed to consume core health slot");
                    _isFinished = true;
                    return;
                }
            }

            // Check if at full health
            if (player.Stats.HealthPercentage >= 1.0f)
            {
                Debug.Log("PlayerHealAction: Reached full health, ending action");
                _isFinished = true;
                return;
            }

            // Check if Core hitboxes entered range (WaveAction gets priority)
            if (ShouldCancel(player))
            {
                _isFinished = true;
                return;
            }

            // Update visual effects
            UpdateHealEffects(player, deltaTime);
        }

        /// <summary>
        /// Cancel the recover action
        /// </summary>
        /// <param name="player">Player controller reference</param>
        public void Cancel(PlayerController player)
        {
            if (_isActive)
            {
                Debug.Log("PlayerHealAction: Cancelled");
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
                Debug.Log("PlayerHealAction: Interrupted by damage");
                _isFinished = true; // Will be cleaned up by PlayerActionController
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
            float healAmount = HEAL_AMOUNT_PER_CONSUMPTION;

            // Heal the player
            player.HealHealth(healAmount);

            // Play heal effect
            PlayHealEffect(player);

            Debug.Log($"PlayerHealAction: Healed {healAmount:F1} health health");
        }

        /// <summary>
        /// Play recover start effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void PlayHealStartEffects(PlayerController player)
        {
            // Play recovery start audio
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService != null)
            {
                // TODO: Add specific recovery audio clips
                audioService.PlaySFX2D(AudioClipType.PlayerHit, 0.4f, 0.9f); // Placeholder audio
            }

            // TODO: Add visual effects (healing particles, screen glow, etc.)
            Debug.Log("PlayerHealAction: Playing recovery start effects (placeholder)");
        }

        /// <summary>
        /// Play heal effect
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void PlayHealEffect(PlayerController player)
        {
            // TODO: Play healing audio
            // TODO: Show healing numbers/effect

            Debug.Log("PlayerHealAction: Playing heal effect (placeholder)");
        }

        /// <summary>
        /// Update ongoing recover effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <param name="deltaTime">Time since last frame</param>
        private void UpdateHealEffects(PlayerController player, float deltaTime)
        {
            // TODO: Update visual effects intensity based on core tier
            // TODO: Update UI feedback showing recovery progress
            // TODO: Update audio effects
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
            StopHealEffects(player);

            // Note: Movement input will be automatically restored by the input system
            // when the action ends, so no need to explicitly restore it here

            Debug.Log("PlayerHealAction: Cleaned up - movement input will be restored by input system");
        }

        /// <summary>
        /// Stop recover effects
        /// </summary>
        /// <param name="player">Player controller reference</param>
        private void StopHealEffects(PlayerController player)
        {
            // TODO: Stop visual effects
            // TODO: Stop audio effects

            Debug.Log("PlayerHealAction: Stopped recovery effects");
        }

        /// <summary>
        /// Check if the action should be cancelled due to external conditions
        /// </summary>
        /// <param name="player">Player controller reference</param>
        /// <returns>True if action should be cancelled</returns>
        public bool ShouldCancel(PlayerController player)
        {
            // Check if Core hitboxes entered range (WaveAction gets priority)
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer?.HasCoreHitboxesInCoreAttackRange() == true)
            {
                Debug.Log("PlayerHealAction: Core hitboxes entered range, should cancel for WaveAction priority");
                return true;
            }

            return false;
        }
    }
}
