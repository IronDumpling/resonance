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
        /// 执行交互
        /// </summary>
        /// <param name="playerTransform">玩家Transform</param>
        /// <returns>是否成功交互</returns>
        bool PerformInteraction(Transform playerTransform);
        
        // Events
        event System.Action<GameObject, string> OnInteractableChanged; // 可交互对象, 提示文本
        event System.Action<GameObject, Transform> OnInteractionPerformed; // 交互对象, 玩家
    }
}
