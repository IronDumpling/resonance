using UnityEngine;

namespace Resonance.Interfaces.Services
{
    /// <summary>
    /// 交互服务接口
    /// 管理玩家与场景中可交互物体的交互
    /// </summary>
    public interface IInteractionService : IGameService
    {
        /// <summary>
        /// 当前可交互的对象
        /// </summary>
        GameObject CurrentInteractable { get; }
        
        /// <summary>
        /// 是否有可交互的对象
        /// </summary>
        bool HasInteractable { get; }
        
        /// <summary>
        /// 当前交互提示文本
        /// </summary>
        string InteractionText { get; }
        
        /// <summary>
        /// 注册可交互对象
        /// </summary>
        /// <param name="interactable">可交互对象</param>
        void RegisterInteractable(GameObject interactable);
        
        /// <summary>
        /// 移除可交互对象
        /// </summary>
        /// <param name="interactable">可交互对象</param>
        void UnregisterInteractable(GameObject interactable);
        
        /// <summary>
        /// 设置当前可交互对象
        /// </summary>
        /// <param name="interactable">可交互对象</param>
        /// <param name="interactionText">交互提示文本</param>
        void SetCurrentInteractable(GameObject interactable, string interactionText = "");
        
        /// <summary>
        /// 清除当前可交互对象
        /// </summary>
        void ClearCurrentInteractable();

        /// <summary>
        /// 获取最近的可交互对象
        /// </summary>
        /// <returns>最近的可交互对象，如果没有则为null</returns>
        Interfaces.Objects.IInteractable GetNearestInteractable();

        /// <summary>
        /// 处理可交互对象进入范围
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <param name="interactable">可交互对象</param>
        void OnInteractableEnteredRange(GameObject gameObject, Interfaces.Objects.IInteractable interactable);

        /// <summary>
        /// 处理可交互对象离开范围
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <param name="interactable">可交互对象</param>
        void OnInteractableExitedRange(GameObject gameObject, Interfaces.Objects.IInteractable interactable);
        
        // Events
        event System.Action<GameObject, string> OnInteractableChanged; // 可交互对象, 提示文本
        event System.Action<GameObject, Transform> OnInteractionPerformed; // 交互对象, 玩家
    }
}
