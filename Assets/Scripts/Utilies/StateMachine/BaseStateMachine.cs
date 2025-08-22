using System.Collections.Generic;
using UnityEngine;
using Resonance.Core;

namespace Resonance.Utilities
{
    public class BaseStateMachine
    {
        private IState _currentState;
        private IState _previousState;
        private readonly Dictionary<string, IState> _states;

        public IState CurrentState => _currentState;
        public IState PreviousState => _previousState;

        public BaseStateMachine()
        {
            _states = new Dictionary<string, IState>();
        }

        public void AddState(IState state)
        {
            if (state == null)
            {
                Debug.LogError("Cannot add null state to state machine");
                return;
            }

            if (_states.ContainsKey(state.Name))
            {
                Debug.LogWarning($"State {state.Name} already exists in state machine. Overwriting.");
            }

            _states[state.Name] = state;
        }

        public void RemoveState(string stateName)
        {
            if (_states.ContainsKey(stateName))
            {
                if (_currentState?.Name == stateName)
                {
                    Debug.LogWarning($"Cannot remove current state {stateName}");
                    return;
                }
                _states.Remove(stateName);
            }
        }

        public bool ChangeState(string stateName)
        {
            if (!_states.TryGetValue(stateName, out IState newState))
            {
                Debug.LogError($"State {stateName} not found in state machine");
                return false;
            }

            return ChangeState(newState);
        }

        public bool ChangeState(IState newState)
        {
            if (newState == null)
            {
                Debug.LogError("Cannot change to null state");
                return false;
            }

            if (_currentState == newState)
            {
                return true;
            }

            if (_currentState != null && !_currentState.CanTransitionTo(newState))
            {
                Debug.LogWarning($"Cannot transition from {_currentState.Name} to {newState.Name}");
                return false;
            }

            _previousState = _currentState;
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();

            return true;
        }

        public void Update()
        {
            _currentState?.Update();
        }

        public T GetState<T>(string stateName) where T : class, IState
        {
            if (_states.TryGetValue(stateName, out IState state))
            {
                return state as T;
            }
            return null;
        }

        public bool HasState(string stateName)
        {
            return _states.ContainsKey(stateName);
        }

        public void Clear()
        {
            _currentState?.Exit();
            _currentState = null;
            _previousState = null;
            _states.Clear();
        }
    }
}
