using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

namespace Resonance.Core.StateMachine.States
{
    public class InitializingState : IState
    {
        public string Name => "Initializing";

        public void Enter()
        {
            Debug.Log("State: Entering Initializing");
            
            // No UI display needed during initialization
            // LoadingPanel will be shown in other scenes when needed
        }



        public void Update()
        {
            // Auto-transition to main menu after all services are initialized
            if (GameManager.Instance.Services != null)
            {
                // Check if all core services are running
                var inputService = ServiceRegistry.Get<IInputService>();
                var loadSceneService = ServiceRegistry.Get<ILoadSceneService>();
                var uiService = ServiceRegistry.Get<IUIService>();
                
                if (inputService?.State == SystemState.Running &&
                    loadSceneService?.State == SystemState.Running &&
                    uiService?.State == SystemState.Running)
                {
                    Debug.Log("InitializingState: All services initialized, transitioning to MainMenu");
                    GameManager.Instance.StateMachine.ChangeState("MainMenu");
                }
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
