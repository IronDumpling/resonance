using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;

namespace Resonance.Core.StateMachine.States
{
    public class InitializingState : IState
    {
        public string Name => "Initializing";

        public void Enter()
        {
            Debug.Log("State: Entering Initializing");
            
            // Delay UI display to allow CanvasUIManager to register panels first
            GameManager.Instance.StartCoroutine(ShowUIWhenReady());
        }

        private System.Collections.IEnumerator ShowUIWhenReady()
        {
            // Wait a frame for CanvasUIManager to register panels
            yield return null;
            
            // Show initializing UI
            var uiService = ServiceRegistry.Get<IUIService>();
            uiService?.ShowPanelsForState("Initializing");
        }

        public void Update()
        {
            // Auto-transition to main menu after initialization
            if (GameManager.Instance.Services != null)
            {
                // Check if all services are running
                var inputService = ServiceRegistry.Get<IInputService>();
                var loadSceneService = ServiceRegistry.Get<ILoadSceneService>();
                var uiService = ServiceRegistry.Get<IUIService>();
                
                if (inputService?.State == SystemState.Running &&
                    loadSceneService?.State == SystemState.Running &&
                    uiService?.State == SystemState.Running)
                {
                    Debug.Log("State: All services initialized, transitioning to MainMenu");
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
