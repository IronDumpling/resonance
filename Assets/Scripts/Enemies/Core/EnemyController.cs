using UnityEngine;
using System.Collections.Generic;
using Resonance.Enemies;
using Resonance.Enemies.Data;
using Resonance.Enemies.Movement;
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
        private MovementSystem _movement;
        
        // State Data
        private EnemyStateData _stateData = new EnemyStateData();
        
        // State Tracking (behavior tree manages behavior, controller tracks data)
        private float _stunEndTime = 0f;
        
        // Combat State
        private float _lastNormalAttackTime = 0f;
        private float _lastWaveAttackTime = 0f;
        private float _revivalTimer = 0f;
        private bool _hitboxEnabled = false;
        private HashSet<IDamageable> _currentAttackHits = new HashSet<IDamageable>();
        private AttackType _currentAttackType = AttackType.Normal;
        
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
        private float _waitAtWaypointDuration = 1f;

        // Damage Hitbox
        private Transform _damageHitboxChild;

        // Chase Configuration
        private float _targetUpdateInterval = 0.5f;
        
        // Statistics
        private int _timesHit = 0;
        private Dictionary<DamageType, float> _totalDamageTaken;
        private Dictionary<DamageType, float> _totalDamageDealt;
        private Dictionary<AttackType, int> _attacksLaunchedCount;
        
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
        public MovementSystem Movement => _movement;
        public Transform PlayerTarget => _playerTarget;
        public bool HasPlayerTarget => _hasPlayerTarget && _playerTarget != null;
        public Vector3 LastKnownPlayerPosition => _lastKnownPlayerPosition;
        public Vector3 PatrolCenter => _patrolCenter;
        public Vector3 CurrentPatrolTarget => _currentPatrolTarget;
        public bool IsPatrolling => _isPatrolling;
        public Vector3 PatrolWaypointA => _patrolWaypointA;
        public Vector3 PatrolWaypointB => _patrolWaypointB;
        
        // Patrol Configuration Properties
        public float WaitAtWaypointDuration => _waitAtWaypointDuration;
        
        // Chase Configuration Properties
        public float TargetUpdateInterval => _targetUpdateInterval;
        
        // Revival Configuration Properties
        public float RevivalTimer => _revivalTimer;
        
        public EnemyStateData StateData => _stateData;
        
        // Health Properties
        public bool IsPhysicallyAlive => _stateData.IsPhysicallyAlive;
        public bool IsPhysicallyDead => _stateData.IsPhysicallyDead;
        public bool IsCoreDead => _stateData.IsCoreDead;
        
        // State Properties
        public EnemyState CurrentState => _stateData.CurrentState;

        /// <summary>
        /// Check if hitbox is currently enabled
        /// </summary>
        public bool IsHitboxEnabled => _hitboxEnabled;
        
        // Health Tier Properties
        public HealthTier HealthTier => _stats.healthTier;
        public CrystalEnergyTier CoreTier => _stats.crystalCore.EnergyTier;
        
        // Combat Properties
        // Can only normal attack if:
        // 1. Enemy is alive, has core alive, and has player target
        // 2. Player is in attack range
        // 3. Not on attack cooldown
        public bool CanNormalAttack => IsPhysicallyAlive && HasPlayerTarget && 
                                    Time.time >= _lastNormalAttackTime + _stats.normalAttackStats.cooldown;
        
        // Can only wave attack if:
        // 1. Enemy is alive, has core alive, and has player target
        // 2. Not on attack cooldown
        // 3. Has at least 1 energy slot to consume
        public bool CanWaveAttack => IsPhysicallyAlive && HasPlayerTarget && 
                                    Time.time >= _lastWaveAttackTime + _stats.waveAttackStats.cooldown &&
                                    _stats.crystalCore.CanConsumeSlot(); 
        
        public AttackType CurrentAttackType => _currentAttackType;
        
        /// <summary>
        /// Set current attack type (called by BehaviorTree action nodes)
        /// </summary>
        public void SetCurrentAttackType(AttackType attackType)
        {
            _currentAttackType = attackType;
        }
        
        // Position Properties
        public Vector3 CurrentPosition => _movement?.CurrentPosition ?? _patrolCenter;
        
        // Animation-driven combat properties (read-only for animation bridge)
        public AttackStats NormalAttackStats => _stats.normalAttackStats;
        public AttackStats WaveAttackStats => _stats.waveAttackStats;
        public bool IsPlayerInAttackRangeValue => _isPlayerInAttackRange;
        public bool HasPlayerTargetValue => _hasPlayerTarget && _playerTarget != null;

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
            _movement = new MovementSystem(_stats, enemyTransform, this);

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
                { DamageType.Chaos, 0f }
            };

            _totalDamageDealt = new Dictionary<DamageType, float>()
            {
                { DamageType.PhysicalHealth, 0f },
                { DamageType.CoreHealth, 0f },
                { DamageType.Chaos, 0f }
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
            bool isStunned = Time.time < _stunEndTime;
            _stateData.UpdateState(_stats.currentHealth, _stats.crystalCore.CurrentCoreHealth, isStunned);
            
            UpdateRevivalTimer(deltaTime);
            _stats.UpdateChaos(deltaTime);
            UpdatePlayerDetection();
            _movement?.Update(deltaTime);
        }

        #region Health System

        /// <summary>
        /// Update revival timer and restore health
        /// Called every frame when IsReviving is true (managed by BehaviorTree)
        /// </summary>
        private void UpdateRevivalTimer(float deltaTime)
        {
            if (CurrentState == EnemyState.Reviving)
            {
                _revivalTimer += deltaTime;
                
                // Revival progress - restore physical health
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
                }
            }
        }

        /// <summary>
        /// Take damage from a DamageInfo
        /// Processes all damage types from the same attack together
        /// Note: Enemies don't have invulnerability system currently
        /// </summary>
        public void TakeDamage(DamageInfo damageInfo)
        {
            // Process all damage types in the DamageInfo
            Damages damages = damageInfo.damages;
            if (damages == null) return;
            
            // Apply Physical Health damage
            if (damages.HasDamage(DamageType.PhysicalHealth))
            {
                float damageAmount = damages.GetDamage(DamageType.PhysicalHealth);
                TakeHealthDamage(damageAmount);
            }
            
            // Apply Core Health damage
            if (damages.HasDamage(DamageType.CoreHealth))
            {
                float damageAmount = damages.GetDamage(DamageType.CoreHealth);
                TakeCoreDamage(damageAmount);
            }
            
            // Apply Chaos damage (processed last)
            if (damages.HasDamage(DamageType.Chaos))
            {
                float damageAmount = damages.GetDamage(DamageType.Chaos);
                TakeChaosDamage(damageAmount);
            }
            
            Debug.Log($"EnemyController: Processed DamageInfo - {damageInfo}");
        }
        
        /// <summary>
        /// Take physical health damage
        /// </summary>
        private void TakeHealthDamage(float damage)
        {
            if (IsPhysicallyDead) return;
            
            var previousTier = _stats.healthTier;
            _stats.TakeHealthDamage(damage);
            _stats.UpdateHealthTier();
            
            _timesHit++;
            _totalDamageTaken[DamageType.PhysicalHealth] += damage;
            
            OnHealthChanged?.Invoke(_stats.currentHealth, _stats.maxHealth);
            
            // Check for health tier change
            if (_stats.healthTier != previousTier)
            {
                OnPhysicalTierChanged?.Invoke(_stats.healthTier);
            }

            if (_stats.currentHealth <= 0f)
            {
                HandlePhysicalDeath();
            }
            
            Debug.Log($"EnemyController: Took {damage:F1} physical health damage. Current: {_stats.currentHealth:F1}/{_stats.maxHealth}");
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
                HandleTrueDeath();
            }
            
            Debug.Log($"EnemyController: Took {damage:F1} core health damage. Current: {_stats.crystalCore.CurrentCoreHealth:F1}/{_stats.crystalCore.MaxCoreHealth}");
        }

        /// <summary>
        /// Take chaos damage
        /// </summary>
        private void TakeChaosDamage(float damage)
        {
            if (IsPhysicallyDead) return;

            float addedChaos = _stats.crystalCore.AddChaos(damage);
            
            if (addedChaos > 0f)
            {
                StartStun(addedChaos * 0.1f);
                Debug.Log($"EnemyController: Took {damage:F1} chaos damage. Stun duration: {addedChaos * 0.1f:F2}s");
            }
        }

        /// <summary>
        /// Start stun effect (called when chaos damage is taken)
        /// </summary>
        public void StartStun(float duration)
        {
            _stunEndTime = Time.time + duration;
            Debug.Log($"EnemyController: Stun started for {duration:F2}s");
        }

        /// <summary>
        /// Handle physical health death (physical health reaches 0)
        /// Triggers events, actual revival is managed by BehaviorTree
        /// </summary>
        private void HandlePhysicalDeath()
        {
            Debug.Log("EnemyController: Physical health depleted - triggering death event");
            OnPhysicalDeath?.Invoke();
            
            // If core is also destroyed, trigger true death
            if (IsCoreDead)
            {
                Debug.Log("EnemyController: Core also destroyed, triggering true death");
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
        }

        /// <summary>
        /// Start revival process (called by BehaviorTree)
        /// </summary>
        public void StartRevival()
        {
            _revivalTimer = 0f;
            OnRevivalStarted?.Invoke();
            Debug.Log("EnemyController: Revival started");
        }

        /// <summary>
        /// Complete revival process (called by BehaviorTree)
        /// </summary>
        public void CompleteRevival()
        {
            _revivalTimer = 0f;
            
            // Clear any existing hit tracking to ensure clean state
            _currentAttackHits.Clear();
            
            OnRevivalCompleted?.Invoke();
            Debug.Log("EnemyController: Revival completed, cleared hit tracking for new combat phase");
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
            if(damageInfo.damages.HasDamage(DamageType.Chaos))
            {
                _totalDamageDealt[DamageType.Chaos] += damageInfo.damages.GetDamage(DamageType.Chaos);
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

        /// <summary>
        /// Check if player target is in chaos state (for wave attack condition)
        /// </summary>
        public bool IsPlayerInChaosState()
        {
            if (!HasPlayerTarget) return false;
            
            // Try to get IDamageable from player target
            var damageable = _playerTarget.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = _playerTarget.GetComponentInParent<IDamageable>();
            }
            
            if (damageable == null)
            {
                return false;
            }
            
            bool isInChaos = damageable.ChaosState == WaveChaosState.Chaos;
            
            return isInChaos;
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
        /// Set chase configuration
        /// </summary>
        public void SetChaseConfiguration(
            float targetUpdateInterval)
        {
            _targetUpdateInterval = targetUpdateInterval;
            Debug.Log($"EnemyController: Chase configuration set - UpdateInterval: {targetUpdateInterval:F2}s");
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
                { DamageType.Chaos, 0f }
            };

            _totalDamageDealt = new Dictionary<DamageType, float>()
            {
                { DamageType.PhysicalHealth, 0f },
                { DamageType.CoreHealth, 0f },
                { DamageType.Chaos, 0f }
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
