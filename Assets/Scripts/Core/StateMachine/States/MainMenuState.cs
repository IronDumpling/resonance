using UnityEngine;
using Resonance.Core;

namespace Resonance.Core.StateMachine.States
{
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
}
