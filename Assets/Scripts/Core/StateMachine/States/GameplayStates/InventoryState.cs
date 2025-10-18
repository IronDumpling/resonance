using UnityEngine;
using Resonance.Core;
using Resonance.Items;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

namespace Resonance.Core.StateMachine.States
{
    /// <summary>
    /// InventoryState - Gameplay's sub-state
    /// When the player opens inventory, enter this state
    /// In this state, gameplay is paused but UI remains interactive
    /// </summary>
    public class InventoryState : IState
    {
        public string Name => "Inventory";

        private IUIService _uiService;
        private ISelectivePauseService _pauseService;
        private IInputService _inputService;

        // Events
        public static event System.Action OnInventoryStarted;
        public static event System.Action OnInventoryEnded;

        public void Enter()
        {
            Debug.Log("State: Entering Inventory");

            // Get services
            _uiService = ServiceRegistry.Get<IUIService>();
            _pauseService = ServiceRegistry.Get<ISelectivePauseService>();
            _inputService = ServiceRegistry.Get<IInputService>();

            Debug.Log($"InventoryState: UIService = {(_uiService != null ? "Found" : "NULL")}");
            Debug.Log($"InventoryState: SelectivePauseService = {(_pauseService != null ? "Found" : "NULL")}");
            Debug.Log($"InventoryState: InputService = {(_inputService != null ? "Found" : "NULL")}");

            // Pause gameplay
            if (_pauseService != null)
            {
                Debug.Log("InventoryState: Calling PauseGameplay()");
                _pauseService.PauseGameplay();
            }
            else
            {
                Debug.LogError("InventoryState: SelectivePauseService is null, cannot pause gameplay");
            }

            // Switch input to Inventory map (disable player actions, enable inventory controls)
            if (_inputService != null)
            {
                _inputService.DisablePlayerInput();
                _inputService.EnableInventoryInput();
                Debug.Log("InventoryState: Switched to Inventory input mode");
            }

            // Show InventoryPanel
            _uiService?.ShowPanelsForState("Gameplay/Inventory");

            // Trigger events
            OnInventoryStarted?.Invoke();

            Debug.Log("InventoryState: Inventory opened successfully");
        }

        public void Update()
        {
            // Do nothing - inventory is handled by UI and input events
        }

        public void Exit()
        {
            Debug.Log("State: Exiting Inventory");

            // Restore to normal Gameplay UI state
            _uiService?.ShowPanelsForState("Gameplay");

            // Resume gameplay
            if (_pauseService != null)
            {
                Debug.Log("InventoryState: Calling ResumeGameplay()");
                _pauseService.ResumeGameplay();
            }
            else
            {
                Debug.LogError("InventoryState: SelectivePauseService is null, cannot resume gameplay");
            }

            // Restore player input
            if (_inputService != null)
            {
                _inputService.DisableInventoryInput();
                _inputService.EnablePlayerInput();
                Debug.Log("InventoryState: Restored player input mode");
            }

            // Trigger event
            OnInventoryEnded?.Invoke();

            Debug.Log("InventoryState: Inventory closed successfully");
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