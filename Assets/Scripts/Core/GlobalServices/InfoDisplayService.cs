using UnityEngine;
using Resonance.Core;
using Resonance.Core.StateMachine;
using Resonance.Core.StateMachine.States;
using Resonance.Interfaces.Objects;
using Resonance.Interfaces.Services;
using Resonance.Items;
using Resonance.Items.Core;

namespace Resonance.Core.GlobalServices
{
    /// <summary>
    /// Info display service
    /// 统一管理所有IInfoable对象的信息显示逻辑
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
        /// <param name="infoData">要显示的信息数据</param>
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
