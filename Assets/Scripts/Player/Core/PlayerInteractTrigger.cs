using UnityEngine;
using Resonance.Interfaces.Objects;
using Resonance.Interfaces.Services;
using Resonance.Utilities;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Player交互范围触发器组件
    /// 挂载到Player的InteractRange子对象上，检测范围内的可交互对象
    /// 参考EnemyDetectionTrigger的实现模式
    /// </summary>
    public class PlayerInteractTrigger : MonoBehaviour
    {
        private PlayerMonoBehaviour _playerMono;
        private IInteractionService _interactionService;
        private bool _isInitialized = false;
        private LayerMask _interactionLayerMask = 1 << 7; // Default to layer 7 (Interactable)

        // 当前范围内的可交互对象
        private IInteractable _currentInteractable = null;

        /// <summary>
        /// 初始化触发器
        /// </summary>
        /// <param name="playerMono">玩家MonoBehaviour引用</param>
        public void Initialize(PlayerMonoBehaviour playerMono)
        {
            _playerMono = playerMono;
            _isInitialized = true;

            // 获取交互服务
            _interactionService = ServiceRegistry.Get<IInteractionService>();
            if (_interactionService == null)
            {
                Debug.LogError("PlayerInteractTrigger: InteractionService not found");
                return;
            }
            
            Debug.Log($"PlayerInteractTrigger: Initialized successfully on {gameObject.name}");
        }

        void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized || _playerMono == null) return;

            // 检查层级过滤
            if ((_interactionLayerMask.value & (1 << other.gameObject.layer)) == 0)
            {
                Debug.Log($"PlayerInteractTrigger: Layer filter check failed for {other.name}");
                return;
            }

            // 忽略Player自己的colliders
            // if (other.transform.IsChildOf(_playerMono.transform) || other.transform == _playerMono.transform)
            // {
            //     return;
            // }

            // 检查是否是可交互对象
            IInteractable interactable = other.GetComponent<IInteractable>();
            
            // 如果没找到，尝试在父对象上查找（处理Gun的Visual子对象情况）
            if (interactable == null && other.transform.parent != null)
            {
                interactable = other.transform.parent.GetComponent<IInteractable>();
            }
            
            // 如果还没找到，尝试在根对象上查找
            if (interactable == null)
            {
                interactable = other.transform.root.GetComponent<IInteractable>();
            }

            if (interactable != null && interactable.CanInteract())
            {
                GameObject interactableGameObject = (interactable as MonoBehaviour)?.gameObject;
                if (interactableGameObject != null)
                {
                    HandleInteractableEnter(interactable, interactableGameObject);
                    Debug.Log($"PlayerInteractTrigger: HandleInteractableEnter called for {interactable.GetInteractableName()}");
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!_isInitialized || _playerMono == null) return;

            // 检查层级过滤
            if ((_interactionLayerMask.value & (1 << other.gameObject.layer)) == 0)
            {
                Debug.Log($"PlayerInteractTrigger: Layer filter check failed for {other.name}");
                return;
            }

            // 忽略Player自己的colliders
            // if (other.transform.IsChildOf(_playerMono.transform) || other.transform == _playerMono.transform)
            // {
            //     return;
            // }

            // 检查是否是可交互对象
            IInteractable interactable = other.GetComponent<IInteractable>();
            
            if (interactable == null && other.transform.parent != null)
            {
                interactable = other.transform.parent.GetComponent<IInteractable>();
                Debug.Log($"PlayerInteractTrigger: HandleInteractableExit called for {interactable.GetInteractableName()}");
            }
            
            if (interactable == null)
            {
                interactable = other.transform.root.GetComponent<IInteractable>();
                Debug.Log($"PlayerInteractTrigger: HandleInteractableExit called for {interactable.GetInteractableName()}");
            }

            if (interactable != null)
            {
                GameObject interactableGameObject = (interactable as MonoBehaviour)?.gameObject;
                if (interactableGameObject != null)
                {
                    HandleInteractableExit(interactable, interactableGameObject);
                    Debug.Log($"PlayerInteractTrigger: HandleInteractableExit called for {interactable.GetInteractableName()}");
                }
            }
        }

        /// <summary>
        /// 处理可交互对象进入范围
        /// </summary>
        /// <param name="interactable">可交互对象</param>
        /// <param name="gameObject">游戏对象</param>
        private void HandleInteractableEnter(IInteractable interactable, GameObject gameObject)
        {
            // 通知InteractionService有新的可交互对象进入范围
            if (_interactionService != null)
            {
                _interactionService.OnInteractableEnteredRange(gameObject, interactable);
            }

            // 如果当前没有可交互对象，设置这个为当前对象
            if (_currentInteractable == null)
            {
                _currentInteractable = interactable;
                ShowInteractionUI(interactable);
                Debug.Log($"PlayerInteractTrigger: ShowInteractionUI called for {interactable.GetInteractableName()}");
            }
        }

        /// <summary>
        /// 处理可交互对象离开范围
        /// </summary>
        /// <param name="interactable">可交互对象</param>
        /// <param name="gameObject">游戏对象</param>
        private void HandleInteractableExit(IInteractable interactable, GameObject gameObject)
        {
            // 通知InteractionService可交互对象离开范围
            if (_interactionService != null)
            {
                _interactionService.OnInteractableExitedRange(gameObject, interactable);
            }

            // 如果离开的是当前可交互对象，清除它
            if (_currentInteractable == interactable)
            {
                HideInteractionUI(interactable);
                _currentInteractable = null;

                // 尝试从InteractionService获取下一个最近的可交互对象
                var nextInteractable = _interactionService?.GetNearestInteractable();
                if (nextInteractable != null)
                {
                    _currentInteractable = nextInteractable;
                    ShowInteractionUI(nextInteractable);
                    Debug.Log($"PlayerInteractTrigger: ShowInteractionUI called for {nextInteractable.GetInteractableName()}");
                }
            }
        }

        /// <summary>
        /// 显示交互UI
        /// </summary>
        /// <param name="interactable">可交互对象</param>
        private void ShowInteractionUI(IInteractable interactable)
        {
            if (interactable == null) return;

            // 如果可交互对象有UI显示方法，调用它
            var gunMono = interactable as Resonance.Items.GunMonoBehaviour;
            if (gunMono != null)
            {
                gunMono.ShowInteractionUI();
                Debug.Log($"PlayerInteractTrigger: ShowInteractionUI called for {interactable.GetInteractableName()}");
            }


        }

        /// <summary>
        /// 隐藏交互UI
        /// </summary>
        /// <param name="interactable">可交互对象</param>
        private void HideInteractionUI(IInteractable interactable)
        {
            if (interactable == null) return;

            // 如果可交互对象有UI隐藏方法，调用它
            var gunMono = interactable as Resonance.Items.GunMonoBehaviour;
            if (gunMono != null)
            {
                gunMono.HideInteractionUI();
            }

            Debug.Log($"PlayerInteractTrigger: HideInteractionUI called for {interactable.GetInteractableName()}");
        }

        /// <summary>
        /// 获取当前可交互对象
        /// </summary>
        /// <returns>当前可交互对象，如果没有则为null</returns>
        public IInteractable GetCurrentInteractable()
        {
            return _currentInteractable;
        }

        /// <summary>
        /// 清除当前可交互对象（例如当对象被拾取后）
        /// </summary>
        public void ClearCurrentInteractable()
        {
            if (_currentInteractable != null)
            {
                HideInteractionUI(_currentInteractable);
                _currentInteractable = null;
            }

            Debug.Log($"PlayerInteractTrigger: ClearCurrentInteractable called");
        }

        /// <summary>
        /// 设置交互层级遮罩
        /// </summary>
        /// <param name="layerMask">层级遮罩</param>
        public void SetInteractionLayerMask(LayerMask layerMask)
        {
            _interactionLayerMask = layerMask;
            Debug.Log($"PlayerInteractTrigger: SetInteractionLayerMask called with {layerMask.value}");
        }
    }
}
