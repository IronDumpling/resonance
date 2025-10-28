using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Resonance.Core;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;
using Resonance.Player.Actions;
using Resonance.Player.Triggers;
using Resonance.Enemies.Triggers;
using Resonance.UI;
using Resonance.Utilities.Types;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Wave attack action node - attacks player's core health via WavePanel QTE
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyActionBase for component access
    /// - No internal condition checking (handled by Conditional nodes)
    /// - Focuses only on executing the wave attack behavior
    /// - Returns Running while attacking, Success when complete
    /// - Deals CoreHealth damage through WavePanel QTE system
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Executes a wave attack that damages player's core health via WavePanel QTE")]
    public class WaveAttackAction : EnemyActionBase
    {
        // Static event for state machine integration
        public static event System.Action<IWavable, IWavable> OnWaveAttackActionStarted; // source, target
        public static event System.Action OnWaveAttackActionEnded;

        // AI configuration
        [SerializeField] private float _aiCheckInterval = 0.1f; // How often to check wave match
        [SerializeField] private float _perfectMatchThreshold = 90f; // AI tries for perfect match
        [SerializeField] private int _maxAttackAttempts = 1; // Maximum QTE attempts
        [SerializeField] private float _minTimeBetweenAttempts = 0.5f; // Cooldown between attempts

        private bool _attackLaunched = false;
        private IWavable _targetWavable = null; // Target for wave attack
        private IUIService _uiService;
        private WavePanel _wavePanel;
        
        // AI state tracking
        private bool _waveStateActive = false;
        private bool _waveStateFinished = false;
        private int _attackAttempts = 0;
        private float _lastAttemptTime = 0f;
        private float _lastCheckTime = 0f;

        public override void OnStart()
        {
            base.OnStart();
            _attackLaunched = false;
            _targetWavable = null;
            _attackAttempts = 0;
            _waveStateActive = false;
            _waveStateFinished = false;
            _lastAttemptTime = 0f;
            _lastCheckTime = 0f;
            
            // Get UI service
            _uiService = ServiceRegistry.Get<IUIService>();
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

                // 2. Consume energy for wave attack
                if (!Controller.Stats.crystalCore.ConsumeEnergySlot())
                {
                    return TaskStatus.Failure;
                }

                // 3. Launch business logic (set cooldown)
                if (!Controller.LaunchWaveAttack())
                {
                    return TaskStatus.Failure;
                }

                // 4. Enable enemy's crystal core collider for wave attack
                EnemyHitboxManager hitboxManager = enemyMono.HitboxManager;
                if (hitboxManager != null)
                {
                    hitboxManager.EnableCoreColliderForWaveAttack();
                }
                
                // 5. Get source IWavable (enemy's own crystal core hitbox)
                IWavable sourceWavable = enemyMono.CrystalCoreHitbox;
                
                // 6. Broadcast wave attack started event (this triggers WaveState)
                OnWaveAttackActionStarted?.Invoke(sourceWavable, _targetWavable);
                Debug.Log($"[BT Action] WaveAttackAction: Started wave attack with source: {(sourceWavable != null ? "valid" : "null")}, target: {(_targetWavable != null ? "valid" : "null")}");
                
                // 7. Set Animator parameters
                if (Animator != null && Animator.isActiveAndEnabled)
                {
                    // Set InAttackRange to allow Animator to enter WaveAttackSM
                    Animator.SetBool("InAttackRange", true);
                    
                    // Trigger wave attack transition
                    Animator.SetTrigger("WaveAttackStart");
                }
                else
                {
                    Debug.LogWarning($"[BT Action] WaveAttackAction: Animator not available!");
                }
                
                _attackLaunched = true;
                _waveStateActive = true;
                
                // 8. Stop movement during attack
                Movement?.Stop();
            }

            // ===== Phase 2: AI QTE Logic (Poll WavePanel and trigger attacks) =====
            if (_waveStateActive && !_waveStateFinished)
            {
                // Get WavePanel if not yet cached
                if (_wavePanel == null && _uiService != null)
                {
                    _wavePanel = _uiService.GetPanel<WavePanel>("WavePanel");
                }
                
                if (_wavePanel != null)
                {
                    // Check if enough time has passed since last check
                    if (Time.time - _lastCheckTime >= _aiCheckInterval)
                    {
                        _lastCheckTime = Time.time;
                        
                        // Get current wave match percentage
                        float matchPercentage = _wavePanel.GetCurrentMatchPercentage();
                        
                        if (matchPercentage >= 0f) // -1 means wave not active
                        {
                            // Check if we should attempt an attack
                            bool shouldAttempt = matchPercentage >= _perfectMatchThreshold &&
                                               _attackAttempts < _maxAttackAttempts &&
                                               Time.time - _lastAttemptTime >= _minTimeBetweenAttempts;
                            
                            if (shouldAttempt)
                            {
                                Debug.Log($"[BT Action] WaveAttackAction: AI triggering QTE attack (Match: {matchPercentage:F1}%, Attempt: {_attackAttempts + 1}/{_maxAttackAttempts})");
                                
                                // Trigger QTE with Perfect result (enemy always gets perfect timing)
                                WaveInteractionResult result = _wavePanel.ProcessWaveTrigger(WaveInteractionResult.Perfect);
                                
                                _attackAttempts++;
                                _lastAttemptTime = Time.time;
                                
                                Debug.Log($"[BT Action] WaveAttackAction: AI attack result: {result}");
                                
                                // Check if we've reached max attempts
                                if (_attackAttempts >= _maxAttackAttempts)
                                {
                                    Debug.Log($"[BT Action] WaveAttackAction: AI reached max attempts ({_maxAttackAttempts}), ending wave attack");
                                    _waveStateFinished = true;
                                    _waveStateActive = false;
                                    
                                    // Broadcast wave attack ended event to exit WaveState
                                    OnWaveAttackActionEnded?.Invoke();
                                }
                            }
                        }
                    }
                }
                
                // Continue running while wave state is active
                return TaskStatus.Running;
            }

            // ===== Phase 3: Wave State Finished =====
            if (_waveStateFinished)
            {
                Debug.Log($"[BT Action] WaveAttackAction: Wave state finished, completing action");
                return TaskStatus.Success;
            }

            // Continue waiting
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
            
            Debug.Log($"[BT Action] WaveAttackAction: Ended - wave attack complete");
            
            // Clean up state
            _attackLaunched = false;
            _waveStateActive = false;
            _waveStateFinished = false;
            _targetWavable = null;
            _wavePanel = null;
            _attackAttempts = 0;
        }
    }
}
