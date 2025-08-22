using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;

namespace Resonance.Core.StateMachine.States
{
    public class MainMenuState : IState
    {
        public string Name => "MainMenu";

        public void Enter()
        {
            Debug.Log("State: Entering MainMenu");
            
            // Load MainMenu scene
            var loadSceneService = ServiceRegistry.Get<ILoadSceneService>();
            if (loadSceneService != null && loadSceneService.CurrentSceneName != "MainMenu")
            {
                loadSceneService.LoadSceneAsync("MainMenu");
            }
            
            // Show main menu UI
            var uiService = ServiceRegistry.Get<IUIService>();
            uiService?.ShowPanelsForState("MainMenu");
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
