using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Resonance.Core;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;
using Resonance.Player.Triggers;
using Resonance.Enemies.Triggers;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Wave attack action node - attacks player's core health
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyActionBase for component access
    /// - No internal condition checking (handled by Conditional nodes)
    /// - Focuses only on executing the wave attack behavior
    /// - Returns Running while attacking, Success when complete
    /// - Deals CoreHealth damage to break player's crystal core
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Executes a wave attack that damages player's core health")]
    public class WaveAttackAction : EnemyActionBase
    {
        // Static event for state machine integration
        public static event System.Action<IWavable, IWavable> OnWaveAttackActionStarted; // source, target
        public static event System.Action OnWaveAttackActionEnded;

        private bool _sequenceFinished = false;
        private bool _attackLaunched = false;
        private IWavable _targetWavable = null; // Target for wave attack

        public override void OnStart()
        {
            base.OnStart();
            _attackLaunched = false;
            _sequenceFinished = false;
            _targetWavable = null;
        }

        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            // ===== Phase 1: Launch Wave Attack =====
            if (!_attackLaunched)
            {
                // 1. Find target IWavable (PlayerCrystalCoreHitbox)
                _targetWavable = FindTargetWavable();
                if (_targetWavable == null)
                {
                    Debug.LogWarning($"[BT Action] WaveAttackAction: Cannot find valid target IWavable");
                    return TaskStatus.Failure;
                }

                // 2. Subscribe to completion event
                Controller.OnAttackSequenceFinished += HandleSequenceFinished;

                // 3. Consume energy for wave attack
                if (!Controller.Stats.crystalCore.ConsumeEnergySlot())
                {
                    Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
                    return TaskStatus.Failure;
                }

                // 4. Launch business logic (set cooldown)
                if (!Controller.LaunchWaveAttack())
                {
                    Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
                    return TaskStatus.Failure;
                }

                // 5. Enable enemy's crystal core collider for wave attack
                EnemyHitboxManager hitboxManager = enemyMono.HitboxManager;
                if (hitboxManager != null)
                {
                    hitboxManager.EnableCoreColliderForWaveAttack();
                }
                
                // 6. Get source IWavable (enemy's own crystal core hitbox)
                IWavable sourceWavable = enemyMono.CrystalCoreHitbox;
                
                // 7. Broadcast wave attack started event
                OnWaveAttackActionStarted?.Invoke(sourceWavable, _targetWavable);
                Debug.Log($"[BT Action] WaveAttackAction: Started with source: {(sourceWavable != null ? "valid" : "null")}, target: {(_targetWavable != null ? "valid" : "null")}");
                
                // 8. Set Animator parameters
                if (Animator != null && Animator.isActiveAndEnabled)
                {
                    // Set InAttackRange to allow Animator to enter WaveAttackSM
                    Animator.SetBool("InAttackRange", true);
                    
                    // Trigger wave attack transition
                    Animator.SetTrigger("WaveAttackStart");
                }
                else
                {
                    Debug.LogWarning($"[BT Action] WaveAttackAction: Animator not available! Will rely on event callback.");
                }
                
                _attackLaunched = true;
                
                // 9. Stop movement during attack
                Movement?.Stop();
            }

            // ===== Phase 2: Wait for Animation Event =====
            if (_sequenceFinished)
            {
                return TaskStatus.Success;
            }

            // Continue waiting for animation to complete
            return TaskStatus.Running;
        }

        /// <summary>
        /// Find target IWavable (PlayerCrystalCoreHitbox) with enabled collider
        /// </summary>
        /// <returns>Target IWavable or null if not found</returns>
        private IWavable FindTargetWavable()
        {
            // Get player service
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null)
            {
                Debug.LogWarning($"[BT Action] WaveAttackAction: PlayerService or CurrentPlayer is null");
                return null;
            }

            // Get player's crystal core hitbox
            var playerMono = playerService.CurrentPlayer;
            var playerCoreHitbox = playerMono.CrystalCoreHitbox;

            if (playerCoreHitbox == null)
            {
                Debug.LogWarning($"[BT Action] WaveAttackAction: Player has no CrystalCoreHitbox");
                return null;
            }

            // Check if the collider is enabled
            var collider = playerCoreHitbox.GetComponent<Collider>();
            if (collider == null || !collider.enabled)
            {
                Debug.LogWarning($"[BT Action] WaveAttackAction: Player CrystalCoreHitbox collider is disabled or missing");
                return null;
            }

            // Check if it's a valid target for wave attack
            if (!playerCoreHitbox.IsValidForWaveAttack())
            {
                Debug.LogWarning($"[BT Action] WaveAttackAction: Player CrystalCoreHitbox is not valid for wave attack");
                return null;
            }

            Debug.Log($"[BT Action] WaveAttackAction: Found valid target IWavable - PlayerCrystalCoreHitbox");
            return playerCoreHitbox;
        }

        private void HandleSequenceFinished()
        {
            _sequenceFinished = true;
        }

        public override void OnEnd()
        {
            // Reset Animator parameters - let Animator exit WaveAttackSM
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("InAttackRange", false);
            }
            
            // Disable enemy's crystal core collider after wave attack
            EnemyHitboxManager hitboxManager = enemyMono.HitboxManager;
            if (hitboxManager != null)
            {
                hitboxManager.DisableCoreColliderAfterWaveAttack();
            }
            
            // Broadcast wave attack ended event
            OnWaveAttackActionEnded?.Invoke();
            Debug.Log($"[BT Action] WaveAttackAction: Ended - camera should switch back");
            
            // Clean up event subscriptions
            if (Controller != null)
            {
                Controller.OnAttackSequenceFinished -= HandleSequenceFinished;
            }
            
            _attackLaunched = false;
            _sequenceFinished = false;
            _targetWavable = null;
        }
    }
}
