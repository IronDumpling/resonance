using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

namespace Resonance.Core.StateMachine.States
{
    public class GameplayState : IState
    {
        public string Name => "Gameplay";
        private IUIService _uiService;
        private bool _hasShownUI = false;

        public void Enter()
        {
            Debug.Log("State: Entering Gameplay");
            
            _uiService = ServiceRegistry.Get<IUIService>();
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady += OnSceneUIPanelsReady;
                Debug.Log("GameplayState: Subscribed to OnSceneUIPanelsReady event");
            }
        }

        private void OnSceneUIPanelsReady(string sceneName)
        {
            if (sceneName == "Level_01" && !_hasShownUI)
            {
                Debug.Log($"GameplayState: Scene {sceneName} UI panels are ready, showing gameplay UI");
                _hasShownUI = true;
                _uiService?.ShowPanelsForState("Gameplay");
                _uiService.OnSceneUIPanelsReady -= OnSceneUIPanelsReady;
            }
        }

        public void Update()
        {
            // Handle gameplay logic
        }

        public void Exit()
        {
            Debug.Log("State: Exiting Gameplay");
            
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady -= OnSceneUIPanelsReady;
            }
            
            _hasShownUI = false;
        }

        public bool CanTransitionTo(IState newState)
        {
            return newState.Name == "Paused" || newState.Name == "MainMenu";
        }
    }
}
