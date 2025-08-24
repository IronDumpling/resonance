using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

namespace Resonance.Core.StateMachine.States
{
    public class MainMenuState : IState
    {
        public string Name => "MainMenu";
        private IUIService _uiService;
        private bool _hasShownUI = false;

        public void Enter()
        {
            Debug.Log("State: Entering MainMenu");
            
            _uiService = ServiceRegistry.Get<IUIService>();
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady += OnSceneUIPanelsReady;
            }
            
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
                // Scene already loaded, wait for UI panels to be ready
                Debug.Log("MainMenuState: MainMenu scene already loaded, waiting for UI panels to be ready");
            }
        }

        private void OnMainMenuSceneLoaded(string sceneName)
        {
            if (sceneName == "MainMenu")
            {
                var loadSceneService = ServiceRegistry.Get<ILoadSceneService>();
                loadSceneService.OnSceneLoadCompleted -= OnMainMenuSceneLoaded;
                
                Debug.Log("MainMenuState: MainMenu scene loaded, waiting for UI panels to be ready");
            }
        }

        private void OnSceneUIPanelsReady(string sceneName)
        {
            // 只处理MainMenu场景的UI准备完成事件
            if (sceneName == "MainMenu" && !_hasShownUI)
            {
                Debug.Log($"MainMenuState: Scene {sceneName} UI panels are ready, showing main menu UI");
                _hasShownUI = true;
                
                // 显示主菜单UI
                _uiService?.ShowPanelsForState("MainMenu");
                
                // 取消订阅，避免重复处理
                _uiService.OnSceneUIPanelsReady -= OnSceneUIPanelsReady;
            }
        }

        public void Update()
        {
            // Handle main menu logic
        }

        public void Exit()
        {
            Debug.Log("State: Exiting MainMenu");
            
            // 清理事件订阅
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady -= OnSceneUIPanelsReady;
            }
            
            // 重置状态
            _hasShownUI = false;
        }

        public bool CanTransitionTo(IState newState)
        {
            return newState.Name == "Gameplay";
        }
    }
}
