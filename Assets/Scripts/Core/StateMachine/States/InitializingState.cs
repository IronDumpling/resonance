using UnityEngine;
using Resonance.Core;

namespace Resonance.Core.StateMachine.States
{
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
}
