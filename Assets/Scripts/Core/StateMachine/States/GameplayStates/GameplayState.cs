using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;
using Resonance.Player.Actions;
using Resonance.Enemies;
using Resonance.Enemies.Triggers;
using Resonance.Items;

namespace Resonance.Core.StateMachine.States
{
    public class GameplayState : IState
    {
        public string Name => "Gameplay";
        private IUIService _uiService;
        
        // Substate management
        private BaseStateMachine _subStateMachine;
        private EnemyHitbox _currentWaveTarget;
        
        // Substates
        private WaveState _resonanceState;
        private InfoReadingState _infoReadingState;
        private InventoryState _inventoryState;

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
            
            // Subscribe to PlayerWaveAttackAction events
            PlayerWaveAttackAction.OnWaveAttackActionStarted += OnWaveStarted;
            PlayerWaveAttackAction.OnWaveAttackActionEnded += OnWaveEnded;
            Debug.Log("GameplayState: Subscribed to PlayerWaveAttackAction events");
            
            // Subscribe to InfoReadingState events
            InfoReadingState.OnInfoReadingEnded += OnInfoReadingEnded;
            Debug.Log("GameplayState: Subscribed to InfoReadingState events");
            
            // Subscribe to InventoryState events
            InventoryState.OnInventoryEnded += OnInventoryEnded;
            Debug.Log("GameplayState: Subscribed to InventoryState events");
            
            // Subscribe to input events
            var inputService = ServiceRegistry.Get<IInputService>();
            if (inputService != null)
            {
                inputService.OnOpenInventory += StartInventory;
                inputService.OnCloseInventory += CloseInventory;
                Debug.Log("GameplayState: Subscribed to inventory input events");
            }
            else
            {
                Debug.LogError("GameplayState: InputService not found, cannot subscribe to inventory events");
            }
            
            // Subscribe to InteractionService events for auto-opening inventory when full
            GlobalServices.InteractionService.OnInventoryFullPickupAttempt += OnInventoryFullPickupAttempt;
            Debug.Log("GameplayState: Subscribed to InteractionService inventory-full event");
            
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

        /// <summary>
        /// Handle inventory-full pickup attempt
        /// Automatically opens inventory for player to organize items
        /// </summary>
        private void OnInventoryFullPickupAttempt()
        {
            Debug.Log("GameplayState: Inventory full during pickup attempt - auto-opening inventory");
            StartInventory();
        }
        
        public void Exit()
        {
            Debug.Log("State: Exiting Gameplay");
            
            // Unsubscribe from events (Risk mitigation: Event lifecycle management)
            if (_uiService != null)
            {
                _uiService.OnSceneUIPanelsReady -= OnSceneUIPanelsReady;
            }
            
            PlayerWaveAttackAction.OnWaveAttackActionStarted -= OnWaveStarted;
            PlayerWaveAttackAction.OnWaveAttackActionEnded -= OnWaveEnded;
            Debug.Log("GameplayState: Unsubscribed from PlayerWaveAttackAction events");
            
            // Unsubscribe from InfoReadingState events
            InfoReadingState.OnInfoReadingEnded -= OnInfoReadingEnded;
            Debug.Log("GameplayState: Unsubscribed from InfoReadingState events");
            
            // Unsubscribe from InventoryState events
            InventoryState.OnInventoryEnded -= OnInventoryEnded;
            Debug.Log("GameplayState: Unsubscribed from InventoryState events");
            
            // Unsubscribe from input events
            var inputService = ServiceRegistry.Get<IInputService>();
            if (inputService != null)
            {
                inputService.OnOpenInventory -= StartInventory;
                inputService.OnCloseInventory -= CloseInventory;
                Debug.Log("GameplayState: Unsubscribed from inventory input events");
            }
            
            // Unsubscribe from InteractionService events
            GlobalServices.InteractionService.OnInventoryFullPickupAttempt -= OnInventoryFullPickupAttempt;
            Debug.Log("GameplayState: Unsubscribed from InteractionService events");
            
            // Cleanup substate machine
            _subStateMachine?.Clear();
            _subStateMachine = null;
            _currentWaveTarget = null;
        }

        public bool CanTransitionTo(IState newState)
        {
            return newState.Name == "OutGame";
        }
        
        /// <summary>
        /// Setup the substate machine with Normal, Wave, and InfoReading substates
        /// </summary>
        private void SetupSubStateMachine()
        {
            _subStateMachine = new BaseStateMachine();
            
            // Add substates
            _subStateMachine.AddState(new NormalGameplayState());
            
            // Create and add WaveState (without target initially)
            _resonanceState = new WaveState(null);
            _subStateMachine.AddState(_resonanceState);
            
            // Create and add InfoReadingState
            _infoReadingState = new InfoReadingState();
            _subStateMachine.AddState(_infoReadingState);
            
            // Create and add InventoryState
            _inventoryState = new InventoryState();
            _subStateMachine.AddState(_inventoryState);
            
            // Start with normal gameplay
            _subStateMachine.ChangeState("Normal");
            Debug.Log("GameplayState: Initialized substate machine with Normal, Wave, InfoReading, and Inventory states");
        }
        
        /// <summary>
        /// Handle resonance action started event
        /// </summary>
        /// <param name="targetCore">The target core being attacked</param>
        private void OnWaveStarted(EnemyHitbox targetCore)
        {
            // Risk mitigation: Defensive programming
            if (targetCore == null)
            {
                Debug.LogWarning("GameplayState: OnWaveStarted called with null target core");
                return;
            }
            
            if (_subStateMachine == null)
            {
                Debug.LogError("GameplayState: SubStateMachine is null, cannot transition to Wave");
                return;
            }
            
            // Prevent multiple simultaneous resonance attacks
            if (_currentWaveTarget != null)
            {
                Debug.LogWarning("GameplayState: Already in Wave state, ignoring new resonance start");
                return;
            }
            
            Debug.Log($"GameplayState: Wave started on target {targetCore.name}");
            
            // Store target reference
            _currentWaveTarget = targetCore;
            
            // Update existing WaveState with new target
            _resonanceState.SetTargetCore(targetCore);
            
            // Transition to Wave substate (Risk mitigation: Atomic state transition)
            if (!_subStateMachine.ChangeState("Wave"))
            {
                Debug.LogError("GameplayState: Failed to transition to Wave substate");
                // Cleanup on failure
                _currentWaveTarget = null;
                return;
            }
            
            Debug.Log("GameplayState: Successfully transitioned to Wave substate");
        }
        
        /// <summary>
        /// Handle resonance action ended event
        /// </summary>
        private void OnWaveEnded()
        {
            Debug.Log("GameplayState: Wave ended");
            
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
            _currentWaveTarget = null;
        }

        /// <summary>
        /// Start info reading session
        /// </summary>
        /// <param name="infoData">The info data to read</param>
        public void OnInfoReadingStarted(InfoDataAsset infoData)
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
        /// Start inventory session (public method for multiple trigger points)
        /// </summary>
        public void StartInventory()
        {
            if (_subStateMachine == null)
            {
                Debug.LogError("GameplayState: SubStateMachine is null, cannot transition to Inventory");
                return;
            }
            
            // Prevent opening if already in inventory
            if (_subStateMachine.CurrentState?.Name == "Inventory")
            {
                Debug.Log("GameplayState: Already in Inventory substate, ignoring");
                return;
            }

            Debug.Log("GameplayState: Starting inventory");

            // Transition to Inventory substate
            if (!_subStateMachine.ChangeState("Inventory"))
            {
                Debug.LogError("GameplayState: Failed to transition to Inventory substate");
            }
            else
            {
                Debug.Log("GameplayState: Successfully transitioned to Inventory substate");
            }
        }
        
        /// <summary>
        /// Close inventory (called by input system)
        /// </summary>
        private void CloseInventory()
        {
            if (_subStateMachine == null)
            {
                Debug.LogError("GameplayState: SubStateMachine is null, cannot close inventory");
                return;
            }
            
            // Only close if currently in inventory
            if (_subStateMachine.CurrentState?.Name != "Inventory")
            {
                Debug.Log("GameplayState: Not in Inventory substate, ignoring close request");
                return;
            }

            Debug.Log("GameplayState: Closing inventory");

            // Transition back to Normal substate
            if (!_subStateMachine.ChangeState("Normal"))
            {
                Debug.LogError("GameplayState: Failed to transition back to Normal substate from Inventory");
            }
            else
            {
                Debug.Log("GameplayState: Successfully transitioned back to Normal substate from Inventory");
            }
        }

        /// <summary>
        /// Handle inventory ended event (from InventoryState.OnInventoryEnded)
        /// </summary>
        private void OnInventoryEnded()
        {
            Debug.Log("GameplayState: Inventory ended event received");
            // No action needed - state transition is already handled by CloseInventory()
            // This event is here for future extensibility (e.g., save inventory changes)
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
