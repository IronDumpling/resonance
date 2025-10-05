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
            
            // Create OutGameState and set up its substate machine
            var outGameState = new OutGameState();
            _stateMachine.AddState(outGameState);
            
            // Get the substate machine from OutGameState and register it
            var outGameSubStateMachine = outGameState.GetSubStateMachine();
            if (outGameSubStateMachine != null)
            {
                _stateMachine.AddSubStateMachine("OutGame", outGameSubStateMachine);
            }
            
            // Create GameplayState and set up its substate machine
            var gameplayState = new GameplayState();
            _stateMachine.AddState(gameplayState);
            
            // Get the substate machine from GameplayState and register it
            var gameplaySubStateMachine = gameplayState.GetSubStateMachine();
            if (gameplaySubStateMachine != null)
            {
                _stateMachine.AddSubStateMachine("Gameplay", gameplaySubStateMachine);
            }

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
