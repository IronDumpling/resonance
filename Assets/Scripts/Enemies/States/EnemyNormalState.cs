using UnityEngine;
using Resonance.Core;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.States
{
    /// <summary>
    /// Enemy正常状态，包含Normal、Alert、Attack子状态
    /// </summary>
    public class EnemyNormalState : IState
    {
        private EnemyController _enemyController;
        private EnemySubState _currentSubState;
        private float _stateTimer = 0f;
        private float _patrolTimer = 0f;
        private float _alertTimer = 0f;
        
        // Sub-state timings
        private const float PATROL_INTERVAL = 3f;
        private const float ALERT_DURATION = 5f;
        private const float ATTACK_INTERVAL = 0.1f; // Check attack every 0.1s
        
        public string Name => "Normal";

        private enum EnemySubState
        {
            Normal,    // Idle or patrolling
            Alert,     // Player detected, moving towards player
            Attack     // Attacking player
        }

        public EnemyNormalState(EnemyController enemyController)
        {
            _enemyController = enemyController;
        }

        public void Enter()
        {
            Debug.Log("EnemyState: Entered Normal state");
            
            _currentSubState = EnemySubState.Normal;
            _stateTimer = 0f;
            _patrolTimer = 0f;
            _alertTimer = 0f;
            
            // Subscribe to enemy events
            _enemyController.OnPlayerDetected += HandlePlayerDetected;
            _enemyController.OnPlayerLost += HandlePlayerLost;
        }

        public void Update()
        {
            _stateTimer += Time.deltaTime;
            
            switch (_currentSubState)
            {
                case EnemySubState.Normal:
                    UpdateNormalBehavior();
                    break;
                case EnemySubState.Alert:
                    UpdateAlertBehavior();
                    break;
                case EnemySubState.Attack:
                    UpdateAttackBehavior();
                    break;
            }
        }

        public void Exit()
        {
            Debug.Log("EnemyState: Exited Normal state");
            
            // Unsubscribe from enemy events
            _enemyController.OnPlayerDetected -= HandlePlayerDetected;
            _enemyController.OnPlayerLost -= HandlePlayerLost;
            
            // Stop any ongoing behaviors
            _enemyController.StopPatrol();
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition to death states from Normal
            return newState.Name == "PhysicalDeath" || newState.Name == "TrueDeath";
        }

        #region Sub-State Updates

        private void UpdateNormalBehavior()
        {
            // Check for player detection
            if (_enemyController.HasPlayerTarget && _enemyController.IsPlayerInDetectionRange())
            {
                TransitionToAlert();
                return;
            }
            
            // Handle patrolling
            _patrolTimer += Time.deltaTime;
            if (_patrolTimer >= PATROL_INTERVAL)
            {
                _patrolTimer = 0f;
                StartPatrol();
            }
        }

        private void UpdateAlertBehavior()
        {
            _alertTimer += Time.deltaTime;
            
            // Check if player is still detected
            if (!_enemyController.HasPlayerTarget || !_enemyController.IsPlayerInDetectionRange())
            {
                // Player lost, return to normal after alert duration
                if (_alertTimer >= ALERT_DURATION)
                {
                    TransitionToNormal();
                    return;
                }
            }
            else
            {
                // Reset alert timer if player is still detected
                _alertTimer = 0f;
                
                // Check if player is in attack range
                if (_enemyController.IsPlayerInAttackRange())
                {
                    TransitionToAttack();
                    return;
                }
            }
            
            // Move towards player's last known position
            // TODO: Implement movement logic when EnemyMovement is created
        }

        private void UpdateAttackBehavior()
        {
            // Check if player is still in attack range
            if (!_enemyController.HasPlayerTarget || !_enemyController.IsPlayerInAttackRange())
            {
                TransitionToAlert();
                return;
            }
            
            // Try to attack
            if (_enemyController.CanAttack)
            {
                _enemyController.LaunchAttack();
                Debug.Log("EnemyNormalState: Attack launched!");
            }
        }

        #endregion

        #region Sub-State Transitions

        private void TransitionToNormal()
        {
            _currentSubState = EnemySubState.Normal;
            _alertTimer = 0f;
            _patrolTimer = 0f;
            Debug.Log("EnemyNormalState: Transitioned to Normal sub-state");
        }

        private void TransitionToAlert()
        {
            _currentSubState = EnemySubState.Alert;
            _alertTimer = 0f;
            Debug.Log("EnemyNormalState: Transitioned to Alert sub-state");
        }

        private void TransitionToAttack()
        {
            _currentSubState = EnemySubState.Attack;
            Debug.Log("EnemyNormalState: Transitioned to Attack sub-state");
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerDetected(Transform player)
        {
            if (_currentSubState == EnemySubState.Normal)
            {
                TransitionToAlert();
            }
        }

        private void HandlePlayerLost()
        {
            if (_currentSubState == EnemySubState.Alert || _currentSubState == EnemySubState.Attack)
            {
                // Start alert timer to eventually return to normal
                _alertTimer = 0f;
            }
        }

        #endregion

        #region Patrol Logic

        private void StartPatrol()
        {
            Vector3 patrolPoint = _enemyController.GeneratePatrolPoint();
            _enemyController.SetPatrolTarget(patrolPoint);
            Debug.Log($"EnemyNormalState: Started patrol to {patrolPoint}");
        }

        #endregion

        #region Public Queries

        /// <summary>
        /// Get current sub-state name
        /// </summary>
        public string GetCurrentSubState()
        {
            return _currentSubState.ToString();
        }

        /// <summary>
        /// Check if enemy is currently attacking
        /// </summary>
        public bool IsAttacking()
        {
            return _currentSubState == EnemySubState.Attack;
        }

        /// <summary>
        /// Check if enemy is alert
        /// </summary>
        public bool IsAlert()
        {
            return _currentSubState == EnemySubState.Alert;
        }

        /// <summary>
        /// Check if enemy is in normal behavior
        /// </summary>
        public bool IsNormal()
        {
            return _currentSubState == EnemySubState.Normal;
        }

        #endregion
    }
}
