using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;

namespace Resonance.Core.StateMachine.States
{
    public class GameplayState : IState
    {
        public string Name => "Gameplay";

        public void Enter()
        {
            Debug.Log("State: Entering Gameplay");
            
            // Show gameplay UI
            var uiService = ServiceRegistry.Get<IUIService>();
            uiService?.ShowPanelsForState("Gameplay");
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
}
