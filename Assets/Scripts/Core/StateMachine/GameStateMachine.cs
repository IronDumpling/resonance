using UnityEngine;
using Resonance.Utilities;
using Resonance.Core;
using Resonance.Core.StateMachine.States;

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

        public bool ChangeState(string statePath)
        {
            return _stateMachine?.ChangeState(statePath) ?? false;
        }

        public string GetCurrentStatePath()
        {
            return _stateMachine?.CurrentStatePath ?? "";
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
}
