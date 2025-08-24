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
            _stateMachine.AddState(new PlayerPhysicalDeathState(_playerController));
            _stateMachine.AddState(new PlayerTrueDeathState(_playerController));
            _stateMachine.AddState(new PlayerCoreState());
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
            if (CanAim())
            {
                return ChangeState("Aiming");
            }
            Debug.Log("PlayerStateMachine: Cannot start aiming - no weapon equipped or not in Normal state");
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

        public bool EnterPhysicalDeath()
        {
            // Can enter physical death from any state except already in death states
            if (!IsInState("PhysicalDeath") && !IsInState("TrueDeath"))
            {
                return ChangeState("PhysicalDeath");
            }
            return false;
        }

        public bool EnterTrueDeath()
        {
            // Can enter true death from any state
            if (!IsInState("TrueDeath"))
            {
                return ChangeState("TrueDeath");
            }
            return false;
        }

        public bool Respawn()
        {
            // Can respawn from physical death or true death (through external systems)
            if (IsInState("PhysicalDeath") || IsInState("TrueDeath"))
            {
                return ChangeState("Normal");
            }
            return false;
        }

        #endregion

        #region State Queries

        public bool CanMove()
        {
            return IsInState("Normal") || IsInState("Aiming") || IsInState("PhysicalDeath");
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
            return IsInState("Normal") && _playerController.HasEquippedWeapon;
        }

        public bool CanShoot()
        {
            return IsInState("Aiming") && _playerController.HasEquippedWeapon && _playerController.WeaponManager.CanShoot();
        }

        public bool IsPhysicallyDead()
        {
            return IsInState("PhysicalDeath");
        }

        public bool IsTrulyDead()
        {
            return IsInState("TrueDeath");
        }

        public bool IsAlive()
        {
            return !IsInState("PhysicalDeath") && !IsInState("TrueDeath");
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
