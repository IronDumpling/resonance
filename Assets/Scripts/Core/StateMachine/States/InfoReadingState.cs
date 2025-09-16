using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;
using Resonance.Items;

namespace Resonance.Core.StateMachine.States
{
    /// <summary>
    /// 信息阅读状态 - Gameplay的子状态
    /// 当玩家与InfoMonoBehaviour交互时进入此状态
    /// 在此状态下，游戏逻辑暂停但UI保持可交互
    /// </summary>
    public class InfoReadingState : IState
    {
        public string Name => "ReadingInfo";

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

            // 暂停游戏逻辑但保持UI交互
            _pauseService?.PauseGameplay();

            // 显示InfoPanel
            _uiService?.ShowPanelsForState("Gameplay/ReadingInfo");

            // 触发事件
            OnInfoReadingStarted?.Invoke(_currentInfoData);

            Debug.Log($"InfoReadingState: Started reading info - {_currentInfoData?.infoName ?? "Unknown"}");
        }

        public void Update()
        {
            // 处理输入 - ESC键或其他关闭操作
            // 这里可以监听输入服务的ESC键事件
            var inputService = ServiceRegistry.Get<IInputService>();
            if (inputService != null)
            {
                // 如果有ESC键输入，关闭InfoPanel
                // 注意：具体的输入检测逻辑需要根据你们的InputService实现来调整
                // 这里只是示例框架
            }
        }

        public void Exit()
        {
            Debug.Log("State: Exiting InfoReading");

            // 隐藏InfoPanel
            _uiService?.HidePanel("InfoPanel");

            // 恢复游戏逻辑
            _pauseService?.ResumeGameplay();

            // 触发事件
            TriggerInfoReadingEnd();

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
        /// 关闭信息阅读（由UI调用）
        /// </summary>
        public void CloseInfoReading()
        {
            Debug.Log("InfoReadingState: Close info reading requested");
            
            // 触发结束事件，让GameplayState处理状态转换
            TriggerInfoReadingEnd();
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
