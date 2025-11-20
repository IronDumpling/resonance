using UnityEngine;
using Resonance.Core;
using Resonance.Core.StateMachine;
using Resonance.Core.StateMachine.States;
using Resonance.Shared.Interfaces.Objects;
using Resonance.Shared.Interfaces.Services;
using Resonance.Gameplay.Items;
using Resonance.Gameplay.Items.Core;

namespace Resonance.Core.GlobalServices
{
    /// <summary>
    /// Info display service
    /// Unified management of all IInfoable object information display logic
    /// </summary>
    public static class InfoDisplayService
    {
        /// <summary>
        /// Show IInfoable object information
        /// </summary>
        /// <param name="infoable">Object that can display information</param>
        public static void ShowInfo(IInfoable infoable)
        {
            if (infoable == null)
            {
                Debug.LogError("InfoDisplayService: Cannot show info for null IInfoable object");
                return;
            }

            if (!infoable.HasValidInfo())
            {
                Debug.LogWarning("InfoDisplayService: IInfoable object has no valid info to display");
                return;
            }

            var infoData = infoable.GetInfoData();
            ShowInfoData(infoData);
        }

        /// <summary>
        /// Show InfoData structure information
        /// </summary>
        /// <param name="infoData">Information data to display</param>
        public static void ShowInfoData(InfoData infoData)
        {
            if (!infoData.IsValid())
            {
                Debug.LogError("InfoDisplayService: Cannot show invalid InfoData");
                return;
            }

            // Create a temporary InfoDataAsset to compatible with existing system
            var tempInfoAsset = CreateTempInfoDataAsset(infoData);
            
            // Start info reading session
            StartInfoReadingSession(tempInfoAsset);
        }

        /// <summary>
        /// Create a temporary InfoDataAsset from InfoData
        /// </summary>
        private static InfoDataAsset CreateTempInfoDataAsset(InfoData infoData)
        {
            var tempInfo = ScriptableObject.CreateInstance<InfoDataAsset>();
            
            // Use reflection to set private field, or we need to add a constructor in InfoDataAsset
            var field = typeof(InfoDataAsset).GetField("_infoData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(tempInfo, infoData);
            
            return tempInfo;
        }

        /// <summary>
        /// Start info reading session
        /// </summary>
        private static void StartInfoReadingSession(InfoDataAsset infoAsset)
        {
            if (infoAsset == null)
            {
                Debug.LogError("InfoDisplayService: Cannot start info reading session with null InfoDataAsset");
                return;
            }

            // Get GameManager and state machine
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("InfoDisplayService: GameManager instance not found");
                return;
            }

            var stateMachine = gameManager.GetComponent<GameStateMachine>();
            if (stateMachine == null)
            {
                Debug.LogError("InfoDisplayService: GameStateMachine not found on GameManager");
                return;
            }

            // Get current GameplayState
            var gameplayState = stateMachine.GetState<GameplayState>("Gameplay");
            if (gameplayState == null)
            {
                Debug.LogError("InfoDisplayService: GameplayState not found in state machine");
                return;
            }

            Debug.Log($"InfoDisplayService: Starting info reading session for {infoAsset.infoName}");

            // Start info reading session
            gameplayState.OnInfoReadingStarted(infoAsset);
        }
    }
}
