using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Utilities.Types;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;
using Resonance.Enemies.Triggers;
using Resonance.UI;

namespace Resonance.Core.StateMachine.States
{
    /// <summary>
    /// Wave substate - handles QTE mechanics during wave attacks
    /// Active when player is performing wave action on enemy cores
    /// </summary>
    public class WaveState : IState
    {
        public string Name => "Wave";
        
        private IUIService _uiService;
        private IInputService _inputService;
        private IWavable _sourceWavable;  // The attacker (player or enemy)
        private IWavable _targetWavable;  // The target being attacked
        private bool _isInitialized = false;
        
        // Safety timeout mechanism (Risk mitigation: Prevent stuck states)
        private float _stateEnterTime = 0f;
        private const float MAX_RESONANCE_DURATION = 30f; // 30 seconds timeout

        public static event System.Action OnWaveStateEntered;
        public static event System.Action OnWaveStateExited;

        /// <summary>
        /// Constructor for WaveState
        /// </summary>
        /// <param name="sourceWavable">The source of the wave attack</param>
        /// <param name="targetWavable">The target being attacked</param>
        public WaveState(IWavable sourceWavable = null, IWavable targetWavable = null)
        {
            _sourceWavable = sourceWavable;
            _targetWavable = targetWavable;
        }
        
        /// <summary>
        /// Set the wave attack context before entering the state
        /// This must be called before Enter() if constructor was called with null parameters
        /// </summary>
        public void SetWaveAttackContext(IWavable sourceWavable, IWavable targetWavable)
        {
            _sourceWavable = sourceWavable;
            _targetWavable = targetWavable;
            
            Debug.Log($"WaveState: Set wave attack context - Source: {(sourceWavable != null ? "valid" : "null")}, " +
                     $"Target: {(targetWavable != null ? "valid" : "null")}");
        }

        public void Enter()
        {
            Debug.Log("WaveState: Entering Wave substate");
            
            // Get services
            _uiService = ServiceRegistry.Get<IUIService>();
            _inputService = ServiceRegistry.Get<IInputService>();
            
            if (_uiService != null)
            {
                // Show WavePanel for this substate
                _uiService.ShowPanelsForState("Gameplay/Wave");
                Debug.Log("WaveState: Showed WavePanel");
                
                // Pass wave attack context to WavePanel
                var wavePanel = _uiService.GetPanel<WavePanel>("WavePanel");
                if (wavePanel != null)
                {
                    wavePanel.SetWaveAttackContext(_sourceWavable, _targetWavable);
                    Debug.Log($"WaveState: Initialized WavePanel with source and target wavables");
                }
            }
            
            // Switch input to Wave map (disable player actions, enable QTE)
            if (_inputService != null)
            {
                _inputService.DisablePlayerInput();
                _inputService.EnableWaveInput();
                Debug.Log("WaveState: Switched to Wave input mode");
            }
            
            // Record enter time for timeout mechanism
            _stateEnterTime = Time.time;
            
            _isInitialized = true;
            
            OnWaveStateEntered?.Invoke();
        }

        public void Update()
        {
            if (!_isInitialized) return;
            
            // Safety timeout check (Risk mitigation: Prevent stuck states)
            if (Time.time - _stateEnterTime > MAX_RESONANCE_DURATION)
            {
                Debug.LogWarning("WaveState: Timeout reached, forcing exit from Wave state");
                // This will be handled by the parent GameplayState through normal exit mechanisms
                return;
            }
            
            // Monitor target core state for safety (defensive programming)
            if (_targetWavable == null)
            {
                Debug.LogWarning("WaveState: Target core is null");
                return;
            }
            
            // Additional safety checks could be added here
            // For example, verify the target is still valid
        }

        public void Exit()
        {
            Debug.Log("WaveState: Exiting Wave substate");
            
            // Restore player input
            if (_inputService != null)
            {
                _inputService.DisableWaveInput();
                _inputService.EnablePlayerInput();
                Debug.Log("WaveState: Restored player input mode");
            }
            
            // Hide WavePanel and show normal Gameplay panels
            if (_uiService != null)
            {
                _uiService.ShowPanelsForState("Gameplay");
                Debug.Log("WaveState: Restored normal Gameplay panels");
            }
            
            // Clear references
            _targetWavable = null;
            _isInitialized = false;

            OnWaveStateExited?.Invoke();
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
