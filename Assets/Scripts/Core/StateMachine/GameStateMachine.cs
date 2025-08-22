using UnityEngine;
using Resonance.Utilities;
using Resonance.Core;

namespace Resonance.Core.StateMachine
{
    public class GameStateMachine : MonoBehaviour
    {
        private BaseStateMachine _stateMachine;
        private bool _isInitialized = false;

        public BaseStateMachine StateMachine => _stateMachine;
        public IState CurrentState => _stateMachine?.CurrentState;

        void Awake()
        {
            _stateMachine = new BaseStateMachine();
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("GameStateMachine already initialized");
                return;
            }

            Debug.Log("GameStateMachine: Initializing");
            
            SetupStates();
            
            _isInitialized = true;
            Debug.Log("GameStateMachine: Initialized successfully");
        }

        private void SetupStates()
        {
            // Add basic game states
            _stateMachine.AddState(new InitializingState());
            _stateMachine.AddState(new MainMenuState());
            _stateMachine.AddState(new GameplayState());
            _stateMachine.AddState(new PausedState());

            // Start with initializing state
            _stateMachine.ChangeState("Initializing");
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
            return _stateMachine?.ChangeState(stateName) ?? false;
        }

        public bool ChangeState(IState state)
        {
            return _stateMachine?.ChangeState(state) ?? false;
        }

        public T GetState<T>(string stateName) where T : class, IState
        {
            return _stateMachine?.GetState<T>(stateName);
        }

        public void Shutdown()
        {
            if (_isInitialized)
            {
                Debug.Log("GameStateMachine: Shutting down");
                _stateMachine?.Clear();
                _isInitialized = false;
            }
        }
    }

    // Basic game states
    public class InitializingState : IState
    {
        public string Name => "Initializing";

        public void Enter()
        {
            Debug.Log("State: Entering Initializing");
        }

        public void Update()
        {
            // Auto-transition to main menu after initialization
            if (GameManager.Instance.Services != null)
            {
                GameManager.Instance.StateMachine.ChangeState("MainMenu");
            }
        }

        public void Exit()
        {
            Debug.Log("State: Exiting Initializing");
        }

        public bool CanTransitionTo(IState newState)
        {
            return newState.Name == "MainMenu";
        }
    }

    public class MainMenuState : IState
    {
        public string Name => "MainMenu";

        public void Enter()
        {
            Debug.Log("State: Entering MainMenu");
        }

        public void Update()
        {
            // Handle main menu logic
        }

        public void Exit()
        {
            Debug.Log("State: Exiting MainMenu");
        }

        public bool CanTransitionTo(IState newState)
        {
            return newState.Name == "Gameplay";
        }
    }

    public class GameplayState : IState
    {
        public string Name => "Gameplay";

        public void Enter()
        {
            Debug.Log("State: Entering Gameplay");
        }

        public void Update()
        {
            // Handle gameplay logic
        }

        public void Exit()
        {
            Debug.Log("State: Exiting Gameplay");
        }

        public bool CanTransitionTo(IState newState)
        {
            return newState.Name == "Paused" || newState.Name == "MainMenu";
        }
    }

    public class PausedState : IState
    {
        public string Name => "Paused";

        public void Enter()
        {
            Debug.Log("State: Entering Paused");
            GameManager.Instance.PauseGame();
        }

        public void Update()
        {
            // Handle pause menu logic
        }

        public void Exit()
        {
            Debug.Log("State: Exiting Paused");
            GameManager.Instance.ResumeGame();
        }

        public bool CanTransitionTo(IState newState)
        {
            return newState.Name == "Gameplay" || newState.Name == "MainMenu";
        }
    }
}
