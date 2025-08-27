using UnityEngine;
using Resonance.Core;
using Resonance.Enemies.Core;
using Resonance.Enemies.Actions;

namespace Resonance.Enemies.States
{
    /// <summary>
    /// Enemy正常状态，包含Patrol、Alert、Combat三个子状态
    /// 每个子状态使用对应的Action执行具体行为
    /// </summary>
    public class EnemyNormalState : IState
    {
        private EnemyController _enemyController;
        private EnemySubState _currentSubState;
        private float _stateTimer = 0f;
        // private float _patrolTimer = 0f;
        private float _alertTimer = 0f;
        
        // Sub-state timings
        private const float PATROL_INTERVAL = 3f;
        private const float ALERT_DURATION = 5f;
        private const float ATTACK_INTERVAL = 0.1f; // Check attack every 0.1s
        
        public string Name => "Normal";

        private enum EnemySubState
        {
            Patrol,    // Patrolling between waypoints - uses PatrolAction
            Alert,     // Player detected, chasing - uses ChaseAction
            Combat     // Player in attack range - uses AttackAction
        }

        public EnemyNormalState(EnemyController enemyController)
        {
            _enemyController = enemyController;
        }

        public void Enter()
        {
            Debug.Log("EnemyState: Entered Normal state");
            
            // Determine initial sub-state based on player detection
            if (_enemyController.HasPlayerTarget && _enemyController.IsPlayerInAttackRange())
            {
                _currentSubState = EnemySubState.Combat;
            }
            else if (_enemyController.HasPlayerTarget && _enemyController.IsPlayerInDetectionRange())
            {
                _currentSubState = EnemySubState.Alert;
            }
            else
            {
                _currentSubState = EnemySubState.Patrol;
            }
            
            _stateTimer = 0f;
            // _patrolTimer = 0f;
            _alertTimer = 0f;
            
            // Start the appropriate action for the initial sub-state
            StartActionForSubState(_currentSubState);
            
            // Subscribe to enemy events
            _enemyController.OnPlayerDetected += HandlePlayerDetected;
            _enemyController.OnPlayerLost += HandlePlayerLost;
            
            Debug.Log($"EnemyNormalState: Started in {_currentSubState} sub-state");
        }

        public void Update()
        {
            _stateTimer += Time.deltaTime;
            
            switch (_currentSubState)
            {
                case EnemySubState.Patrol:
                    UpdatePatrolBehavior();
                    break;
                case EnemySubState.Alert:
                    UpdateAlertBehavior();
                    break;
                case EnemySubState.Combat:
                    UpdateCombatBehavior();
                    break;
            }
        }

        public void Exit()
        {
            Debug.Log("EnemyState: Exited Normal state");
            
            // Stop current action
            StopCurrentAction();
            
            // Unsubscribe from enemy events
            _enemyController.OnPlayerDetected -= HandlePlayerDetected;
            _enemyController.OnPlayerLost -= HandlePlayerLost;
            
            // Stop any ongoing behaviors
            _enemyController.StopPatrol();
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition to revival and true death states from Normal
            return newState.Name == "Reviving" || newState.Name == "TrueDeath";
        }

        #region Sub-State Updates

        private void UpdatePatrolBehavior()
        {
            // Check for player detection
            if (_enemyController.HasPlayerTarget && _enemyController.IsPlayerInDetectionRange())
            {
                if (_enemyController.IsPlayerInAttackRange())
                {
                    TransitionToCombat();
                }
                else
                {
                    TransitionToAlert();
                }
                return;
            }
            
            // PatrolAction handles the actual patrolling behavior
            // We just need to ensure it's running
            EnsureActionIsRunning("Patrol");
        }

        private void UpdateAlertBehavior()
        {
            _alertTimer += Time.deltaTime;
            
            // Check if player is still detected
            if (!_enemyController.HasPlayerTarget || !_enemyController.IsPlayerInDetectionRange())
            {
                // Player lost, return to patrol after alert duration
                if (_alertTimer >= ALERT_DURATION)
                {
                    TransitionToPatrol();
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
                    TransitionToCombat();
                    return;
                }
            }
            
            // ChaseAction handles the actual chasing behavior
            EnsureActionIsRunning("Chase");
        }

        private void UpdateCombatBehavior()
        {
            // Check if player is still in attack range
            if (!_enemyController.HasPlayerTarget || !_enemyController.IsPlayerInAttackRange())
            {
                // Player moved out of attack range
                if (_enemyController.HasPlayerTarget && _enemyController.IsPlayerInDetectionRange())
                {
                    TransitionToAlert(); // Still detected, switch to chase
                }
                else
                {
                    TransitionToPatrol(); // Lost player completely
                }
                return;
            }
            
            // AttackAction handles the actual attacking behavior
            EnsureActionIsRunning("Attack");
        }

        #endregion

        #region Sub-State Transitions

        private void TransitionToPatrol()
        {
            if (_currentSubState == EnemySubState.Patrol) return;
            
            Debug.Log("EnemyNormalState: Transitioning to Patrol sub-state");
            StopCurrentAction();
            _currentSubState = EnemySubState.Patrol;
            _alertTimer = 0f;
            // _patrolTimer = 0f;
            StartActionForSubState(EnemySubState.Patrol);
        }

        private void TransitionToAlert()
        {
            if (_currentSubState == EnemySubState.Alert) return;
            
            Debug.Log("EnemyNormalState: Transitioning to Alert sub-state");
            StopCurrentAction();
            _currentSubState = EnemySubState.Alert;
            _alertTimer = 0f;
            StartActionForSubState(EnemySubState.Alert);
        }

        private void TransitionToCombat()
        {
            if (_currentSubState == EnemySubState.Combat) return;
            
            Debug.Log("EnemyNormalState: Transitioning to Combat sub-state");
            StopCurrentAction();
            _currentSubState = EnemySubState.Combat;
            StartActionForSubState(EnemySubState.Combat);
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerDetected(Transform player)
        {
            // Player detected - transition to appropriate sub-state
            if (_enemyController.IsPlayerInAttackRange())
            {
                TransitionToCombat();
            }
            else if (_enemyController.IsPlayerInDetectionRange())
            {
                TransitionToAlert();
            }
        }

        private void HandlePlayerLost()
        {
            // Player lost - start alert timer to eventually return to patrol
            if (_currentSubState == EnemySubState.Alert || _currentSubState == EnemySubState.Combat)
            {
                _alertTimer = 0f; // Will trigger transition to patrol after ALERT_DURATION
            }
        }

        #endregion

        #region Action Management
        
        /// <summary>
        /// Start the appropriate action for the given sub-state
        /// </summary>
        private void StartActionForSubState(EnemySubState subState)
        {
            var actionController = _enemyController.ActionController;
            
            // First ensure all actions are registered (only register if not already registered)
            EnsureActionsRegistered();
            
            // Then try to start the appropriate action
            switch (subState)
            {
                case EnemySubState.Patrol:
                    actionController.TryStartAction("Patrol");
                    break;
                    
                case EnemySubState.Alert:
                    actionController.TryStartAction("Chase");
                    break;
                    
                case EnemySubState.Combat:
                    actionController.TryStartAction("Attack");
                    break;
            }
        }
        
        /// <summary>
        /// Ensure all required actions are registered (only register once)
        /// </summary>
        private void EnsureActionsRegistered()
        {
            var actionController = _enemyController.ActionController;
            
            // Register each action only if it's not already registered
            if (!actionController.HasAction("Patrol"))
            {
                actionController.RegisterAction(new EnemyPatrolAction());
            }
            
            if (!actionController.HasAction("Chase"))
            {
                actionController.RegisterAction(new EnemyChaseAction());
            }
            
            if (!actionController.HasAction("Attack"))
            {
                actionController.RegisterAction(new EnemyAttackAction());
            }
        }
        
        /// <summary>
        /// Ensure the correct action is running for current sub-state
        /// </summary>
        private void EnsureActionIsRunning(string expectedActionName)
        {
            var actionController = _enemyController.ActionController;
            
            if (!actionController.IsActive || actionController.CurrentActionName != expectedActionName)
            {
                // Current action finished or wrong action running, try to start the expected action
                actionController.TryStartAction(expectedActionName);
            }
        }
        
        /// <summary>
        /// Stop the current action
        /// </summary>
        private void StopCurrentAction()
        {
            var actionController = _enemyController.ActionController;
            
            if (actionController.IsActive)
            {
                actionController.CancelCurrentAction();
            }
            
            // Unregister all actions
            actionController.UnregisterAction("Patrol");
            actionController.UnregisterAction("Chase");
            actionController.UnregisterAction("Attack");
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
        /// Check if enemy is currently in combat
        /// </summary>
        public bool IsAttacking()
        {
            return _currentSubState == EnemySubState.Combat;
        }

        /// <summary>
        /// Check if enemy is alert (chasing)
        /// </summary>
        public bool IsAlert()
        {
            return _currentSubState == EnemySubState.Alert;
        }

        /// <summary>
        /// Check if enemy is patrolling
        /// </summary>
        public bool IsPatrolling()
        {
            return _currentSubState == EnemySubState.Patrol;
        }
        
        /// <summary>
        /// Check if enemy is in normal behavior (patrolling)
        /// </summary>
        public bool IsNormal()
        {
            return _currentSubState == EnemySubState.Patrol;
        }

        #endregion
    }
}
