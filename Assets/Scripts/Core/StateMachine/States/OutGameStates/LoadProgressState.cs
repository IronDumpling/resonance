using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

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
            
            // Show LoadProgress panel immediately if UI is already ready
            ShowLoadProgressUI();
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
            // Within OutGameState substate machine, allow transitions to all other substates
            // This includes: MainMenu, LoadProgress, or any future OutGame substates
            return true;
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
        /// Show the LoadProgress panel
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