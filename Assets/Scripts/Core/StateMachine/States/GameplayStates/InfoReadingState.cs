using UnityEngine;
using Resonance.Core;
using Resonance.Gameplay.Items;
using Resonance.Utilities;
using Resonance.Shared.Interfaces.Services;

namespace Resonance.Core.StateMachine.States
{
    /// <summary>
    /// InfoReadingState - Gameplay's sub-state
    /// When the player interacts with InfoMonoBehaviour, enter this state
    /// In this state, gameplay is paused but UI remains interactive
    /// </summary>
    public class InfoReadingState : IState
    {
        public string Name => "InfoReading";

        private IUIService _uiService;
        private ISelectivePauseService _pauseService;
        private IInputService _inputService;
        private InfoDataAsset _currentInfoData;

        // Events
        public static event System.Action<InfoDataAsset> OnInfoReadingStarted;
        public static event System.Action OnInfoReadingEnded;

        public void Enter()
        {
            Debug.Log("State: Entering InfoReading");

            // Get services
            _uiService = ServiceRegistry.Get<IUIService>();
            _pauseService = ServiceRegistry.Get<ISelectivePauseService>();
            _inputService = ServiceRegistry.Get<IInputService>();

            Debug.Log($"InfoReadingState: UIService = {(_uiService != null ? "Found" : "NULL")}");
            Debug.Log($"InfoReadingState: SelectivePauseService = {(_pauseService != null ? "Found" : "NULL")}");
            Debug.Log($"InfoReadingState: InputService = {(_inputService != null ? "Found" : "NULL")}");

            // Switch input to Information map (disable player actions, enable info reading controls)
            if (_inputService != null)
            {
                _inputService.DisablePlayerInput();
                _inputService.EnableInformationInput();
                Debug.Log("InfoReadingState: Switched to Information input mode");
            }

            // Pause gameplay
            if (_pauseService != null)
            {
                Debug.Log("InfoReadingState: Calling PauseGameplay()");
                _pauseService.PauseGameplay();
            }
            else
            {
                Debug.LogError("InfoReadingState: SelectivePauseService is null, cannot pause gameplay");
            }

            // Show InfoPanel
            _uiService?.ShowPanelsForState("Gameplay/InfoReading");

            // Trigger events
            OnInfoReadingStarted?.Invoke(_currentInfoData);

            Debug.Log($"InfoReadingState: Started reading info - {_currentInfoData?.infoName ?? "Unknown"}");
        }

        public void Update()
        {
            // Do nothing
        }

        public void Exit()
        {
            Debug.Log("State: Exiting InfoReading");

            // Restore to normal Gameplay UI state
            _uiService?.ShowPanelsForState("Gameplay");

            // Resume gameplay
            if (_pauseService != null)
            {
                Debug.Log("InfoReadingState: Calling ResumeGameplay()");
                _pauseService.ResumeGameplay();
            }
            else
            {
                Debug.LogError("InfoReadingState: SelectivePauseService is null, cannot resume gameplay");
            }

            // Restore player input
            if (_inputService != null)
            {
                _inputService.DisableInformationInput();
                _inputService.EnablePlayerInput();
                Debug.Log("InfoReadingState: Restored player input mode");
            }

            // Clean up state
            _currentInfoData = null;

            Debug.Log("InfoReadingState: Info reading session ended");
        }

        public bool CanTransitionTo(IState newState)
        {
            if (newState.Name == "OutGame" || newState.Name == "Gameplay" || newState.Name == "Initializing")
            {
                return false; // Cannot transition to parent-level states
            }
            return true; // Allow all same-level substates
        }

        /// <summary>
        /// Set the info data to read
        /// </summary>
        /// <param name="infoData">Info data asset</param>
        public void SetInfoData(InfoDataAsset infoData)
        {
            _currentInfoData = infoData;
            Debug.Log($"InfoReadingState: Set info data - {infoData?.infoName ?? "null"}");
        }

        /// <summary>
        /// Get the current info data being read
        /// </summary>
        /// <returns>The current info data</returns>
        public InfoDataAsset GetCurrentInfoData()
        {
            return _currentInfoData;
        }

        /// <summary>
        /// Static method: Trigger info reading ended event (for external use)
        /// </summary>
        public static void TriggerInfoReadingEnd()
        {
            Debug.Log("InfoReadingState: External trigger for info reading end");
            OnInfoReadingEnded?.Invoke();
        }
    }
}
