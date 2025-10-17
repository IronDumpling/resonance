using UnityEngine;
using Resonance.Utilities;
using Resonance.Core;
using Resonance.Enemies.States;

namespace Resonance.Enemies.Core
{
    /// <summary>
    /// Enemy-specific state machine that manages enemy states: Normal, Reviving, and TrueDeath.
    /// Handles transitions based on health and core health status.
    /// </summary>
    public class EnemyStateMachine
    {
        private BaseStateMachine _stateMachine;
        private EnemyController _enemyController;
        private bool _isInitialized = false;

        public BaseStateMachine StateMachine => _stateMachine;
        public IState CurrentState => _stateMachine?.CurrentState;
        public string CurrentStateName => _stateMachine?.CurrentState?.Name ?? "None";
        public string CurrentStatePath => _stateMachine?.CurrentStatePath ?? "";

        // Events
        public System.Action<string> OnStateChanged;

        public EnemyStateMachine(EnemyController enemyController)
        {
            _enemyController = enemyController;
            _stateMachine = new BaseStateMachine();
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("EnemyStateMachine already initialized");
                return;
            }

            Debug.Log("EnemyStateMachine: Initializing");
            
            SetupStates();
            
            _isInitialized = true;
            Debug.Log("EnemyStateMachine: Initialized successfully");
        }

        private void SetupStates()
        {
            // Add enemy states
            _stateMachine.AddState(new EnemyNormalState(_enemyController));
            _stateMachine.AddState(new EnemyRevivingState(_enemyController));
            _stateMachine.AddState(new EnemyTrueDeathState(_enemyController));
            _stateMachine.AddState(new EnemyStunState(_enemyController));

            // Start with normal state
            _stateMachine.ChangeState("Normal");
        }

        public void Update()
        {
            if (_isInitialized)
            {
                _stateMachine?.Update();
            }
        }

        public bool ChangeState(string stateName)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("EnemyStateMachine not initialized");
                return false;
            }

            string previousState = CurrentStateName;
            bool success = _stateMachine?.ChangeState(stateName) ?? false;
            
            if (success && previousState != CurrentStateName)
            {
                OnStateChanged?.Invoke(CurrentStateName);
                Debug.Log($"EnemyStateMachine: Changed from {previousState} to {CurrentStateName}");
            }
            
            return success;
        }

        public bool ChangeState(IState state)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("EnemyStateMachine not initialized");
                return false;
            }

            string previousState = CurrentStateName;
            bool success = _stateMachine?.ChangeState(state) ?? false;
            
            if (success && previousState != CurrentStateName)
            {
                OnStateChanged?.Invoke(CurrentStateName);
                Debug.Log($"EnemyStateMachine: Changed from {previousState} to {CurrentStateName}");
            }
            
            return success;
        }

        public T GetState<T>(string stateName) where T : class, IState
        {
            return _stateMachine?.GetState<T>(stateName);
        }

        public bool HasState(string stateName)
        {
            return _stateMachine?.HasState(stateName) ?? false;
        }

        public bool IsInState(string stateName)
        {
            return CurrentStateName == stateName;
        }

        #region State Transitions

        /// <summary>
        /// Start revival process when health health reaches 0
        /// </summary>
        public bool StartRevival()
        {
            if (IsInState("Normal"))
            {
                return ChangeState("Reviving");
            }
            Debug.Log("EnemyStateMachine: Cannot start revival - not in normal state");
            return false;
        }

        public bool CompleteRevival()
        {
            if (IsInState("Reviving"))
            {
                return ChangeState("Normal");
            }
            Debug.Log("EnemyStateMachine: Cannot complete revival - not in reviving state");
            return false;
        }

        public bool EnterTrueDeath()
        {
            // Can enter true death from any state except already in true death
            if (!IsInState("TrueDeath"))
            {
                return ChangeState("TrueDeath");
            }
            return false;
        }

        public bool EnterStun()
        {
            // Can enter stun from any state except already in stun
            if (!IsInState("Stun"))
            {
                Debug.Log($"EnemyStateMachine: Attempting to enter Stun state from {CurrentStateName}");
                bool result = ChangeState("Stun");
                if (!result)
                {
                    Debug.LogError($"EnemyStateMachine: Failed to change to Stun state from {CurrentStateName}");
                }
                return result;
            }
            else
            {
                Debug.LogWarning("EnemyStateMachine: Already in Stun state, cannot enter again");
            }
            return false;
        }

        public bool ExitStun()
        {
            // Can exit stun from any state except already in stun
            if (IsInState("Stun"))
            {
                return ChangeState("Normal");
            }
            return false;
        }

        #endregion

        #region State Queries

        public bool CanMove()
        {
            return IsInState("Normal");
        }

        public bool CanNormalAttack()
        {
            return IsInState("Normal");
        }

        public bool IsReviving()
        {
            return IsInState("Reviving");
        }

        public bool IsTrulyDead()
        {
            return IsInState("TrueDeath");
        }

        public bool IsAlive()
        {
            return IsInState("Normal");
        }

        public bool CanEnterStun()
        {
            return IsInState("Normal");
        }

        public bool IsStunned()
        {
            return IsInState("Stun");
        }

        #endregion

        #region Advanced State Queries

        /// <summary>
        /// Get current sub-state if in Normal state
        /// </summary>
        public string GetNormalSubState()
        {
            if (IsInState("Normal"))
            {
                var normalState = GetState<EnemyNormalState>("Normal");
                return normalState?.GetCurrentSubState() ?? "Unknown";
            }
            return "Not in Normal state";
        }

        /// <summary>
        /// Check if enemy is currently attacking
        /// </summary>
        public bool IsAttacking()
        {
            if (IsInState("Normal"))
            {
                var normalState = GetState<EnemyNormalState>("Normal");
                return normalState?.IsAttacking() ?? false;
            }
            return false;
        }

        /// <summary>
        /// Check if enemy is chase
        /// </summary>
        public bool IsChasing()
        {
            if (IsInState("Normal"))
            {
                var normalState = GetState<EnemyNormalState>("Normal");
                return normalState?.IsChasing() ?? false;
            }
            return false;
        }

        /// <summary>
        /// Get revival progress (0-1) if reviving
        /// </summary>
        public float GetRevivalProgress()
        {
            if (IsInState("Reviving"))
            {
                var revivingState = GetState<EnemyRevivingState>("Reviving");
                return revivingState?.GetRevivalProgress() ?? 0f;
            }
            return 0f;
        }

        /// <summary>
        /// Get time remaining until destruction if in true death
        /// </summary>
        public float GetDestructionTimeRemaining()
        {
            if (IsInState("TrueDeath"))
            {
                var trueDeathState = GetState<EnemyTrueDeathState>("TrueDeath");
                return trueDeathState?.GetDestructionTimeRemaining() ?? 0f;
            }
            return 0f;
        }

        /// <summary>
        /// Check if ready for destruction
        /// </summary>
        public bool IsReadyForDestruction()
        {
            if (IsInState("TrueDeath"))
            {
                var trueDeathState = GetState<EnemyTrueDeathState>("TrueDeath");
                return trueDeathState?.IsReadyForDestruction() ?? false;
            }
            return false;
        }

        #endregion

        public void Shutdown()
        {
            if (_isInitialized)
            {
                Debug.Log("EnemyStateMachine: Shutting down");
                OnStateChanged = null;
                _stateMachine?.Clear();
                _isInitialized = false;
            }
        }
    }
}
