using UnityEngine;
using System.Collections.Generic;
using Resonance.Gameplay.Enemies;
using Resonance.Gameplay.Enemies.Data;
using Resonance.Gameplay.Enemies.Movement;
using Resonance.Gameplay.Enemies.Triggers;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Shared.Types;
using Resonance.Systems.CrystalCore;
using Resonance.Shared.Interfaces;
using Resonance.Shared.Interfaces.Services;

namespace Resonance.Gameplay.Enemies.Core
{
    /// <summary>
    /// Enemy Controller, manages enemy state and behavior
    /// This is a Non-MonoBehaviour class, handling enemy logic
    /// </summary>
    public class EnemyController : IPausable
    {
        // Core Data
        private EnemyRuntimeStats _stats;
        private EnemyMovement _movement;
        private EnemyVision _vision;
        
        // State Data
        private EnemyStateData _stateData = new EnemyStateData();
        
        // State Tracking (behavior tree manages behavior, controller tracks data)
        private float _staggerEndTime = 0f;  // Stagger end time (balance damage causes stagger)
        private float _unbalancedTimer = 0f;  // Timer for unbalanced state
        
        // Combat State
        private float _lastNormalAttackTime = 0f;
        private float _lastWaveAttackTime = 0f;
        private bool _hitboxEnabled = false;
        private HashSet<IDamageable> _currentAttackHits = new HashSet<IDamageable>();
        private AttackType _currentAttackType = AttackType.Normal;
        
        // Target Tracking
        private Transform _playerTarget;
        private Vector3 _lastKnownPlayerPosition;
        private bool _hasPlayerTarget = false;
        private bool _isPlayerInAttackRange = false;
        private float _timeSinceLastSawPlayer = 0f; // Time since last saw player
        
        // Patrol State
        private Vector3 _patrolCenter;
        private Vector3 _currentPatrolTarget;
        private bool _isPatrolling = false;
        private Vector3 _patrolWaypointA;
        private Vector3 _patrolWaypointB;
        private bool _movingToWaypointB = true; // true = moving to B, false = moving to A
        
        // Patrol Configuration
        private float _waitAtWaypointDuration = 1f;

        // Damage Hitbox
        private Transform _damageHitboxChild;
        
        // Statistics
        private int _timesHit = 0;
        private Dictionary<DamageType, float> _totalDamageTaken;
        private Dictionary<DamageType, float> _totalDamageDealt;
        private Dictionary<AttackType, int> _attacksLaunchedCount;
        
        // Balance System Events
        public System.Action<float, float> OnBalanceChanged; // current, max
        public System.Action<float, float> OnCoreEnergyChanged; // current, max
        public System.Action OnUnbalanced; // Balance reaches 0
        public System.Action OnDeath; // Core health reaches 0
        public System.Action OnUnbalancedStarted; // Unbalanced state started
        public System.Action OnUnbalancedCompleted; // Unbalanced state completed
        public System.Action OnCoreExposureStarted; // Core exposure started (wave execution)
        public System.Action OnCoreExposureCompleted; // Core exposure completed
        
        // Balance Tier Events
        public System.Action<BalanceTier> OnBalanceTierChanged;
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
        public EnemyMovement Movement => _movement;
        public EnemyVision Vision => _vision;
        public Transform PlayerTarget => _playerTarget;
        public bool HasPlayerTarget => _hasPlayerTarget && _playerTarget != null;
        public bool IsPlayerInAttackRange => _isPlayerInAttackRange;
        public Vector3 LastKnownPlayerPosition => _lastKnownPlayerPosition;
        public Vector3 PatrolCenter => _patrolCenter;
        public Vector3 CurrentPatrolTarget => _currentPatrolTarget;
        public bool IsPatrolling => _isPatrolling;
        public Vector3 PatrolWaypointA => _patrolWaypointA;
        public Vector3 PatrolWaypointB => _patrolWaypointB;
        
        // Patrol Configuration Properties
        public float WaitAtWaypointDuration => _waitAtWaypointDuration;
        
        // Unbalanced State Properties
        public float UnbalancedTimer => _unbalancedTimer;
        
        public EnemyStateData StateData => _stateData;

        // Movement Properties
        public Vector3 CurrentPosition => _movement?.CurrentPosition ?? _patrolCenter;
        
        // Balance Properties
        public bool IsBalanced => _stateData.IsBalanced;
        public bool IsUnbalanced => _stateData.IsUnbalanced;
        public bool IsCoreDead => _stateData.IsCoreDead;
        
        // State Properties
        public EnemyState CurrentState => _stateData.CurrentState;

        /// <summary>
        /// Check if hitbox is currently enabled
        /// </summary>
        public bool IsHitboxEnabled => _hitboxEnabled;
        
        // Balance Tier Properties
        public BalanceTier BalanceTier => _stats.balanceTier;
        public CrystalEnergyTier CoreTier => _stats.crystalCore.EnergyTier;
        
        // Combat Properties
        // Can only normal attack if:
        // 1. Enemy is balanced (balance > 0), has core alive, and has player target
        // 2. Player is in attack range
        // 3. Not on attack cooldown
        public bool CanNormalAttack => IsBalanced && HasPlayerTarget && 
                                    Time.time >= _lastNormalAttackTime + _stats.normalAttackStats.cooldown;
        
        // Can only wave attack if:
        // 1. Enemy is balanced (balance > 0), has core alive, and has player target
        // 2. Not on attack cooldown
        // 3. Has at least 1 energy slot to consume
        public bool CanWaveAttack => IsBalanced && HasPlayerTarget && 
                                    Time.time >= _lastWaveAttackTime + _stats.waveAttackStats.cooldown &&
                                    _stats.crystalCore.CanConsumeSlot(); 
        
        // Combat properties
        public AttackType CurrentAttackType => _currentAttackType;
        public AttackStats NormalAttackStats => _stats.normalAttackStats;
        public AttackStats WaveAttackStats => _stats.waveAttackStats;

        public EnemyController(EnemyBaseStats baseStats, Vector3 spawnPosition, Transform enemyTransform = null)
        {
            Initialize(baseStats, spawnPosition, enemyTransform);
        }

        private void Initialize(EnemyBaseStats baseStats, Vector3 spawnPosition, Transform enemyTransform = null)
        {
            _stats = baseStats.CreateRuntimeStats();
            _patrolCenter = spawnPosition;
            _currentPatrolTarget = spawnPosition;
            _lastKnownPlayerPosition = spawnPosition; // Initialize to spawn position
            
            // Initialize movement system with reference to this controller
            _movement = new EnemyMovement(_stats, enemyTransform, this);

            // Initialize vision system
            if (enemyTransform != null)
            {
                // Try to get existing EnemyVision component, or add one if it doesn't exist
                _vision = enemyTransform.GetComponent<EnemyVision>();
                if (_vision == null)
                {
                    _vision = enemyTransform.gameObject.AddComponent<EnemyVision>();
                }
                _vision.Initialize(enemyTransform, _stats);
                Debug.Log($"EnemyController: Vision system initialized");
            }
            else
            {
                Debug.LogWarning($"EnemyController: No enemy transform provided, vision system disabled");
            }

            // Initialize damage hitbox
            _damageHitboxChild = enemyTransform?.Find("DamageHitbox");
            
            // Register with SelectivePauseService
            RegisterWithPauseService();

            InitializeStatistics();

            Debug.Log($"EnemyController: Initialized at {spawnPosition}");
        }

        private void InitializeStatistics()
        {
            _timesHit = 0;

            _totalDamageTaken = new Dictionary<DamageType, float>()
            {
                { DamageType.PhysicalHealth, 0f },
                { DamageType.CoreHealth, 0f },
                { DamageType.Balance, 0f }
            };

            _totalDamageDealt = new Dictionary<DamageType, float>()
            {
                { DamageType.PhysicalHealth, 0f },
                { DamageType.CoreHealth, 0f },
                { DamageType.Balance, 0f }
            };
            
            _attacksLaunchedCount = new Dictionary<AttackType, int>()
            {
                { AttackType.Normal, 0 },
                { AttackType.Wave, 0 }
            };
        }

        /// <summary>
        /// Update enemy controller (called from MonoBehaviour)
        /// Only updates data, behavior is handled by BehaviorTree
        /// </summary>
        public void Update(float deltaTime)
        {
            bool isStaggered = Time.time < _staggerEndTime;
            _stateData.UpdateState(_stats.currentBalance, _stats.crystalCore.CurrentCoreHealth, isStaggered);
            
            UpdateBalanceRecovery(deltaTime);
            UpdatePlayerDetection();
            _movement?.Update(deltaTime);
        }

        #region Balance System

        /// <summary>
        /// Update balance recovery
        /// Balance recovers naturally over time in Normal, Staggered states
        /// Recovers at different rate in CoreExposed state
        /// Does NOT recover in Unbalanced state
        /// </summary>
        private void UpdateBalanceRecovery(float deltaTime)
        {
            // Only recover if not at max and not in Unbalanced state
            if (_stats.currentBalance >= _stats.maxBalance) return;
            if (CurrentState == EnemyState.Unbalanced) return;
            
            // Determine recovery rate based on state
            float recoveryRate = _stats.balanceRecoveryRate;
            
            if (CurrentState == EnemyState.CoreExposed)
            {
                // Slower recovery when core is exposed
                recoveryRate = _stats.balanceRecoveryRateInCoreExposed;
            }
            
            if (recoveryRate > 0f)
            {
                var previousTier = _stats.balanceTier;
                float restored = _stats.RestoreBalance(recoveryRate * deltaTime);
                
                OnBalanceChanged?.Invoke(_stats.currentBalance, _stats.maxBalance);
                
                // Check for balance tier change
                if (_stats.balanceTier != previousTier)
                {
                    OnBalanceTierChanged?.Invoke(_stats.balanceTier);
                }
                
                // Check if balance is fully restored in CoreExposed state
                if (CurrentState == EnemyState.CoreExposed && _stats.currentBalance >= _stats.maxBalance)
                {
                    Debug.Log($"[EnemyController] Balance fully restored in CoreExposed state, triggering completion");
                }
            }
        }

        /// <summary>
        /// Take damage from a DamageInfo
        /// Enemies only have Balance and Core Health, no Physical Health
        /// Note: Enemies don't have invulnerability system currently
        /// </summary>
        public void TakeDamage(DamageInfo damageInfo)
        {
            // Process all damage types in the DamageInfo
            Damages damages = damageInfo.damages;
            if (damages == null) return;
            
            // Apply Core Health damage
            if (damages.HasDamage(DamageType.CoreHealth))
            {
                float damageAmount = damages.GetDamage(DamageType.CoreHealth);
                TakeCoreDamage(damageAmount);
            }
            
            // Apply Balance damage (stance/posture damage)
            if (damages.HasDamage(DamageType.Balance))
            {
                float damageAmount = damages.GetDamage(DamageType.Balance);
                TakeBalanceDamage(damageAmount);
            }
            
            Debug.Log($"EnemyController: Processed DamageInfo - {damageInfo}");
        }
        
        /// <summary>
        /// Take balance damage (stance/posture damage)
        /// Reduces balance, causes stagger, and triggers unbalanced state when balance reaches 0
        /// </summary>
        private void TakeBalanceDamage(float damage)
        {
            if (IsCoreDead) return;
            if (CurrentState == EnemyState.Unbalanced || CurrentState == EnemyState.CoreExposed) return;
            
            var previousTier = _stats.balanceTier;
            float actualDamage = _stats.TakeBalanceDamage(damage);
            
            _timesHit++;
            _totalDamageTaken[DamageType.Balance] += actualDamage;
            
            OnBalanceChanged?.Invoke(_stats.currentBalance, _stats.maxBalance);
            
            // Check for balance tier change
            if (_stats.balanceTier != previousTier)
            {
                OnBalanceTierChanged?.Invoke(_stats.balanceTier);
            }

            // Apply stagger effect based on damage amount
            if (actualDamage > 0f)
            {
                float staggerDuration = actualDamage * _stats.staggerDurationPerDamage;
                StartStagger(staggerDuration);
            }

            // Check if balance reaches 0
            if (_stats.currentBalance <= 0f)
            {
                HandleUnbalanced();
            }
            
            Debug.Log($"EnemyController: Took {actualDamage:F1} balance damage. Current: {_stats.currentBalance:F1}/{_stats.maxBalance}, Stagger: {actualDamage * _stats.staggerDurationPerDamage:F2}s");
        }

        /// <summary>
        /// Take core health damage
        /// </summary>
        private void TakeCoreDamage(float damage)
        {
            if (IsCoreDead) return;

            var previousTier = _stats.crystalCore.EnergyTier;
            _stats.crystalCore.TakeCoreHealthDamage(damage);
            _stats.crystalCore.UpdateCalculatedValues();
            
            _timesHit++;
            _totalDamageTaken[DamageType.CoreHealth] += damage;
            
            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentCoreHealth);
            
            // Check for core tier change
            if (_stats.crystalCore.EnergyTier != previousTier)
            {
                OnCoreTierChanged?.Invoke(_stats.crystalCore.EnergyTier);
            }

            if (_stats.crystalCore.CurrentCoreHealth <= 0f)
            {
                HandleDeath();
            }
            
            Debug.Log($"EnemyController: Took {damage:F1} core health damage. Current: {_stats.crystalCore.CurrentCoreHealth:F1}/{_stats.crystalCore.MaxCoreHealth}");
        }

        /// <summary>
        /// Start stagger effect (called when balance damage is taken)
        /// </summary>
        public void StartStagger(float duration)
        {
            _staggerEndTime = Time.time + duration;
            Debug.Log($"EnemyController: Stagger started for {duration:F2}s");
        }

        /// <summary>
        /// Handle unbalanced state (balance reaches 0)
        /// Triggers events, actual state management is handled by BehaviorTree
        /// </summary>
        private void HandleUnbalanced()
        {
            Debug.Log("EnemyController: Balance depleted - entering unbalanced state");
            OnUnbalanced?.Invoke();
            
            // If core is also destroyed, trigger death
            if (IsCoreDead)
            {
                Debug.Log("EnemyController: Core also destroyed, triggering death");
                HandleDeath();
            }
        }

        /// <summary>
        /// Handle death (core health reaches 0)
        /// </summary>
        private void HandleDeath()
        {
            Debug.Log("EnemyController: Core health depleted - enemy dead");
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Start unbalanced state (called by BehaviorTree)
        /// </summary>
        public void StartUnbalanced()
        {
            _unbalancedTimer = 0f;
            _stateData.StartUnbalanced();  // Set flag in StateData to keep state as Unbalanced
            OnUnbalancedStarted?.Invoke();
            Debug.Log("EnemyController: Unbalanced state started");
        }

        /// <summary>
        /// Complete unbalanced state (called by BehaviorTree when timer expires)
        /// Restores balance and returns to Normal state
        /// </summary>
        public void CompleteUnbalanced()
        {
            _unbalancedTimer = 0f;
            _stateData.CompleteUnbalanced();  // Clear flag in StateData
            
            // Restore balance to max when unbalanced timer expires
            _stats.currentBalance = _stats.maxBalance;
            OnBalanceChanged?.Invoke(_stats.currentBalance, _stats.maxBalance);
            
            // Clear any existing hit tracking to ensure clean state
            _currentAttackHits.Clear();
            
            OnUnbalancedCompleted?.Invoke();
            Debug.Log("EnemyController: Unbalanced state completed, balance restored");
        }

        /// <summary>
        /// Start core exposure state (called when player executes wave attack)
        /// </summary>
        public void StartCoreExposure()
        {
            _stateData.StartCoreExposure();  // Set flag in StateData to keep state as CoreExposed
            OnCoreExposureStarted?.Invoke();
            Debug.Log("EnemyController: Core exposure started - being executed by player");
        }

        /// <summary>
        /// Complete core exposure state (called when balance is fully restored)
        /// </summary>
        public void CompleteCoreExposure()
        {
            _stateData.CompleteCoreExposure();  // Clear flag in StateData
            
            // Clear any existing hit tracking to ensure clean state
            _currentAttackHits.Clear();
            
            OnCoreExposureCompleted?.Invoke();
            Debug.Log("EnemyController: Core exposure completed, returning to normal");
        }

        #endregion

        #region Combat System

        /// <summary>
        /// Start attack process (animation-driven) - sets cooldown and triggers events
        /// Actual damage is dealt through hitbox during animation window
        /// </summary>
        public bool LaunchNormalAttack()
        {
            if (!CanNormalAttack) return false;

            _currentAttackType = AttackType.Normal;
            _lastNormalAttackTime = Time.time;
            _attacksLaunchedCount[AttackType.Normal]++;
            
            // Trigger attack started event (for animation system)
            OnAttackLaunched?.Invoke(_stats.normalAttackStats.damages.GetDamage(DamageType.PhysicalHealth));
            Debug.Log($"EnemyController: Normal attack process started - damage will be dealt through hitbox");
            
            return true;
        }

        /// <summary>
        /// Start wave attack process - targets player's core when they are in chaos state
        /// </summary>
        public bool LaunchWaveAttack()
        {
            if (!CanWaveAttack) return false;

            _currentAttackType = AttackType.Wave;
            _lastWaveAttackTime = Time.time;
            _attacksLaunchedCount[AttackType.Wave]++;
            
            // Trigger attack started event (for animation system)
            OnAttackLaunched?.Invoke(_stats.waveAttackStats.damages.GetDamage(DamageType.CoreHealth));
            Debug.Log($"EnemyController: Wave attack process started - targeting player's core");
            
            return true;
        }

        /// <summary>
        /// Get the attack stats for the current attack type
        /// </summary>
        public AttackStats GetCurrentAttackStats()
        {
            switch (_currentAttackType)
            {
                case AttackType.Normal:
                    return _stats.normalAttackStats;
                case AttackType.Wave:
                    return _stats.waveAttackStats;
                default:
                    throw new System.Exception($"EnemyController: Invalid attack type: {_currentAttackType}");
            }
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
            
            // Reset attack type to normal
            _currentAttackType = AttackType.Normal;
            
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

            if (IsCoreDead)
            {
                Debug.LogWarning("EnemyController: Attempted to apply damage but enemy is core dead");
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
                return false;
            }

            // Apply damage
            target.TakeDamage(damageInfo);
            
            // Track this hit
            _currentAttackHits.Add(target);
            
            // Energy System: Normal attacks gain energy when hitting
            if (_currentAttackType == AttackType.Normal)
            {
                // Gain energy equal to physical damage dealt (or a fixed amount)
                float physicalDamage = damageInfo.damages.GetDamage(DamageType.PhysicalHealth);
                float energyGained = physicalDamage * _stats.normalAttackToEnergyRatio;
                if (energyGained > 0f)
                {
                    _stats.crystalCore.AddEnergy(energyGained);
                    Debug.Log($"EnemyController: Normal attack hit! Gained {energyGained:F0} energy. Current: {_stats.crystalCore.CurrentEnergy:F0}/{_stats.crystalCore.MaxEnergy:F0}");
                }
            }
            
            // Update statistics
            if(damageInfo.damages.HasDamage(DamageType.PhysicalHealth))
            {
                _totalDamageDealt[DamageType.PhysicalHealth] += damageInfo.damages.GetDamage(DamageType.PhysicalHealth);
            }
            if(damageInfo.damages.HasDamage(DamageType.CoreHealth))
            {
                _totalDamageDealt[DamageType.CoreHealth] += damageInfo.damages.GetDamage(DamageType.CoreHealth);
            }
            if(damageInfo.damages.HasDamage(DamageType.Balance))
            {
                _totalDamageDealt[DamageType.Balance] += damageInfo.damages.GetDamage(DamageType.Balance);
            }
            
            Debug.Log($"EnemyController: Applied {damageInfo.damages.ToString()} damage to target");
            return true;
        }

        /// <summary>
        /// Reset attack cooldown (for testing purposes)
        /// </summary>
        public void ResetAttackCooldown()
        {
            _lastNormalAttackTime = 0f;
            _lastWaveAttackTime = 0f;
            Debug.Log("EnemyController: Attack cooldowns reset (Normal and Core)");
        }

        #endregion

        #region Player Detection

        private void UpdatePlayerDetection()
        {
            // Use vision system to detect player
            if (_vision != null)
            {
                bool canSeePlayer = _vision.CanSeePlayer();
                
                if (canSeePlayer)
                {
                    // Reset timer when we can see the player
                    _timeSinceLastSawPlayer = 0f;
                    
                    // Vision system updates the last known position internally
                    // Update our local last known position from vision system
                    if (_vision.HasLastKnownPosition)
                    {
                        _lastKnownPlayerPosition = _vision.LastKnownPlayerPosition;
                    }
                    
                    // If we can see player but don't have target set, set it
                    if (!_hasPlayerTarget)
                    {
                        FindPlayer();
                    }
                    else
                    {
                        // Already has target, keep tracking the position
                        if (HasPlayerTarget)
                        {
                            _lastKnownPlayerPosition = _playerTarget.position;
                            
                            // Update vision system's last known position as well
                            if (_vision != null)
                            {
                                _vision.UpdateLastKnownPosition(_playerTarget.position);
                            }
                        }
                    }
                }
                else
                {
                    // Lost sight of player - check if we should lose target
                    if (_hasPlayerTarget)
                    {
                        _timeSinceLastSawPlayer += Time.deltaTime;
                        
                        // Lose target if lost vision for too long
                        if (_timeSinceLastSawPlayer >= _stats.visionLossTimeout)
                        {
                            Debug.Log($"[EnemyController] ✗ TARGET LOST: vision timeout");
                            LosePlayer();
                        }
                    }
                }
            }
            else
            {
                // Fallback: if no vision system, use old tracking method
                if (HasPlayerTarget)
                {
                    _lastKnownPlayerPosition = _playerTarget.position;
                }
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
            _timeSinceLastSawPlayer = 0f; // Reset vision loss timer when acquiring target
            
            // Also update vision system's last known position
            if (_vision != null)
            {
                _vision.UpdateLastKnownPosition(player.position);
            }
            
            Debug.Log($"[EnemyController] ✓ TARGET ACQUIRED: {player.name}");
            
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
            _timeSinceLastSawPlayer = 0f; // Reset vision loss timer
            
            // Note: We don't clear the vision system's last known position here
            // This allows the enemy to remember where they last saw the player
            
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
                Debug.Log($"EnemyController: Completed patrol cycle.");
            }
            
            Debug.Log($"EnemyController: Switched patrol direction, now moving to {(_movingToWaypointB ? "B" : "A")}");
        }
        
        /// <summary>
        /// Set patrol configuration
        /// </summary>
        public void SetPatrolConfiguration(
            float waitDuration)
        {
            _waitAtWaypointDuration = waitDuration;
            Debug.Log($"EnemyController: Patrol configuration set - WaitDuration: {waitDuration:F1}s");
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
            return $"Balance: {_stats.currentBalance:F1}/{_stats.maxBalance}, " +
                   $"Core Energy: {_stats.crystalCore.CurrentEnergy:F1}/{_stats.crystalCore.MaxEnergy}, " +
                   $"Core Health: {_stats.crystalCore.CurrentCoreHealth:F1}/{_stats.crystalCore.MaxCoreHealth}, " +
                   $"Hits Taken: {_timesHit}, Damage Taken: {_totalDamageTaken.ToString()}, " +
                   $"Attacks: {_attacksLaunchedCount.ToString()}, Damage Dealt: {_totalDamageDealt.ToString()}";
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        public void ResetStats()
        {
            _timesHit = 0;

            _totalDamageTaken = new Dictionary<DamageType, float>()
            {
                { DamageType.PhysicalHealth, 0f },
                { DamageType.CoreHealth, 0f },
                { DamageType.Balance, 0f }
            };

            _totalDamageDealt = new Dictionary<DamageType, float>()
            {
                { DamageType.PhysicalHealth, 0f },
                { DamageType.CoreHealth, 0f },
                { DamageType.Balance, 0f }
            };
            
            _attacksLaunchedCount = new Dictionary<AttackType, int>()
            {
                { AttackType.Normal, 0 },
                { AttackType.Wave, 0 }
            };
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Shutdown enemy controller
        /// </summary>
        public void Shutdown()
        {
            OnBalanceChanged = null;
            OnCoreEnergyChanged = null;
            OnUnbalanced = null;
            OnDeath = null;
            OnUnbalancedStarted = null;
            OnUnbalancedCompleted = null;
            OnCoreExposureStarted = null;
            OnCoreExposureCompleted = null;
            OnAttackLaunched = null;
            OnPlayerDetected = null;
            OnPlayerLost = null;
            OnStateChanged = null;
            OnBalanceTierChanged = null;
            OnCoreTierChanged = null;
            OnAttackWindowOpened = null;
            OnAttackWindowClosed = null;
            OnAttackSequenceFinished = null;
            
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
        }

        public void Resume()
        {
            if (!_isPaused) return;
            
            _isPaused = false;
            Debug.Log("EnemyController: Resumed");
        }

        #endregion
    }
}
