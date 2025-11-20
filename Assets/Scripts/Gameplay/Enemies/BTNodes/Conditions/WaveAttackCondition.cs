using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Resonance.Core;
using Resonance.Gameplay.Player.Triggers;
using Resonance.Shared.Interfaces;
using Resonance.Shared.Interfaces.Services;

namespace Resonance.Gameplay.Enemies.BTNodes.Conditions
{
    /// <summary>
    /// Condition node that checks if enemy can perform wave attack
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyConditionalBase for component access
    /// - Returns Success if can perform wave attack, Failure otherwise
    /// - Wave attack requires: alive, has target, cooldown ready, energy available, and valid IWavable target
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Checks if enemy can perform a wave attack (cooldown ready, energy available, and valid IWavable target)")]
    public class WaveAttackCondition : EnemyConditionalBase
    {
        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            // Check basic wave attack ability (cooldown and energy)
            if (!Controller.CanWaveAttack)
            {
                return TaskStatus.Failure;
            }

            // Check if there's a valid IWavable target (PlayerCrystalCoreHitbox with enabled collider)
            if (!HasValidWavableTarget())
            {
                return TaskStatus.Failure;
            }

            return TaskStatus.Success;
        }

        /// <summary>
        /// Check if there's a valid IWavable target (PlayerCrystalCoreHitbox with enabled collider)
        /// </summary>
        /// <returns>True if valid target exists</returns>
        private bool HasValidWavableTarget()
        {
            // Get player service
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null)
            {
                return false;
            }

            // Get player's crystal core hitbox
            var playerMono = playerService.CurrentPlayer;
            var playerCoreHitbox = playerMono.CrystalCoreHitbox;

            if (playerCoreHitbox == null)
            {
                return false;
            }

            // Check if the collider is enabled
            var collider = playerCoreHitbox.GetComponent<Collider>();
            if (collider == null || !collider.enabled)
            {
                return false;
            }

            // Check if it's a valid target for wave attack
            if (!playerCoreHitbox.IsValidForWaveAttack())
            {
                return false;
            }

            return true;
        }
    }
}

