using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Shared.Interfaces.Services;

namespace Resonance.Core.StateMachine.States
{
    /// <summary>
    /// Normal gameplay substate - default substate of GameplayState
    /// Handles regular gameplay when not in special modes like Wave
    /// </summary>
    public class NormalGameplayState : IState
    {
        public string Name => "Normal";

        private IInputService _inputService;

        public void Enter()
        {
            Debug.Log("NormalGameplayState: Entering normal gameplay substate");
            
            _inputService = ServiceRegistry.Get<IInputService>();
            
            if (_inputService != null)
            {
                _inputService.EnablePlayerInput();
                Debug.Log("NormalGameplayState: Enabled player input");
            }
        }

        public void Update()
        {
            
        }

        public void Exit()
        {
            Debug.Log("NormalGameplayState: Exiting normal gameplay substate");
            
            // Normal gameplay cleanup
            if (_inputService != null)
            {
                _inputService.DisablePlayerInput();
                Debug.Log("NormalGameplayState: Disabled player input");
            }
        }

        public bool CanTransitionTo(IState newState)
        {
            if (newState.Name == "OutGame" || newState.Name == "Gameplay" || newState.Name == "Initializing")
            {
                return false; // Cannot transition to parent-level states
            }
            return true; // Allow all same-level substates
        }
    }
}
