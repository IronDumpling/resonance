using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;
using Resonance.UI;

namespace Resonance.Core.StateMachine.States
{
    public class LoadProgressState : IState
    {
        public string Name => "LoadProgress";
        private IUIService _uiService;
        private bool _hasShownUI = false;

        public void Enter()
        {
            Debug.Log("State: Entering LoadProgress substate");
            
            _uiService = ServiceRegistry.Get<IUIService>();
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady += OnSceneUIPanelsReady;
            }
            
            // Try to show LoadProgress panel immediately as backup mechanism
            TryShowLoadProgressUI();
        }
        
        public void Update()
        {
            // Handle load progress logic
        }
        
        public void Exit()
        {
            Debug.Log("State: Exiting LoadProgress substate");
            
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
        
        /// <summary>
        /// Handle scene UI panels ready event
        /// </summary>
        private void OnSceneUIPanelsReady(string sceneName)
        {
            if (!_hasShownUI)
            {
                Debug.Log($"LoadProgressState: Scene {sceneName} UI panels are ready, showing load progress UI");
                ShowLoadProgressUI();
            }
        }
        
        /// <summary>
        /// Try to show LoadProgress UI directly as backup mechanism
        /// This is called when we need to show UI immediately
        /// </summary>
        private void TryShowLoadProgressUI()
        {
            if (_uiService == null)
            {
                Debug.LogWarning("LoadProgressState: UIService is null, cannot show load progress UI");
                return;
            }

            // Check if it's safe to show UI directly
            if (IsLoadProgressUISafeToShow())
            {
                Debug.Log("LoadProgressState: UI is safe to show, displaying load progress UI directly");
                ShowLoadProgressUI();
            }
            else
            {
                Debug.Log("LoadProgressState: UI not ready yet, waiting for OnSceneUIPanelsReady event");
            }
        }

        /// <summary>
        /// Check if LoadProgress UI is safe to show directly
        /// </summary>
        private bool IsLoadProgressUISafeToShow()
        {
            if (_uiService == null) return false;

            // Check if LoadProgressPanel is registered and not already visible
            var loadProgressPanel = _uiService.GetPanel<LoadProgressPanel>("LoadProgressPanel");
            bool isRegistered = loadProgressPanel != null;
            bool isNotVisible = !_uiService.IsPanelVisible("LoadProgressPanel");
            
            Debug.Log($"LoadProgressState: UI Safety Check - Registered: {isRegistered}, Not Visible: {isNotVisible}");
            
            return isRegistered && isNotVisible;
        }

        /// <summary>
        /// Show the LoadProgress panel (unified method for both event and direct display)
        /// </summary>
        private void ShowLoadProgressUI()
        {
            if (_uiService != null && !_hasShownUI)
            {
                _hasShownUI = true;
                _uiService.ShowPanelsForState("OutGame/LoadProgress");
                Debug.Log("LoadProgressState: LoadProgress panel shown");
            }
        }
    }
}