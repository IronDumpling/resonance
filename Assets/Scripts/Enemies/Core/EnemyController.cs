using UnityEngine;
using System.Collections.Generic;
using Resonance.Enemies;
using Resonance.Enemies.Data;
using Resonance.Enemies.States;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Utilities.CrystalCore;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;

namespace Resonance.Enemies.Core
{
    /// <summary>
    /// Enemy Controller, manages enemy state and behavior
    /// This is a Non-MonoBehaviour class, handling enemy logic
    /// </summary>
    public class EnemyController : IPausable
    {
        // Core Data
        private EnemyRuntimeStats _stats;
        private EnemyStateMachine _stateMachine;
        private EnemyActionController _actionController;
        private EnemyMovement _movement;
        
        // Combat State
        private float _lastAttackTime = 0f;
        private float _revivalTimer = 0f;
        private bool _hitboxEnabled = false;
        private HashSet<IDamageable> _currentAttackHits = new HashSet<IDamageable>();
        
        // Target Tracking
        private Transform _playerTarget;
        private Vector3 _lastKnownPlayerPosition;
        private bool _hasPlayerTarget = false;
        private bool _isPlayerInAttackRange = false;
        
        // Patrol State
        private Vector3 _patrolCenter;
        private Vector3 _currentPatrolTarget;
        private bool _isPatrolling = false;
        private Vector3 _patrolWaypointA;
        private Vector3 _patrolWaypointB;
        private bool _movingToWaypointB = true; // true = moving to B, false = moving to A
        
        // Patrol Configuration
        private PatrolMode _patrolMode = PatrolMode.Infinite;
        private int _maxPatrolCycles = 3;
        private float _patrolSpeed = 2f;
        private float _singleCycleDuration = 10f;
        private float _waitAtWaypointDuration = 1f;

        // Damage Hitbox
        private Transform _damageHitboxChild;

        // Patrol Runtime State
        private int _currentPatrolCycles = 0;
        private float _currentCycleStartTime = 0f;
        
        // Chase Configuration
        private float _targetUpdateInterval = 0.5f;
        
        // Statistics
        private int _timesHit = 0;
        private float _totalDamageTaken = 0f;
        private float _totalDamageDealt = 0f;
        private int _attacksLaunched = 0;
        
        // Dual Health Events
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action<float, float> OnCoreEnergyChanged; // current, max
        public System.Action OnPhysicalDeath; // Physical health reaches 0
        public System.Action OnTrueDeath; // Core health reaches 0
        public System.Action OnRevivalStarted; // Revival process started
        public System.Action OnRevivalCompleted; // Revival completed
        
        // Health Tier Events
        public System.Action<HealthTier> OnPhysicalTierChanged;
        public System.Action<CrystalEnergyTier> OnCoreTierChanged;
        
        // Combat Events
        public System.Action<float> OnAttackLaunched; // damage dealt
        public System.Action<Transform> OnPlayerDetected; // player target
        public System.Action OnPlayerLost; // player lost

        // Attack Events
        public System.Action OnAttackWindowOpened; // attack window opened
        public System.Action OnAttackWindowClosed; // attack window closed
        public System.Action OnAttackSequenceFinished; // attack sequence finished
        
        // State Events
        public System.Action<string> OnStateChanged; // state name
        
        // Properties
        public EnemyRuntimeStats Stats => _stats;
        public EnemyStateMachine StateMachine => _stateMachine;
        public EnemyActionController ActionController => _actionController;
        public EnemyMovement Movement => _movement;
        public string CurrentState => _stateMachine?.CurrentStateName ?? "None";
        public Transform PlayerTarget => _playerTarget;
        public bool HasPlayerTarget => _hasPlayerTarget && _playerTarget != null;
        public Vector3 LastKnownPlayerPosition => _lastKnownPlayerPosition;
        public Vector3 PatrolCenter => _patrolCenter;
        public Vector3 CurrentPatrolTarget => _currentPatrolTarget;
        public bool IsPatrolling => _isPatrolling;
        public Vector3 PatrolWaypointA => _patrolWaypointA;
        public Vector3 PatrolWaypointB => _patrolWaypointB;
        
        // Patrol Configuration Properties
        public PatrolMode EnemyPatrolMode => _patrolMode;
        public int MaxPatrolCycles => _maxPatrolCycles;
        public float PatrolSpeed => _patrolSpeed;
        public float SingleCycleDuration => _singleCycleDuration;
        public float WaitAtWaypointDuration => _waitAtWaypointDuration;
        public int CurrentPatrolCycles => _currentPatrolCycles;
        
        // Chase Configuration Properties
        public float TargetUpdateInterval => _targetUpdateInterval;
        
        // Revival Configuration Properties
        public float RevivalTimer => _revivalTimer;
        
        // Health Properties
        public bool IsAlive => _stats.IsAlive;
        public bool IsCoreAlive => _stats.crystalCore != null && _stats.crystalCore.CoreHealthState == Utilities.CoreHealthState.Intact;
        public bool IsInPhysicalDeathState => _stats.IsAlive == false;
        
        // Health Tier Properties
        public HealthTier HealthTier => _stats.healthTier;
        public CrystalEnergyTier CoreTier => _stats.crystalCore.EnergyTier;
        
        // Combat Properties
        public bool CanAttack => IsAlive && IsCoreAlive && HasPlayerTarget && 
                                Time.time >= _lastAttackTime + _stats.normalAttackStats.cooldown;
        
        // Position Properties
        public Vector3 CurrentPosition => _movement?.CurrentPosition ?? _patrolCenter;
        
        // Animation-driven combat properties (read-only for animation bridge)
        public AttackStats NormalAttackStats => _stats.normalAttackStats;
        public AttackStats CoreAttackStats => _stats.coreAttackStats;
        public bool IsPlayerInAttackRangeValue => _isPlayerInAttackRange;
        public bool HasPlayerTargetValue => _hasPlayerTarget && _playerTarget != null;
        public float LastAttackTime => _lastAttackTime;

        public EnemyController(EnemyBaseStats baseStats, Vector3 spawnPosition, Transform enemyTransform = null)
        {
            Initialize(baseStats, spawnPosition, enemyTransform);
        }

        private void Initialize(EnemyBaseStats baseStats, Vector3 spawnPosition, Transform enemyTransform = null)
        {
            _stats = baseStats.CreateRuntimeStats();
            _patrolCenter = spawnPosition;
            _currentPatrolTarget = spawnPosition;
            
            // Initialize movement system with reference to this controller
            _movement = new EnemyMovement(_stats, enemyTransform, this);
            
            // Initialize action controller
            _actionController = new EnemyActionController(this);
            _actionController.OnActionStarted += (action) => Debug.Log($"EnemyController: Action started: {action.Name}");
            _actionController.OnActionFinished += (action) => Debug.Log($"EnemyController: Action finished: {action.Name}");
            _actionController.OnActionCancelled += (action) => Debug.Log($"EnemyController: Action cancelled: {action.Name}");
            _actionController.Initialize();
            
            // Initialize state machine
            _stateMachine = new EnemyStateMachine(this);
            _stateMachine.OnStateChanged += (stateName) => OnStateChanged?.Invoke(stateName);
            _stateMachine.Initialize();

            // Initialize damage hitbox
            _damageHitboxChild = enemyTransform.Find("DamageHitbox");
            
            // Register with SelectivePauseService
            RegisterWithPauseService();

            Debug.Log($"EnemyController: Initialized at {spawnPosition}");
        }

        /// <summary>
        /// Update enemy controller (called from MonoBehaviour)
        /// </summary>
        public void Update(float deltaTime)
        {
            UpdateRevivalTimer(deltaTime);
            _stats.UpdateChaos(deltaTime);
            UpdatePlayerDetection();
            _actionController?.Update(deltaTime);
            _movement?.Update(deltaTime);
            _stateMachine?.Update();
        }

        #region Health System

        /// <summary>
        /// Update revival timer
        /// </summary>
        private void UpdateRevivalTimer(float deltaTime)
        {
            if (_stateMachine.IsInState("Reviving"))
            {
                _revivalTimer += deltaTime;
                
                // Check if core health reached 0 during revival (interruption)
                if (!IsCoreAlive)
                {
                    if(IsAlive)
                    {
                        Debug.Log("EnemyController: Revival interrupted with core death - core health reached 0");
                        CompleteRevival();
                    }
                    else
                    {
                        Debug.Log("EnemyController: Revival interrupted with true death - core health reached 0");
                        HandleTrueDeath();
                    }
                    return;
                }
                
                // Revival progress - restore health health
                if (_stats.revivalRate > 0f && _stats.currentHealth < _stats.maxHealth)
                {
                    var previousTier = _stats.healthTier;
                    _stats.RestoreHealth(_stats.revivalRate * deltaTime);
                    _stats.UpdateHealthTier();
                    
                    OnHealthChanged?.Invoke(_stats.currentHealth, _stats.maxHealth);
                    
                    // Check for health tier change during revival
                    if (_stats.healthTier != previousTier)
                    {
                        OnPhysicalTierChanged?.Invoke(_stats.healthTier);
                    }

                    // Check if revival is complete
                    if (_stats.currentHealth >= _stats.maxHealth)
                    {
                        Debug.Log("EnemyController: Revival completed without interruption - health health restored to full");
                        CompleteRevival();
                    }
                }
            }
        }

        /// <summary>
        /// Take physical health damage
        /// </summary>
        public void TakeHealthDamage(float damage)
        {
            if (!IsCoreAlive) return;
            
            var previousTier = _stats.healthTier;
            _stats.TakeHealthDamage(damage);
            _stats.UpdateHealthTier();
            
            _timesHit++;
            _totalDamageTaken += damage;
            
            OnHealthChanged?.Invoke(_stats.currentHealth, _stats.maxHealth);
            
            // Check for health tier change
            if (_stats.healthTier != previousTier)
            {
                OnPhysicalTierChanged?.Invoke(_stats.healthTier);
            }
            
            // Notify action controller of damage taken
            _actionController?.OnEnemyDamageTaken();

            if (_stats.currentHealth <= 0f)
            {
                HandlePhysicalDeath();
            }
            
            Debug.Log($"EnemyController: Took {damage:F1} physical health damage. Current: {_stats.currentHealth:F1}/{_stats.maxHealth}");
        }

        /// <summary>
        /// Take core health damage
        /// </summary>
        public void TakeCoreDamage(float damage)
        {
            if (!IsCoreAlive) return;

            var previousTier = _stats.crystalCore.EnergyTier;
            _stats.crystalCore.TakeCoreHealthDamage(damage);
            _stats.crystalCore.UpdateCalculatedValues();
            
            _timesHit++;
            _totalDamageTaken += damage;
            
            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentCoreHealth);
            
            // Check for core tier change
            if (_stats.crystalCore.EnergyTier != previousTier)
            {
                OnCoreTierChanged?.Invoke(_stats.crystalCore.EnergyTier);
            }
            
            // Notify action controller of damage taken
            _actionController?.OnEnemyDamageTaken();

            if (_stats.crystalCore.CurrentCoreHealth <= 0f)
            {
                HandleTrueDeath();
            }
            
            Debug.Log($"EnemyController: Took {damage:F1} core health damage. Current: {_stats.crystalCore.CurrentCoreHealth:F1}/{_stats.crystalCore.MaxCoreHealth}");
        }

        /// <summary>
        /// Take chaos damage
        /// </summary>
        public void TakeChaosDamage(float damage)
        {
            if (!IsCoreAlive) return;

            _stats.crystalCore.AddChaos(damage);
            
            Debug.Log($"EnemyController: Took {damage:F1} chaos damage. Current chaos: {_stats.crystalCore.CurrentChaos:F1}/{_stats.crystalCore.MaxChaos}");
        }

        /// <summary>
        /// Handle physical health death (physical health reaches 0)
        /// Check core health to determine next state: Revival if core > 0, TrueDeath if core <= 0
        /// </summary>
        private void HandlePhysicalDeath()
        {
            // Prevent multiple calls
            if (_stateMachine?.IsReviving() == true || _stateMachine?.IsTrulyDead() == true)
            {
                return;
            }
            
            Debug.Log("EnemyController: Physical health depleted - checking core health for state transition");
            OnPhysicalDeath?.Invoke();
            
            // Check core health to determine next state
            if (IsCoreAlive)
            {
                // Core health > 0: Enter revival state
                Debug.Log("EnemyController: Core intact, entering revival state");
                bool revivalStarted = _stateMachine?.StartRevival() ?? false;
                if (revivalStarted)
                {
                    StartRevival(); // Initialize revival timer and trigger events
                }
            }
            else
            {
                // Core health <= 0: Enter true death state
                Debug.Log("EnemyController: Core destroyed, entering true death state");
                HandleTrueDeath();
            }
        }

        /// <summary>
        /// Handle true death (core health reaches 0)
        /// </summary>
        private void HandleTrueDeath()
        {
            Debug.Log("EnemyController: Core health depleted - enemy truly dead");
            OnTrueDeath?.Invoke();
            _stateMachine?.EnterTrueDeath();
        }

        /// <summary>
        /// Start revival process (called by state machine)
        /// </summary>
        public void StartRevival()
        {
            _revivalTimer = 0f;
            OnRevivalStarted?.Invoke();
            Debug.Log("EnemyController: Revival started");
        }

        /// <summary>
        /// Complete revival process
        /// </summary>
        private void CompleteRevival()
        {
            _revivalTimer = 0f;
            
            // Clear any existing hit tracking to ensure clean state
            _currentAttackHits.Clear();
            
            OnRevivalCompleted?.Invoke();
            _stateMachine?.CompleteRevival();
            Debug.Log("EnemyController: Revival completed, cleared hit tracking for new combat phase");
        }

        #endregion

        #region Combat System

        /// <summary>
        /// Start attack process (animation-driven) - sets cooldown and triggers events
        /// Actual damage is dealt through hitbox during animation window
        /// </summary>
        public bool LaunchAttack()
        {
            if (!CanAttack) return false;

            _lastAttackTime = Time.time;
            _attacksLaunched++;
            
            // Trigger attack started event (for animation system)
            OnAttackLaunched?.Invoke(_stats.normalAttackStats.damages.GetDamage(DamageType.PhysicalHealth));
            Debug.Log($"EnemyController: Attack process started - damage will be dealt through hitbox");
            
            return true;
        }

        /// <summary>
        /// Enable hitbox for damage dealing (called by animation events)
        /// </summary>
        public void EnableHitbox()
        {
            _hitboxEnabled = true;
            _currentAttackHits.Clear(); // Reset hit tracking for new attack
            
            // Find and enable the actual damage hitbox GameObject
            if (_damageHitboxChild != null)
            {
                _damageHitboxChild.gameObject.SetActive(true);
                Debug.Log("EnemyController: Hitbox enabled - damage window opened");
                OnAttackWindowOpened?.Invoke();
            }
            else
            {
                Debug.LogWarning("EnemyController: EnableHitbox called but no DamageHitbox child found!");
            }
        }

        /// <summary>
        /// Disable hitbox for damage dealing (called by animation events)
        /// </summary>
        public void DisableHitbox()
        {
            _hitboxEnabled = false;
            
            // Clear hit tracking when attack window ends
            _currentAttackHits.Clear();
            
            // Find and disable the actual damage hitbox GameObject
            if (_damageHitboxChild != null)
            {
                _damageHitboxChild.gameObject.SetActive(false);
                Debug.Log("EnemyController: Hitbox disabled - damage window closed, cleared hit tracking");
                OnAttackWindowClosed?.Invoke();
            }
        }

        /// <summary>
        /// Attack sequence finished
        /// </summary>
        public void AttackSequenceFinished()
        {
            OnAttackSequenceFinished?.Invoke();
        }

        /// <summary>
        /// Try to apply damage to a target through the hitbox system
        /// Only works when hitbox is enabled and target hasn't been hit in current attack
        /// </summary>
        public bool TryApplyDamage(IDamageable target, DamageInfo damageInfo)
        {
            if (!_hitboxEnabled)
            {
                Debug.LogWarning("EnemyController: Attempted to apply damage but hitbox is disabled");
                return false;
            }

            if (!IsCoreAlive)
            {
                Debug.LogWarning("EnemyController: Attempted to apply damage but enemy is not corely alive");
                return false;
            }

            if (target == null)
            {
                Debug.LogWarning("EnemyController: Attempted to apply damage to null target");
                return false;
            }

            // Check if we've already hit this target in the current attack
            if (_currentAttackHits.Contains(target))
            {
                // Debug.Log("EnemyController: Target already hit in current attack, skipping");
                return false;
            }

            // Apply damage
            target.TakeDamage(damageInfo);
            
            // Track this hit
            _currentAttackHits.Add(target);
            
            // Update statistics
            _totalDamageDealt += damageInfo.GetTotalDamage();
            
            Debug.Log($"EnemyController: Applied {damageInfo.GetTotalDamage():F1} damage to target");
            return true;
        }

        /// <summary>
        /// Check if hitbox is currently enabled
        /// </summary>
        public bool IsHitboxEnabled => _hitboxEnabled;

        /// <summary>
        /// Reset attack cooldown (for testing purposes)
        /// </summary>
        public void ResetAttackCooldown()
        {
            _lastAttackTime = 0f;
            Debug.Log("EnemyController: Attack cooldown reset");
        }

        /// <summary>
        /// Check if player is in attack range (now handled by trigger system)
        /// </summary>
        public bool IsPlayerInAttackRange()
        {
            // This will be set by the trigger system
            return _isPlayerInAttackRange;
        }

        /// <summary>
        /// Check if player is in detection range (now handled by trigger system)
        /// </summary>
        public bool IsPlayerInDetectionRange()
        {
            // This will be set by the trigger system
            return HasPlayerTarget;
        }

        #endregion

        #region Player Detection

        private void UpdatePlayerDetection()
        {
            // Update player tracking (position tracking only, range detection handled by triggers)
            if (HasPlayerTarget)
            {
                _lastKnownPlayerPosition = _playerTarget.position;
            }
        }

        private void FindPlayer()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                SetPlayerTarget(playerObject.transform);
            }
        }

        /// <summary>
        /// Set player as target
        /// </summary>
        public void SetPlayerTarget(Transform player)
        {
            _playerTarget = player;
            _hasPlayerTarget = true;
            _lastKnownPlayerPosition = player.position;
            OnPlayerDetected?.Invoke(player);
        }

        /// <summary>
        /// Lose player target
        /// </summary>
        public void LosePlayer()
        {
            _playerTarget = null;
            _hasPlayerTarget = false;
            _isPlayerInAttackRange = false; // Also reset attack range
            OnPlayerLost?.Invoke();
        }

        /// <summary>
        /// Set player in attack range (called by trigger system)
        /// </summary>
        public void SetPlayerInAttackRange(bool inRange)
        {
            _isPlayerInAttackRange = inRange;
        }

        #endregion

        #region Patrol System

        /// <summary>
        /// Set new patrol target
        /// </summary>
        public void SetPatrolTarget(Vector3 target)
        {
            _currentPatrolTarget = target;
            _isPatrolling = true;
        }

        /// <summary>
        /// Generate patrol point (uses waypoints if available, otherwise random)
        /// </summary>
        public Vector3 GeneratePatrolPoint()
        {
            // Use waypoint-based patrolling if waypoints are set
            if (HasPatrolWaypoints())
            {
                return GetNextPatrolWaypoint();
            }
            
            // Fallback to random patrol within radius
            Vector2 randomCircle = Random.insideUnitCircle * _stats.patrolRadius;
            Vector3 patrolPoint = _patrolCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);
            return patrolPoint;
        }
        
        /// <summary>
        /// Set patrol waypoints for linear patrolling
        /// </summary>
        public void SetPatrolWaypoints(Vector3 waypointA, Vector3 waypointB)
        {
            _patrolWaypointA = waypointA;
            _patrolWaypointB = waypointB;
            
            // Set patrol center to midpoint between waypoints
            _patrolCenter = (_patrolWaypointA + _patrolWaypointB) * 0.5f;
            
            Debug.Log($"EnemyController: Patrol waypoints set - A: {waypointA}, B: {waypointB}");
        }
        
        /// <summary>
        /// Check if patrol waypoints are configured
        /// </summary>
        public bool HasPatrolWaypoints()
        {
            return Vector3.Distance(_patrolWaypointA, _patrolWaypointB) > 0.1f;
        }
        
        /// <summary>
        /// Get the next patrol waypoint based on current direction
        /// </summary>
        public Vector3 GetNextPatrolWaypoint()
        {
            if (!HasPatrolWaypoints())
            {
                return _patrolCenter; // Fallback to center
            }
            
            return _movingToWaypointB ? _patrolWaypointB : _patrolWaypointA;
        }
        
        /// <summary>
        /// Switch patrol direction (called when reaching a waypoint)
        /// </summary>
        public void SwitchPatrolDirection()
        {
            _movingToWaypointB = !_movingToWaypointB;
            
            // Count cycles when returning to A (completing a full cycle)
            if (!_movingToWaypointB)
            {
                _currentPatrolCycles++;
                Debug.Log($"EnemyController: Completed patrol cycle {_currentPatrolCycles}/{(_patrolMode == PatrolMode.Limited ? _maxPatrolCycles : "∞")}");
            }
            
            Debug.Log($"EnemyController: Switched patrol direction, now moving to {(_movingToWaypointB ? "B" : "A")}");
        }
        
        /// <summary>
        /// Set patrol configuration
        /// </summary>
        public void SetPatrolConfiguration(
            PatrolMode mode,
            int maxCycles,
            float speed,
            float cycleDuration,
            float waitDuration)
        {
            _patrolMode = mode;
            _maxPatrolCycles = maxCycles;
            _patrolSpeed = speed;
            _singleCycleDuration = cycleDuration;
            _waitAtWaypointDuration = waitDuration;
            Debug.Log($"EnemyController: Patrol configuration set - Mode: {mode}, MaxCycles: {maxCycles}, Speed: {speed:F1}");
        }
        
        /// <summary>
        /// Set chase configuration
        /// </summary>
        public void SetChaseConfiguration(
            float targetUpdateInterval)
        {
            _targetUpdateInterval = targetUpdateInterval;
            Debug.Log($"EnemyController: Chase configuration set - UpdateInterval: {targetUpdateInterval:F2}s");
        }
        
        /// <summary>
        /// Check if patrol should stop (for Limited mode)
        /// </summary>
        public bool ShouldStopPatrol()
        {
            return _patrolMode == PatrolMode.Limited && _currentPatrolCycles >= _maxPatrolCycles;
        }
        
        /// <summary>
        /// Reset patrol cycle counter
        /// </summary>
        public void ResetPatrolCycles()
        {
            _currentPatrolCycles = 0;
            _currentCycleStartTime = Time.time;
            Debug.Log("EnemyController: Patrol cycles reset");
        }

        /// <summary>
        /// Stop patrolling
        /// </summary>
        public void StopPatrol()
        {
            _isPatrolling = false;
            _currentPatrolTarget = _patrolCenter;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get enemy statistics
        /// </summary>
        public string GetStats()
        {
            return $"Physical Health: {_stats.currentHealth:F1}/{_stats.maxHealth}, " +
                   $"Core Energy: {_stats.crystalCore.CurrentEnergy:F1}/{_stats.crystalCore.MaxEnergy}, " +
                   $"Core Health: {_stats.crystalCore.CurrentCoreHealth:F1}/{_stats.crystalCore.MaxCoreHealth}, " +
                   $"Hits Taken: {_timesHit}, Damage Taken: {_totalDamageTaken:F1}, " +
                   $"Attacks: {_attacksLaunched}, Damage Dealt: {_totalDamageDealt:F1}";
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        public void ResetStats()
        {
            _timesHit = 0;
            _totalDamageTaken = 0f;
            _totalDamageDealt = 0f;
            _attacksLaunched = 0;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Shutdown enemy controller
        /// </summary>
        public void Shutdown()
        {
            OnHealthChanged = null;
            OnCoreEnergyChanged = null;
            OnPhysicalDeath = null;
            OnTrueDeath = null;
            OnRevivalStarted = null;
            OnRevivalCompleted = null;
            OnAttackLaunched = null;
            OnPlayerDetected = null;
            OnPlayerLost = null;
            OnStateChanged = null;
            OnPhysicalTierChanged = null;
            OnCoreTierChanged = null;
            
            _actionController?.Cleanup();
            _stateMachine?.Shutdown();
            
            // Unregister from SelectivePauseService
            var pauseService = ServiceRegistry.Get<ISelectivePauseService>();
            if (pauseService != null)
            {
                pauseService.UnregisterPausable(this);
                Debug.Log("EnemyController: Unregistered from SelectivePauseService");
            }
            
            Debug.Log("EnemyController: Shutdown completed");
        }

        #endregion

        #region IPausable Implementation

        private bool _isPaused = false;

        public bool IsPaused => _isPaused;

        private void RegisterWithPauseService()
        {
            var pauseService = ServiceRegistry.Get<ISelectivePauseService>();
            if (pauseService != null)
            {
                pauseService.RegisterPausable(this);
                Debug.Log("EnemyController: Registered with SelectivePauseService");
            }
            else
            {
                Debug.LogWarning("EnemyController: SelectivePauseService not found, pause functionality will not work");
            }
        }

        public void Pause()
        {
            if (_isPaused) return;
            
            _isPaused = true;
            Debug.Log("EnemyController: Paused");
            
            // 暂停状态机更新
            // 暂停移动
            // 暂停动作控制器
        }

        public void Resume()
        {
            if (!_isPaused) return;
            
            _isPaused = false;
            Debug.Log("EnemyController: Resumed");
            
            // 恢复状态机更新
            // 恢复移动
            // 恢复动作控制器
        }

        #endregion
    }
}
