using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;
using System.Collections;

namespace Resonance.Core.StateMachine.States
{
    public class GameplayState : IState
    {
        public string Name => "Gameplay";

        public void Enter()
        {
            Debug.Log("State: Entering Gameplay");
            
            // Delay UI display to ensure CanvasUIManager has registered panels
            GameManager.Instance.StartCoroutine(ShowUIAfterDelay());
        }

        private IEnumerator ShowUIAfterDelay()
        {
            // Wait a few frames to ensure CanvasUIManager has completed UI discovery and registration
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            Debug.Log("GameplayState: Attempting to show gameplay UI after delay");
            
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
