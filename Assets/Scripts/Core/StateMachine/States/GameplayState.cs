using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;
using Resonance.Player.Actions;
using Resonance.Enemies;
using Resonance.Items;

namespace Resonance.Core.StateMachine.States
{
    public class GameplayState : IState
    {
        public string Name => "Gameplay";
        private IUIService _uiService;
        
        // Substate management
        private BaseStateMachine _subStateMachine;
        private EnemyHitbox _currentResonanceTarget;
        
        // Substates
        private ResonanceState _resonanceState;
        private InfoReadingState _infoReadingState;

        public void Enter()
        {
            Debug.Log("State: Entering Gameplay");
            
            _uiService = ServiceRegistry.Get<IUIService>();
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady += OnSceneUIPanelsReady;
                Debug.Log("GameplayState: Subscribed to OnSceneUIPanelsReady event");
            }
            
            // Initialize substate machine
            SetupSubStateMachine();
            
            // Subscribe to PlayerResonanceAction events
            PlayerResonanceAction.OnResonanceActionStarted += OnResonanceStarted;
            PlayerResonanceAction.OnResonanceActionEnded += OnResonanceEnded;
            Debug.Log("GameplayState: Subscribed to PlayerResonanceAction events");
            
            // Subscribe to InfoReadingState events
            InfoReadingState.OnInfoReadingEnded += OnInfoReadingEnded;
            Debug.Log("GameplayState: Subscribed to InfoReadingState events");
            
            // Reset UI state for new gameplay session
            Debug.Log("GameplayState: Reset _hasShownUI flag for new gameplay session");
        }

        private void OnSceneUIPanelsReady(string sceneName)
        {
            // Exclude MainMenu and other non-gameplay scenes
            bool isGameplayScene = sceneName.Contains("Level") || sceneName.Contains("Room") || sceneName.Contains("Test");
            
            if (isGameplayScene)
            {
                Debug.Log($"GameplayState: Scene {sceneName} UI panels are ready, showing gameplay UI");
                ShowGameplayUI();
            }
        }
        
        /// <summary>
        /// Show gameplay UI for the current scene
        /// This method can be called multiple times safely (e.g., on scene transitions)
        /// </summary>
        private void ShowGameplayUI()
        {
            if (_uiService != null)
            {
                _uiService.ShowPanelsForState("Gameplay");
            }
            else
            {
                Debug.LogError("GameplayState: UIService is null, cannot show gameplay UI");
            }
        }

        public void Update()
        {
            // Update substate machine
            _subStateMachine?.Update();
        }

        public void Exit()
        {
            Debug.Log("State: Exiting Gameplay");
            
            // Unsubscribe from events (Risk mitigation: Event lifecycle management)
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady -= OnSceneUIPanelsReady;
            }
            
            PlayerResonanceAction.OnResonanceActionStarted -= OnResonanceStarted;
            PlayerResonanceAction.OnResonanceActionEnded -= OnResonanceEnded;
            Debug.Log("GameplayState: Unsubscribed from PlayerResonanceAction events");
            
            // Unsubscribe from InfoReadingState events
            InfoReadingState.OnInfoReadingEnded -= OnInfoReadingEnded;
            Debug.Log("GameplayState: Unsubscribed from InfoReadingState events");
            
            // Cleanup substate machine
            _subStateMachine?.Clear();
            _subStateMachine = null;
            _currentResonanceTarget = null;
        }

        public bool CanTransitionTo(IState newState)
        {
            return newState.Name == "Paused" || newState.Name == "MainMenu";
        }
        
        /// <summary>
        /// Setup the substate machine with Normal, Resonance, and InfoReading substates
        /// </summary>
        private void SetupSubStateMachine()
        {
            _subStateMachine = new BaseStateMachine();
            
            // Add substates
            _subStateMachine.AddState(new NormalGameplayState());
            
            // Create and add ResonanceState (without target initially)
            _resonanceState = new ResonanceState(null);
            _subStateMachine.AddState(_resonanceState);
            
            // Create and add InfoReadingState
            _infoReadingState = new InfoReadingState();
            _subStateMachine.AddState(_infoReadingState);
            
            // Start with normal gameplay
            _subStateMachine.ChangeState("Normal");
            Debug.Log("GameplayState: Initialized substate machine with Normal, Resonance, and InfoReading states");
        }
        
        /// <summary>
        /// Handle resonance action started event
        /// </summary>
        /// <param name="targetCore">The target core being attacked</param>
        private void OnResonanceStarted(EnemyHitbox targetCore)
        {
            // Risk mitigation: Defensive programming
            if (targetCore == null)
            {
                Debug.LogWarning("GameplayState: OnResonanceStarted called with null target core");
                return;
            }
            
            if (_subStateMachine == null)
            {
                Debug.LogError("GameplayState: SubStateMachine is null, cannot transition to Resonance");
                return;
            }
            
            // Prevent multiple simultaneous resonance attacks
            if (_currentResonanceTarget != null)
            {
                Debug.LogWarning("GameplayState: Already in Resonance state, ignoring new resonance start");
                return;
            }
            
            Debug.Log($"GameplayState: Resonance started on target {targetCore.name}");
            
            // Store target reference
            _currentResonanceTarget = targetCore;
            
            // Update existing ResonanceState with new target
            _resonanceState.SetTargetCore(targetCore);
            
            // Transition to Resonance substate (Risk mitigation: Atomic state transition)
            if (!_subStateMachine.ChangeState("Resonance"))
            {
                Debug.LogError("GameplayState: Failed to transition to Resonance substate");
                // Cleanup on failure
                _currentResonanceTarget = null;
                return;
            }
            
            Debug.Log("GameplayState: Successfully transitioned to Resonance substate");
        }
        
        /// <summary>
        /// Handle resonance action ended event
        /// </summary>
        private void OnResonanceEnded()
        {
            Debug.Log("GameplayState: Resonance ended");
            
            // Transition back to Normal substate (Risk mitigation: Atomic state transition)
            if (_subStateMachine != null && !_subStateMachine.ChangeState("Normal"))
            {
                Debug.LogError("GameplayState: Failed to transition back to Normal substate");
                // Force state reset as fallback
                SetupSubStateMachine();
            }
            else
            {
                Debug.Log("GameplayState: Successfully transitioned back to Normal substate");
            }
            
            // Cleanup target reference
            _currentResonanceTarget = null;
        }
        
        /// <summary>
        /// Handle info reading ended event
        /// </summary>
        private void OnInfoReadingEnded()
        {
            Debug.Log("GameplayState: Info reading ended");
            
            // Transition back to Normal substate
            if (_subStateMachine != null && !_subStateMachine.ChangeState("Normal"))
            {
                Debug.LogError("GameplayState: Failed to transition back to Normal substate from InfoReading");
                // Force state reset as fallback
                SetupSubStateMachine();
            }
            else
            {
                Debug.Log("GameplayState: Successfully transitioned back to Normal substate from InfoReading");
            }
        }
        
        /// <summary>
        /// Start info reading session
        /// </summary>
        /// <param name="infoData">The info data to read</param>
        public void StartInfoReading(InfoDataAsset infoData)
        {
            if (infoData == null)
            {
                Debug.LogError("GameplayState: Cannot start info reading with null InfoDataAsset");
                return;
            }
            
            if (_subStateMachine == null)
            {
                Debug.LogError("GameplayState: SubStateMachine is null, cannot transition to InfoReading");
                return;
            }
            
            Debug.Log($"GameplayState: Starting info reading for {infoData.infoName}");
            
            // Set the info data in the InfoReadingState
            _infoReadingState.SetInfoData(infoData);
            
            // Transition to InfoReading substate
            if (!_subStateMachine.ChangeState("InfoReading"))
            {
                Debug.LogError("GameplayState: Failed to transition to InfoReading substate");
                return;
            }
            
            Debug.Log("GameplayState: Successfully transitioned to InfoReading substate");
        }
        
        /// <summary>
        /// Get current substate name for debugging
        /// </summary>
        public string GetCurrentSubstateName()
        {
            return _subStateMachine?.CurrentState?.Name ?? "None";
        }
        
        /// <summary>
        /// Force refresh of gameplay UI (useful after scene transitions)
        /// </summary>
        public void RefreshGameplayUI()
        {
            Debug.Log("GameplayState: Force refreshing gameplay UI");
            ShowGameplayUI();
        }
    }
}
