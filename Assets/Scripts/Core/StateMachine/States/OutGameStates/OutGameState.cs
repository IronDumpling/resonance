using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;
using Resonance.UI;

namespace Resonance.Core.StateMachine.States
{
    public class OutGameState : IState
    {
        public string Name => "OutGame";
        
        // Substate management
        private BaseStateMachine _subStateMachine;
        private IUIService _uiService;
        private IAudioService _audioService;
        
        // Substates
        private MainMenuState _mainMenuState;
        private LoadProgressState _loadProgressState;

        public void Enter()
        {
            Debug.Log("State: Entering OutGame");
            
            _uiService = ServiceRegistry.Get<IUIService>();
            _audioService = ServiceRegistry.Get<IAudioService>();
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady += OnSceneUIPanelsReady;
            }
            
            // Play background music
            PlayBackgroundMusic();
            
            // Initialize substate machine
            SetupSubStateMachine();
            
            // Subscribe to UI events for substate transitions
            MainMenuPanel.OnStartGameRequested += OnStartGameRequested;
            LoadProgressPanel.OnBackToMainMenuRequested += OnBackToMainMenuRequested;
            Debug.Log("OutGameState: Subscribed to MainMenuPanel and AudioService events");
        }
        
        public void Update()
        {
            // Update substate machine
            _subStateMachine?.Update();
        }
        
        public void Exit()
        {
            Debug.Log("State: Exiting OutGame");
            
            // Stop background music
            StopBackgroundMusic();
            
            // Clean up event subscriptions
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady -= OnSceneUIPanelsReady;
            }
            
            // Unsubscribe from UI events
            MainMenuPanel.OnStartGameRequested -= OnStartGameRequested;
            LoadProgressPanel.OnBackToMainMenuRequested -= OnBackToMainMenuRequested;
            Debug.Log("OutGameState: Unsubscribed from MainMenuPanel and AudioService events");
            
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
        }

        /// <summary>
        /// Play background music for OutGame state
        /// </summary>
        private void PlayBackgroundMusic()
        {
            if (_audioService == null)
            {
                Debug.LogWarning("OutGameState: AudioService is null, cannot play background music");
                return;
            }

            // Load the music clip from Resources
            AudioClip musicClip = Resources.Load<AudioClip>("Audios/BGM/lonely_moon");
            if (musicClip == null)
            {
                Debug.LogError("OutGameState: Failed to load 'lonely_moon' music clip from Resources/Audios/BGM/");
                return;
            }

            // Play the music with loop and fade in
            _audioService.PlayMusic(musicClip, loop: true, fadeTime: 1f);
            Debug.Log("OutGameState: Started playing background music 'lonely_moon'");
        }

        /// <summary>
        /// Stop background music
        /// </summary>
        private void StopBackgroundMusic()
        {
            if (_audioService == null)
            {
                Debug.LogWarning("OutGameState: AudioService is null, cannot stop background music");
                return;
            }

            // Stop the music with fade out
            _audioService.StopMusic(fadeTime: 1f);
            Debug.Log("OutGameState: Stopped background music");
        }
        
        /// <summary>
        /// Handle start game request from MainMenuPanel
        /// </summary>
        private void OnStartGameRequested()
        {
            Debug.Log("OutGameState: Start game requested, transitioning to LoadProgress substate");
            
            if (_subStateMachine == null)
            {
                Debug.LogError("OutGameState: SubStateMachine is null, cannot transition to LoadProgress");
                return;
            }
            
            // Transition to LoadProgress substate
            if (!_subStateMachine.ChangeState("LoadProgress"))
            {
                Debug.LogError("OutGameState: Failed to transition to LoadProgress substate");
                return;
            }
            
            Debug.Log("OutGameState: Successfully transitioned to LoadProgress substate");
        }

        /// <summary>
        /// Handle back button clicked from LoadProgressPanel
        /// </summary>
        private void OnBackToMainMenuRequested()
        {
            Debug.Log("OutGameState: Back button clicked, transitioning to MainMenu substate");

            if (_subStateMachine == null)
            {
                Debug.LogError("OutGameState: SubStateMachine is null, cannot transition to MainMenu");
                return;
            }
            
            if (!_subStateMachine.ChangeState("MainMenu"))
            {
                Debug.LogError("OutGameState: Failed to transition to MainMenu substate");
                return;
            }
            
            Debug.Log("OutGameState: Successfully transitioned to MainMenu substate");
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