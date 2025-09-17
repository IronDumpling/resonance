using UnityEngine;
using Resonance.Core;
using Resonance.Items;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

namespace Resonance.Core.StateMachine.States
{
    /// <summary>
    /// 信息阅读状态 - Gameplay的子状态
    /// 当玩家与InfoMonoBehaviour交互时进入此状态
    /// 在此状态下，游戏逻辑暂停但UI保持可交互
    /// </summary>
    public class InfoReadingState : IState
    {
        public string Name => "InfoReading";

        private IUIService _uiService;
        private ISelectivePauseService _pauseService;
        private InfoDataAsset _currentInfoData;

        // Events
        public static event System.Action<InfoDataAsset> OnInfoReadingStarted;
        public static event System.Action OnInfoReadingEnded;

        public void Enter()
        {
            Debug.Log("State: Entering InfoReading");

            // 获取服务
            _uiService = ServiceRegistry.Get<IUIService>();
            _pauseService = ServiceRegistry.Get<ISelectivePauseService>();

            Debug.Log($"InfoReadingState: UIService = {(_uiService != null ? "Found" : "NULL")}");
            Debug.Log($"InfoReadingState: SelectivePauseService = {(_pauseService != null ? "Found" : "NULL")}");

            // 暂停游戏逻辑但保持UI交互
            if (_pauseService != null)
            {
                Debug.Log("InfoReadingState: Calling PauseGameplay()");
                _pauseService.PauseGameplay();
            }
            else
            {
                Debug.LogError("InfoReadingState: SelectivePauseService is null, cannot pause gameplay");
            }

            // 显示InfoPanel
            _uiService?.ShowPanelsForState("Gameplay/InfoReading");

            // 触发事件
            OnInfoReadingStarted?.Invoke(_currentInfoData);

            Debug.Log($"InfoReadingState: Started reading info - {_currentInfoData?.infoName ?? "Unknown"}");
        }

        public void Update()
        {
            
        }

        public void Exit()
        {
            Debug.Log("State: Exiting InfoReading");

            // 恢复到正常的 Gameplay UI 状态
            _uiService?.ShowPanelsForState("Gameplay");

            // 恢复游戏逻辑
            if (_pauseService != null)
            {
                Debug.Log("InfoReadingState: Calling ResumeGameplay()");
                _pauseService.ResumeGameplay();
            }
            else
            {
                Debug.LogError("InfoReadingState: SelectivePauseService is null, cannot resume gameplay");
            }

            // 清理状态
            _currentInfoData = null;

            Debug.Log("InfoReadingState: Info reading session ended");
        }

        public bool CanTransitionTo(IState newState)
        {
            // 只能转换回Normal状态或者到Paused状态
            return newState.Name == "Normal" || newState.Name == "Paused";
        }

        /// <summary>
        /// 设置要阅读的信息数据
        /// </summary>
        /// <param name="infoData">信息数据资产</param>
        public void SetInfoData(InfoDataAsset infoData)
        {
            _currentInfoData = infoData;
            Debug.Log($"InfoReadingState: Set info data - {infoData?.infoName ?? "null"}");
        }

        /// <summary>
        /// 获取当前正在阅读的信息数据
        /// </summary>
        /// <returns>当前的信息数据</returns>
        public InfoDataAsset GetCurrentInfoData()
        {
            return _currentInfoData;
        }

        /// <summary>
        /// 静态方法：触发信息阅读结束事件（供外部调用）
        /// </summary>
        public static void TriggerInfoReadingEnd()
        {
            Debug.Log("InfoReadingState: External trigger for info reading end");
            OnInfoReadingEnded?.Invoke();
        }
    }
}
