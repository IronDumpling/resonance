using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

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
                // Subscribe to scene load completion to show UI
                loadSceneService.OnSceneLoadCompleted += OnMainMenuSceneLoaded;
                loadSceneService.LoadSceneAsync("MainMenu");
            }
            else
            {
                // Scene already loaded, show UI after a short delay
                GameManager.Instance.StartCoroutine(ShowUIWhenReady());
            }
        }

        private void OnMainMenuSceneLoaded(string sceneName)
        {
            if (sceneName == "MainMenu")
            {
                var loadSceneService = ServiceRegistry.Get<ILoadSceneService>();
                loadSceneService.OnSceneLoadCompleted -= OnMainMenuSceneLoaded;
                
                // Wait for CanvasUIManager to register panels
                GameManager.Instance.StartCoroutine(ShowUIWhenReady());
            }
        }

        private System.Collections.IEnumerator ShowUIWhenReady()
        {
            // Wait for CanvasUIManager to register panels
            yield return new UnityEngine.WaitForSeconds(0.1f);
            
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
