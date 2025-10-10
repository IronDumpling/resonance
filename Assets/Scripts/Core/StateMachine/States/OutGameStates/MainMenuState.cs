using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;
using Resonance.UI;

namespace Resonance.Core.StateMachine.States
{
    public class MainMenuState : IState
    {
        public string Name => "MainMenu";
        private IUIService _uiService;
        private bool _hasShownUI = false;

        public void Enter()
        {
            Debug.Log("State: Entering MainMenu substate");
            
            _uiService = ServiceRegistry.Get<IUIService>();
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady += OnSceneUIPanelsReady;
            }
            
            // Load MainMenu scene
            var loadSceneService = ServiceRegistry.Get<ISceneTransitionService>();
            if (loadSceneService != null && loadSceneService.CurrentSceneName != "MainMenu")
            {
                // Subscribe to scene load completion to show UI
                loadSceneService.OnSceneLoadCompleted += OnMainMenuSceneLoaded;
                loadSceneService.LoadSceneAsync("MainMenu");
            }
            else
            {
                // Scene already loaded, try to show UI directly as backup mechanism
                Debug.Log("MainMenuState: MainMenu scene already loaded, attempting to show UI directly");
                TryShowMainMenuUI();
            }
        }

        private void OnMainMenuSceneLoaded(string sceneName)
        {
            if (sceneName == "MainMenu")
            {
                var loadSceneService = ServiceRegistry.Get<ISceneTransitionService>();
                loadSceneService.OnSceneLoadCompleted -= OnMainMenuSceneLoaded;
                
                Debug.Log("MainMenuState: MainMenu scene loaded, waiting for UI panels to be ready");
            }
        }

        private void OnSceneUIPanelsReady(string sceneName)
        {
            // Only handle MainMenu scene UI ready event
            if (sceneName == "MainMenu" && !_hasShownUI)
            {
                Debug.Log($"MainMenuState: Scene {sceneName} UI panels are ready, showing main menu UI");
                
                // Use unified method to show UI
                ShowMainMenuUI();
                
                // Unsubscribe to avoid duplicate processing
                _uiService.OnSceneUIPanelsReady -= OnSceneUIPanelsReady;
            }
        }

        /// <summary>
        /// Try to show main menu UI directly as backup mechanism
        /// This is called when scene is already loaded and we need to show UI immediately
        /// </summary>
        private void TryShowMainMenuUI()
        {
            if (_uiService == null)
            {
                Debug.LogWarning("MainMenuState: UIService is null, cannot show main menu UI");
                return;
            }

            // Check if it's safe to show UI directly
            if (IsMainMenuUISafeToShow())
            {
                Debug.Log("MainMenuState: UI is safe to show, displaying main menu UI directly");
                ShowMainMenuUI();
            }
            else
            {
                Debug.Log("MainMenuState: UI not ready yet, waiting for OnSceneUIPanelsReady event");
            }
        }

        /// <summary>
        /// Check if main menu UI is safe to show directly
        /// </summary>
        private bool IsMainMenuUISafeToShow()
        {
            if (_uiService == null) return false;

            // Check if MainMenuPanel is registered and not already visible
            var mainMenuPanel = _uiService.GetPanel<MainMenuPanel>("MainMenuPanel");
            bool isRegistered = mainMenuPanel != null;
            bool isNotVisible = !_uiService.IsPanelVisible("MainMenuPanel");
            
            Debug.Log($"MainMenuState: UI Safety Check - Registered: {isRegistered}, Not Visible: {isNotVisible}");
            
            return isRegistered && isNotVisible;
        }

        /// <summary>
        /// Show main menu UI (unified method for both event and direct display)
        /// </summary>
        private void ShowMainMenuUI()
        {
            if (_uiService != null)
            {
                _uiService.ShowPanelsForState("OutGame/MainMenu");
                _hasShownUI = true;
                Debug.Log("MainMenuState: Main menu UI displayed successfully");
            }
        }

        public void Update()
        {
            // Handle main menu logic
        }

        public void Exit()
        {
            Debug.Log("State: Exiting MainMenu substate");
            
            // Clean up event subscriptions
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady -= OnSceneUIPanelsReady;
            }
            
            // Reset state
            _hasShownUI = false;
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
