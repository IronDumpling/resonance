using UnityEngine;
using Resonance.Core;

namespace Resonance.Core.StateMachine.States
{
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
