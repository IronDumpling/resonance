using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Interfaces.Services;
using Resonance.Interfaces.Objects;
using Resonance.Items;
using Resonance.Utilities;

namespace Resonance.Core.GlobalServices
{
    /// <summary>
    /// 全局交互服务
    /// 管理玩家与场景中所有可交互物体的交互
    /// </summary>
    public class InteractionService : IInteractionService
    {
        private GameObject _currentInteractable;
        private string _currentInteractionText = "";
        private HashSet<GameObject> _registeredInteractables = new HashSet<GameObject>();
        
        // 新系统：跟踪范围内的可交互对象
        private Dictionary<GameObject, IInteractable> _interactablesInRange = new Dictionary<GameObject, IInteractable>();

        // IGameService Properties
        public int Priority => 30; // After PlayerService (20) since we need the player to be available
        public SystemState State { get; private set; } = SystemState.Uninitialized;

        // Properties
        public GameObject CurrentInteractable => _currentInteractable;
        public bool HasInteractable => _currentInteractable != null;
        public string InteractionText => _currentInteractionText;

        // Events
        public event System.Action<GameObject, string> OnInteractableChanged;

        #region IGameService Implementation

        public void Initialize()
        {
            if (State != SystemState.Uninitialized)
            {
                Debug.LogWarning("InteractionService already initialized");
                return;
            }

            State = SystemState.Initializing;
            Debug.Log("InteractionService: Initializing");

            State = SystemState.Running;
            Debug.Log("InteractionService: Initialized successfully");
        }

        public void Shutdown()
        {
            if (State == SystemState.Shutdown) return;

            Debug.Log("InteractionService: Shutting down");

            _registeredInteractables.Clear();
            _interactablesInRange.Clear();
            _currentInteractable = null;
            _currentInteractionText = "";
            
            OnInteractableChanged = null;

            State = SystemState.Shutdown;
        }

        #endregion

        #region IInteractionService Implementation

        public void RegisterInteractable(GameObject interactable)
        {
            if (interactable == null) return;
            
            if (_registeredInteractables.Add(interactable))
            {
                Debug.Log($"InteractionService: Registered interactable {interactable.name}");
            }
        }

        public void UnregisterInteractable(GameObject interactable)
        {
            if (interactable == null) return;
            
            if (_registeredInteractables.Remove(interactable))
            {
                // 如果当前交互对象被移除，清除它
                if (_currentInteractable == interactable)
                {
                    ClearCurrentInteractable();
                }
                
                Debug.Log($"InteractionService: Unregistered interactable {interactable.name}");
            }
        }

        public void SetCurrentInteractable(GameObject interactable, string interactionText = "")
        {
            if (_currentInteractable == interactable) return;
            
            _currentInteractable = interactable;
            _currentInteractionText = interactionText;
            
            OnInteractableChanged?.Invoke(_currentInteractable, _currentInteractionText);
            
            if (interactable != null)
            {
                Debug.Log($"InteractionService: Current interactable set to {interactable.name}: {interactionText}");
            }
        }

        public void ClearCurrentInteractable()
        {
            if (_currentInteractable == null) return;
            
            Debug.Log($"InteractionService: Cleared current interactable {_currentInteractable.name}");
            
            _currentInteractable = null;
            _currentInteractionText = "";
            
            OnInteractableChanged?.Invoke(null, "");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 获取最近的可交互对象
        /// </summary>
        /// <returns>最近的可交互对象，如果没有则为null</returns>
        public IInteractable GetNearestInteractable()
        {
            if (_interactablesInRange.Count == 0) return null;

            // 获取玩家位置
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null) return null;

            Vector3 playerPosition = playerService.CurrentPlayer.transform.position;
            
            // 找到最近的可交互对象
            IInteractable nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var kvp in _interactablesInRange)
            {
                if (kvp.Value != null && kvp.Value.CanInteract())
                {
                    float distance = Vector3.Distance(playerPosition, kvp.Value.GetPosition());
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = kvp.Value;
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// 处理可交互对象进入范围
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <param name="interactable">可交互对象</param>
        public void OnInteractableEnteredRange(GameObject gameObject, IInteractable interactable)
        {
            if (gameObject == null || interactable == null) return;

            // 添加到范围内对象列表
            _interactablesInRange[gameObject] = interactable;
        }

        /// <summary>
        /// 处理可交互对象离开范围
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <param name="interactable">可交互对象</param>
        public void OnInteractableExitedRange(GameObject gameObject, IInteractable interactable)
        {
            if (gameObject == null) return;

            // 从范围内对象列表移除
            _interactablesInRange.Remove(gameObject);
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// 获取已注册的交互对象数量（用于调试）
        /// </summary>
        /// <returns>交互对象数量</returns>
        public int GetRegisteredCount()
        {
            return _registeredInteractables.Count;
        }

        /// <summary>
        /// 获取所有已注册的交互对象（用于调试）
        /// </summary>
        /// <returns>交互对象列表</returns>
        public GameObject[] GetRegisteredInteractables()
        {
            GameObject[] result = new GameObject[_registeredInteractables.Count];
            _registeredInteractables.CopyTo(result);
            return result;
        }

        #endregion
    }
}
