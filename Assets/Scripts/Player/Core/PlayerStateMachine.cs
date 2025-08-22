using UnityEngine;
using Resonance.Utilities;
using Resonance.Core;
using Resonance.Player.States;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Player-specific state machine that manages player states like Normal, Aiming, Interacting, and Dead.
    /// Inherits from BaseStateMachine to provide player-specific functionality.
    /// </summary>
    public class PlayerStateMachine
    {
        private BaseStateMachine _stateMachine;
        private PlayerController _playerController;
        private bool _isInitialized = false;

        public BaseStateMachine StateMachine => _stateMachine;
        public IState CurrentState => _stateMachine?.CurrentState;
        public string CurrentStateName => _stateMachine?.CurrentState?.Name ?? "None";
        public string CurrentStatePath => _stateMachine?.CurrentStatePath ?? "";

        // Events
        public System.Action<string> OnStateChanged;

        public PlayerStateMachine(PlayerController playerController)
        {
            _playerController = playerController;
            _stateMachine = new BaseStateMachine();
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("PlayerStateMachine already initialized");
                return;
            }

            Debug.Log("PlayerStateMachine: Initializing");
            
            SetupStates();
            
            _isInitialized = true;
            Debug.Log("PlayerStateMachine: Initialized successfully");
        }

        private void SetupStates()
        {
            // Add player states
            _stateMachine.AddState(new PlayerNormalState(_playerController));
            _stateMachine.AddState(new PlayerAimingState(_playerController));
            _stateMachine.AddState(new PlayerInteractingState(_playerController));
            _stateMachine.AddState(new PlayerDeadState(_playerController));

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
                Debug.LogWarning("PlayerStateMachine not initialized");
                return false;
            }

            string previousState = CurrentStateName;
            bool success = _stateMachine?.ChangeState(stateName) ?? false;
            
            if (success && previousState != CurrentStateName)
            {
                OnStateChanged?.Invoke(CurrentStateName);
                Debug.Log($"PlayerStateMachine: Changed from {previousState} to {CurrentStateName}");
            }
            
            return success;
        }

        public bool ChangeState(IState state)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("PlayerStateMachine not initialized");
                return false;
            }

            string previousState = CurrentStateName;
            bool success = _stateMachine?.ChangeState(state) ?? false;
            
            if (success && previousState != CurrentStateName)
            {
                OnStateChanged?.Invoke(CurrentStateName);
                Debug.Log($"PlayerStateMachine: Changed from {previousState} to {CurrentStateName}");
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

        #region Player-Specific State Transitions

        public bool StartAiming()
        {
            if (IsInState("Normal"))
            {
                return ChangeState("Aiming");
            }
            return false;
        }

        public bool StopAiming()
        {
            if (IsInState("Aiming"))
            {
                return ChangeState("Normal");
            }
            return false;
        }

        public bool StartInteracting()
        {
            if (IsInState("Normal"))
            {
                return ChangeState("Interacting");
            }
            return false;
        }

        public bool StopInteracting()
        {
            if (IsInState("Interacting"))
            {
                return ChangeState("Normal");
            }
            return false;
        }

        public bool Die()
        {
            // Can die from any state except already dead
            if (!IsInState("Dead"))
            {
                return ChangeState("Dead");
            }
            return false;
        }

        public bool Respawn()
        {
            if (IsInState("Dead"))
            {
                return ChangeState("Normal");
            }
            return false;
        }

        #endregion

        #region State Queries

        public bool CanMove()
        {
            return IsInState("Normal") || IsInState("Aiming");
        }

        public bool CanRun()
        {
            return IsInState("Normal");
        }

        public bool CanInteract()
        {
            return IsInState("Normal");
        }

        public bool CanAim()
        {
            return IsInState("Normal") && _playerController.HasWeapon;
        }

        public bool CanShoot()
        {
            return IsInState("Aiming") && _playerController.HasWeapon && _playerController.WeaponManager.CanShoot();
        }

        #endregion

        public void Shutdown()
        {
            if (_isInitialized)
            {
                Debug.Log("PlayerStateMachine: Shutting down");
                OnStateChanged = null;
                _stateMachine?.Clear();
                _isInitialized = false;
            }
        }
    }
}
