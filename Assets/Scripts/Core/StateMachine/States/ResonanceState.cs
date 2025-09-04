using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;
using Resonance.Enemies;
using Resonance.UI;

namespace Resonance.Core.StateMachine.States
{
    /// <summary>
    /// Resonance substate - handles QTE mechanics during resonance attacks
    /// Active when player is performing resonance action on enemy cores
    /// </summary>
    public class ResonanceState : IState
    {
        public string Name => "Resonance";
        
        private IUIService _uiService;
        private IInputService _inputService;
        private EnemyHitbox _targetCore;
        private bool _isInitialized = false;
        
        // Safety timeout mechanism (Risk mitigation: Prevent stuck states)
        private float _stateEnterTime = 0f;
        private const float MAX_RESONANCE_DURATION = 30f; // 30 seconds timeout

        public ResonanceState(EnemyHitbox targetCore)
        {
            _targetCore = targetCore;
        }

        public void Enter()
        {
            Debug.Log("ResonanceState: Entering Resonance substate");
            
            // Get services
            _uiService = ServiceRegistry.Get<IUIService>();
            _inputService = ServiceRegistry.Get<IInputService>();
            
            if (_uiService != null)
            {
                // Show ResonancePanel for this substate
                _uiService.ShowPanelsForState("Gameplay/Resonance");
                Debug.Log("ResonanceState: Showed ResonancePanel");
                
                // Pass target core information to ResonancePanel
                var resonancePanel = _uiService.GetPanel<ResonancePanel>("ResonancePanel");
                if (resonancePanel != null)
                {
                    resonancePanel.SetTargetCore(_targetCore);
                    Debug.Log($"ResonanceState: Initialized ResonancePanel with target {_targetCore?.name}");
                }
            }
            
            // Switch input mode to Resonance (disable Recover/Resonance, enable QTE)
            if (_inputService != null)
            {
                _inputService.IsResonanceMode = true;
                Debug.Log("ResonanceState: Switched to Resonance input mode");
            }
            
            // Record enter time for timeout mechanism
            _stateEnterTime = Time.time;
            
            _isInitialized = true;
        }

        public void Update()
        {
            if (!_isInitialized) return;
            
            // Safety timeout check (Risk mitigation: Prevent stuck states)
            if (Time.time - _stateEnterTime > MAX_RESONANCE_DURATION)
            {
                Debug.LogWarning("ResonanceState: Timeout reached, forcing exit from Resonance state");
                // This will be handled by the parent GameplayState through normal exit mechanisms
                return;
            }
            
            // Monitor target core state for safety (defensive programming)
            if (_targetCore == null || !_targetCore.IsInitialized)
            {
                Debug.LogWarning("ResonanceState: Target core is null or not initialized");
                return;
            }
            
            // Additional safety checks could be added here
            // For example, verify the target is still valid
        }

        public void Exit()
        {
            Debug.Log("ResonanceState: Exiting Resonance substate");
            
            // Restore normal input mode
            if (_inputService != null)
            {
                _inputService.IsResonanceMode = false;
                Debug.Log("ResonanceState: Restored normal input mode");
            }
            
            // Hide ResonancePanel and show normal Gameplay panels
            if (_uiService != null)
            {
                _uiService.ShowPanelsForState("Gameplay");
                Debug.Log("ResonanceState: Restored normal Gameplay panels");
            }
            
            // Clear references
            _targetCore = null;
            _isInitialized = false;
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can only transition back to normal gameplay state
            return newState.Name == "Normal";
        }

        /// <summary>
        /// Get the target core hitbox for this resonance state
        /// </summary>
        public EnemyHitbox GetTargetCore()
        {
            return _targetCore;
        }
    }
}
