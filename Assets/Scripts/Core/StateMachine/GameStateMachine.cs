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
            
            // Create OutGameState
            _stateMachine.AddState(new OutGameState());
            
            // Create GameplayState 
            _stateMachine.AddState(new GameplayState());

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

        /// <summary>
        /// Change to a top-level game state (Initializing, OutGame, Gameplay)
        /// This method only handles transitions between first-level states
        /// </summary>
        /// <param name="stateName">Name of the top-level state</param>
        /// <returns>True if state change was successful</returns>
        public bool ChangeState(string stateName)
        {
            if (!_isInitialized)
            {
                Debug.LogError("GameStateMachine: Cannot change state before initialization");
                return false;
            }

            // Only allow top-level state names
            if (stateName != "Initializing" && stateName != "OutGame" && stateName != "Gameplay")
            {
                Debug.LogError($"GameStateMachine: '{stateName}' is not a valid top-level state. Use ChangeSubState for substates.");
                return false;
            }

            return _stateMachine?.ChangeState(stateName) ?? false;
        }

        /// <summary>
        /// Get the current full state path (e.g., "OutGame/MainMenu" or "Gameplay/Normal")
        /// </summary>
        public string GetCurrentStatePath()
        {
            return _stateMachine?.CurrentStatePath ?? "";
        }

        /// <summary>
        /// Get a specific top-level state by name (for advanced usage)
        /// </summary>
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
