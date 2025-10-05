using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

namespace Resonance.Core.StateMachine.States
{
    public class OutGameState : IState
    {
        public string Name => "OutGame";
        
        // Substate management
        private BaseStateMachine _subStateMachine;
        private IUIService _uiService;
        
        // Substates
        private MainMenuState _mainMenuState;
        private LoadProgressState _loadProgressState;

        public void Enter()
        {
            Debug.Log("State: Entering OutGame");
            
            _uiService = ServiceRegistry.Get<IUIService>();
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady += OnSceneUIPanelsReady;
            }
            
            // Initialize substate machine
            SetupSubStateMachine();
        }
        
        public void Update()
        {
            // Update substate machine
            _subStateMachine?.Update();
        }
        
        public void Exit()
        {
            Debug.Log("State: Exiting OutGame");
            
            // Clean up event subscriptions
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady -= OnSceneUIPanelsReady;
            }
            
            // Clear substate machine
            _subStateMachine?.Clear();
        }
        
        public bool CanTransitionTo(IState newState)
        {
            return newState.Name == "Gameplay";
        }
        
        /// <summary>
        /// Setup the substate machine with MainMenu and LoadProgress substates
        /// </summary>
        private void SetupSubStateMachine()
        {
            _subStateMachine = new BaseStateMachine();
            
            // Create and add substates
            _mainMenuState = new MainMenuState();
            _loadProgressState = new LoadProgressState();
            
            _subStateMachine.AddState(_mainMenuState);
            _subStateMachine.AddState(_loadProgressState);
            
            // Start with MainMenu substate
            _subStateMachine.ChangeState("MainMenu");
            Debug.Log("OutGameState: Initialized substate machine with MainMenu and LoadProgress states");
        }
        
        /// <summary>
        /// Handle scene UI panels ready event
        /// </summary>
        private void OnSceneUIPanelsReady(string sceneName)
        {
            Debug.Log($"OutGameState: Scene {sceneName} UI panels are ready");
            // Forward to current substate if needed
        }
        
        /// <summary>
        /// Change to MainMenu substate
        /// </summary>
        public void ChangeToMainMenu()
        {
            if (_subStateMachine != null)
            {
                _subStateMachine.ChangeState("MainMenu");
            }
        }
        
        /// <summary>
        /// Change to LoadProgress substate
        /// </summary>
        public void ChangeToLoadProgress()
        {
            if (_subStateMachine != null)
            {
                _subStateMachine.ChangeState("LoadProgress");
            }
        }
        
        /// <summary>
        /// Get current substate name for debugging
        /// </summary>
        public string GetCurrentSubstateName()
        {
            return _subStateMachine?.CurrentState?.Name ?? "None";
        }
    }
}