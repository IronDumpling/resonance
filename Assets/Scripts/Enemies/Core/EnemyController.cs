using UnityEngine;
using Resonance.Enemies.Data;
using Resonance.Enemies.States;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces;

namespace Resonance.Enemies.Core
{
    /// <summary>
    /// Enemy核心控制器，管理敌人状态和行为
    /// 这是一个Non-MonoBehaviour类，处理敌人逻辑
    /// </summary>
    public class EnemyController
    {
        // Core Data
        private EnemyRuntimeStats _stats;
        private EnemyStateMachine _stateMachine;
        
        // Combat State
        private float _lastAttackTime = 0f;
        private float _revivalTimer = 0f;
        
        // Target Tracking
        private Transform _playerTarget;
        private Vector3 _lastKnownPlayerPosition;
        private bool _hasPlayerTarget = false;
        
        // Patrol State
        private Vector3 _patrolCenter;
        private Vector3 _currentPatrolTarget;
        private bool _isPatrolling = false;
        
        // Statistics
        private int _timesHit = 0;
        private float _totalDamageTaken = 0f;
        private float _totalDamageDealt = 0f;
        private int _attacksLaunched = 0;
        
        // Dual Health Events
        public System.Action<float, float> OnPhysicalHealthChanged; // current, max
        public System.Action<float, float> OnMentalHealthChanged; // current, max
        public System.Action OnPhysicalDeath; // Physical health reaches 0
        public System.Action OnTrueDeath; // Mental health reaches 0
        public System.Action OnRevivalStarted; // Revival process started
        public System.Action OnRevivalCompleted; // Revival completed
        
        // Combat Events
        public System.Action<float> OnAttackLaunched; // damage dealt
        public System.Action<Transform> OnPlayerDetected; // player target
        public System.Action OnPlayerLost; // player lost
        
        // State Events
        public System.Action<string> OnStateChanged; // state name
        
        // Properties
        public EnemyRuntimeStats Stats => _stats;
        public EnemyStateMachine StateMachine => _stateMachine;
        public string CurrentState => _stateMachine?.CurrentStateName ?? "None";
        public Transform PlayerTarget => _playerTarget;
        public bool HasPlayerTarget => _hasPlayerTarget && _playerTarget != null;
        public Vector3 LastKnownPlayerPosition => _lastKnownPlayerPosition;
        public Vector3 PatrolCenter => _patrolCenter;
        public Vector3 CurrentPatrolTarget => _currentPatrolTarget;
        public bool IsPatrolling => _isPatrolling;
        public float RevivalTimer => _revivalTimer;
        
        // Health Properties
        public bool IsPhysicallyAlive => _stats.IsPhysicallyAlive;
        public bool IsMentallyAlive => _stats.IsMentallyAlive;
        public bool IsInPhysicalDeathState => _stats.IsInPhysicalDeathState;
        
        // Combat Properties
        public bool CanAttack => IsMentallyAlive && HasPlayerTarget && 
                                Time.time >= _lastAttackTime + _stats.attackCooldown;

        public EnemyController(EnemyBaseStats baseStats, Vector3 spawnPosition)
        {
            Initialize(baseStats, spawnPosition);
        }

        private void Initialize(EnemyBaseStats baseStats, Vector3 spawnPosition)
        {
            _stats = baseStats.CreateRuntimeStats();
            _patrolCenter = spawnPosition;
            _currentPatrolTarget = spawnPosition;
            
            // Initialize state machine
            _stateMachine = new EnemyStateMachine(this);
            _stateMachine.OnStateChanged += (stateName) => OnStateChanged?.Invoke(stateName);
            _stateMachine.Initialize();
            
            Debug.Log($"EnemyController: Initialized at {spawnPosition}");
        }

        /// <summary>
        /// Update enemy controller (called from MonoBehaviour)
        /// </summary>
        public void Update(float deltaTime)
        {
            UpdateHealthRegeneration(deltaTime);
            UpdateRevivalTimer(deltaTime);
            UpdatePlayerDetection();
            _stateMachine?.Update();
        }

        #region Health System

        private void UpdateHealthRegeneration(float deltaTime)
        {
            bool healthChanged = false;
            
            // Physical health regeneration (only when physically alive)
            if (_stats.physicalHealthRegenRate > 0f && _stats.currentPhysicalHealth < _stats.maxPhysicalHealth && IsPhysicallyAlive)
            {
                _stats.currentPhysicalHealth = Mathf.Min(_stats.maxPhysicalHealth, 
                    _stats.currentPhysicalHealth + _stats.physicalHealthRegenRate * deltaTime);
                OnPhysicalHealthChanged?.Invoke(_stats.currentPhysicalHealth, _stats.maxPhysicalHealth);
                healthChanged = true;
            }
            
            // Mental health regeneration (only in normal state)
            if (_stateMachine.IsInState("Normal") && _stats.mentalHealthRegenRate > 0f && 
                _stats.currentMentalHealth < _stats.maxMentalHealth)
            {
                _stats.currentMentalHealth = Mathf.Min(_stats.maxMentalHealth, 
                    _stats.currentMentalHealth + _stats.mentalHealthRegenRate * deltaTime);
                OnMentalHealthChanged?.Invoke(_stats.currentMentalHealth, _stats.maxMentalHealth);
                healthChanged = true;
            }
        }

        private void UpdateRevivalTimer(float deltaTime)
        {
            if (_stateMachine.IsInState("Reviving"))
            {
                _revivalTimer += deltaTime;
                
                // Revival progress - restore physical health
                if (_stats.revivalRate > 0f && _stats.currentPhysicalHealth < _stats.maxPhysicalHealth)
                {
                    _stats.currentPhysicalHealth = Mathf.Min(_stats.maxPhysicalHealth, 
                        _stats.currentPhysicalHealth + _stats.revivalRate * deltaTime);
                    OnPhysicalHealthChanged?.Invoke(_stats.currentPhysicalHealth, _stats.maxPhysicalHealth);
                    
                    // Check if revival is complete
                    if (_stats.currentPhysicalHealth >= _stats.maxPhysicalHealth)
                    {
                        CompleteRevival();
                    }
                }
            }
        }

        /// <summary>
        /// Take physical damage (affects physical health)
        /// </summary>
        public void TakePhysicalDamage(float damage)
        {
            if (!IsMentallyAlive) return;

            _stats.currentPhysicalHealth = Mathf.Max(0f, _stats.currentPhysicalHealth - damage);
            _timesHit++;
            _totalDamageTaken += damage;
            
            OnPhysicalHealthChanged?.Invoke(_stats.currentPhysicalHealth, _stats.maxPhysicalHealth);

            if (_stats.currentPhysicalHealth <= 0f)
            {
                HandlePhysicalDeath();
            }
            
            Debug.Log($"EnemyController: Took {damage} physical damage, physical health: {_stats.currentPhysicalHealth}");
        }

        /// <summary>
        /// Take mental damage (affects mental health)
        /// </summary>
        public void TakeMentalDamage(float damage)
        {
            if (!IsMentallyAlive) return;

            _stats.currentMentalHealth = Mathf.Max(0f, _stats.currentMentalHealth - damage);
            _timesHit++;
            _totalDamageTaken += damage;
            
            OnMentalHealthChanged?.Invoke(_stats.currentMentalHealth, _stats.maxMentalHealth);

            if (_stats.currentMentalHealth <= 0f)
            {
                HandleTrueDeath();
            }
            
            Debug.Log($"EnemyController: Took {damage} mental damage, mental health: {_stats.currentMentalHealth}");
        }

        /// <summary>
        /// Handle physical death (physical health reaches 0)
        /// </summary>
        private void HandlePhysicalDeath()
        {
            // Prevent multiple calls
            if (_stateMachine?.IsPhysicallyDead() == true || _stateMachine?.IsTrulyDead() == true)
            {
                return;
            }
            
            Debug.Log("EnemyController: Physical death - entering physical death state");
            OnPhysicalDeath?.Invoke();
            _stateMachine?.EnterPhysicalDeath();
        }

        /// <summary>
        /// Handle true death (mental health reaches 0)
        /// </summary>
        private void HandleTrueDeath()
        {
            Debug.Log("EnemyController: True death - enemy destroyed");
            OnTrueDeath?.Invoke();
            _stateMachine?.EnterTrueDeath();
        }

        /// <summary>
        /// Start revival process
        /// </summary>
        public void StartRevival()
        {
            _revivalTimer = 0f;
            OnRevivalStarted?.Invoke();
            _stateMachine?.StartRevival();
            Debug.Log("EnemyController: Revival started");
        }

        /// <summary>
        /// Complete revival process
        /// </summary>
        private void CompleteRevival()
        {
            _revivalTimer = 0f;
            OnRevivalCompleted?.Invoke();
            _stateMachine?.CompleteRevival();
            Debug.Log("EnemyController: Revival completed");
        }

        #endregion

        #region Combat System

        /// <summary>
        /// Launch attack on player
        /// </summary>
        public bool LaunchAttack()
        {
            if (!CanAttack) return false;

            _lastAttackTime = Time.time;
            _attacksLaunched++;
            _totalDamageDealt += _stats.attackDamage;
            
            OnAttackLaunched?.Invoke(_stats.attackDamage);
            Debug.Log($"EnemyController: Launched attack for {_stats.attackDamage} damage");
            
            return true;
        }

        /// <summary>
        /// Check if player is in attack range
        /// </summary>
        public bool IsPlayerInAttackRange()
        {
            if (!HasPlayerTarget) return false;
            
            float distance = Vector3.Distance(_patrolCenter, _playerTarget.position);
            return distance <= _stats.attackRange;
        }

        /// <summary>
        /// Check if player is in detection range
        /// </summary>
        public bool IsPlayerInDetectionRange()
        {
            if (!HasPlayerTarget) return false;
            
            float distance = Vector3.Distance(_patrolCenter, _playerTarget.position);
            return distance <= _stats.detectionRange;
        }

        #endregion

        #region Player Detection

        private void UpdatePlayerDetection()
        {
            // Find player if we don't have one
            if (!HasPlayerTarget)
            {
                FindPlayer();
            }
            
            // Update player tracking
            if (HasPlayerTarget)
            {
                _lastKnownPlayerPosition = _playerTarget.position;
                
                // Check if player is still in detection range
                if (!IsPlayerInDetectionRange())
                {
                    LosePlayer();
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
            OnPlayerDetected?.Invoke(player);
            // Debug.Log($"EnemyController: Player detected at {player.position}");
        }

        /// <summary>
        /// Lose player target
        /// </summary>
        public void LosePlayer()
        {
            _playerTarget = null;
            _hasPlayerTarget = false;
            OnPlayerLost?.Invoke();
            // Debug.Log("EnemyController: Player lost");
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
        /// Generate random patrol point within patrol radius
        /// </summary>
        public Vector3 GeneratePatrolPoint()
        {
            Vector2 randomCircle = Random.insideUnitCircle * _stats.patrolRadius;
            Vector3 patrolPoint = _patrolCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);
            return patrolPoint;
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
            return $"Physical Health: {_stats.currentPhysicalHealth:F1}/{_stats.maxPhysicalHealth}, " +
                   $"Mental Health: {_stats.currentMentalHealth:F1}/{_stats.maxMentalHealth}, " +
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
            OnPhysicalHealthChanged = null;
            OnMentalHealthChanged = null;
            OnPhysicalDeath = null;
            OnTrueDeath = null;
            OnRevivalStarted = null;
            OnRevivalCompleted = null;
            OnAttackLaunched = null;
            OnPlayerDetected = null;
            OnPlayerLost = null;
            OnStateChanged = null;
            
            _stateMachine?.Shutdown();
            
            Debug.Log("EnemyController: Shutdown completed");
        }

        #endregion
    }
}
