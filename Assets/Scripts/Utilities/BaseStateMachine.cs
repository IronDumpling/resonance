using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Resonance.Core;

namespace Resonance.Utilities
{
    public class BaseStateMachine
    {
        private IState _currentState;
        private IState _previousState;
        private readonly Dictionary<string, IState> _states;
        private readonly Dictionary<string, BaseStateMachine> _subStateMachines;

        public IState CurrentState => _currentState;
        public IState PreviousState => _previousState;
        public string CurrentStatePath => GetCurrentStatePath();

        public BaseStateMachine()
        {
            _states = new Dictionary<string, IState>();
            _subStateMachines = new Dictionary<string, BaseStateMachine>();
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

        public bool ChangeState(string statePath)
        {
            // Support hierarchical state paths like "MainMenu/Settings" or "Gameplay/Combat/Attacking"
            if (statePath.Contains("/"))
            {
                return ChangeStateHierarchical(statePath);
            }

            if (!_states.TryGetValue(statePath, out IState newState))
            {
                Debug.LogError($"State {statePath} not found in state machine");
                return false;
            }

            return ChangeState(newState);
        }

        private bool ChangeStateHierarchical(string statePath)
        {
            var pathParts = statePath.Split('/');
            var rootStateName = pathParts[0];
            var subStatePath = string.Join("/", pathParts.Skip(1));

            // First change to the root state if not already there
            if (_currentState?.Name != rootStateName)
            {
                if (!ChangeState(rootStateName))
                {
                    return false;
                }
            }

            // Then change the sub-state machine
            if (_subStateMachines.TryGetValue(rootStateName, out BaseStateMachine subStateMachine))
            {
                return subStateMachine.ChangeState(subStatePath);
            }

            Debug.LogWarning($"No sub-state machine found for state {rootStateName}");
            return false;
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
            
            // Update sub-state machines
            if (_currentState != null && _subStateMachines.TryGetValue(_currentState.Name, out BaseStateMachine subStateMachine))
            {
                subStateMachine.Update();
            }
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
            // Clear all sub-state machines first
            foreach (var subStateMachine in _subStateMachines.Values)
            {
                subStateMachine.Clear();
            }
            _subStateMachines.Clear();

            _currentState?.Exit();
            _currentState = null;
            _previousState = null;
            _states.Clear();
        }

        // New methods for hierarchical state management
        public void AddSubStateMachine(string parentStateName, BaseStateMachine subStateMachine)
        {
            if (!_states.ContainsKey(parentStateName))
            {
                Debug.LogError($"Parent state {parentStateName} not found. Add the parent state first.");
                return;
            }

            _subStateMachines[parentStateName] = subStateMachine;
        }

        public BaseStateMachine GetSubStateMachine(string parentStateName)
        {
            _subStateMachines.TryGetValue(parentStateName, out BaseStateMachine subStateMachine);
            return subStateMachine;
        }

        private string GetCurrentStatePath()
        {
            if (_currentState == null) return "";

            var currentPath = _currentState.Name;
            
            if (_subStateMachines.TryGetValue(_currentState.Name, out BaseStateMachine subStateMachine))
            {
                var subPath = subStateMachine.GetCurrentStatePath();
                if (!string.IsNullOrEmpty(subPath))
                {
                    currentPath += "/" + subPath;
                }
            }

            return currentPath;
        }

        public bool HasStatePath(string statePath)
        {
            if (statePath.Contains("/"))
            {
                var pathParts = statePath.Split('/');
                var rootStateName = pathParts[0];
                var subStatePath = string.Join("/", pathParts.Skip(1));

                if (!_states.ContainsKey(rootStateName)) return false;
                if (!_subStateMachines.TryGetValue(rootStateName, out BaseStateMachine subStateMachine)) return false;

                return subStateMachine.HasStatePath(subStatePath);
            }

            return _states.ContainsKey(statePath);
        }
    }
}
