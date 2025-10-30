using UnityEngine;
using Resonance.Interfaces.Objects;
using Resonance.Interfaces.Services;
using Resonance.Items;
using Resonance.Core;
using Resonance.Utilities.Types;

namespace Resonance.Player.Triggers
{
    /// <summary>
    /// Player Interact Trigger - triggered by E key for interactable objects in range
    /// Attached to the InteractRange child object of Player, detects interactable objects in range
    /// Conditions: PlayerNormalState, valid interactable object in range
    /// Behavior: Player cannot move, performs interaction with target object
    /// End condition: Interaction completes or is cancelled
    /// </summary>
    public class PlayerInteractTrigger : MonoBehaviour
    {
        private PlayerMonoBehaviour _playerMono;
        private IInteractionService _interactionService;
        private bool _isInitialized = false;
        private LayerMask _interactionLayerMask = LayerDict.GetLayer("Interactable");

        // Current interactable object in range
        private IInteractable _currentInteractable = null;

        /// <summary>
        /// Initialize the trigger
        /// </summary>
        /// <param name="playerMono">Player MonoBehaviour reference</param>
        public void Initialize(PlayerMonoBehaviour playerMono)
        {
            _playerMono = playerMono;
            _isInitialized = true;

            // Get the interaction service
            _interactionService = ServiceRegistry.Get<IInteractionService>();
            if (_interactionService == null)
            {
                Debug.LogError("PlayerInteractTrigger: InteractionService not found");
                return;
            }

            // 监听InteractionService的事件, 保持同步
            _interactionService.OnInteractableChanged += OnInteractionServiceChanged;
            
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

            // 检查是否是可交互对象
            IInteractable interactable = other.GetComponent<IInteractable>();
            
            // 如果没找到, 尝试在父对象上查找(处理Weapon的Visual子对象情况)
            if (interactable == null && other.transform.parent != null)
            {
                interactable = other.transform.parent.GetComponent<IInteractable>();
            }
            
            // 如果还没找到, 尝试在根对象上查找
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

            // 如果当前没有可交互对象, 设置这个为当前对象
            if (_currentInteractable == null)
            {
                _currentInteractable = interactable;
                ShowInteractionUI(interactable);
                
                // 同时通知InteractionService设置当前可交互对象
                GameObject interactableGameObject = (interactable as MonoBehaviour)?.gameObject;
                if (interactableGameObject != null && _interactionService != null)
                {
                    _interactionService.SetCurrentInteractable(interactableGameObject, $"Press E to interact with {interactable.GetInteractableName()}");
                }
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

            // 如果离开的是当前可交互对象, 清除它
            if (_currentInteractable == interactable)
            {
                HideInteractionUI(interactable);
                _currentInteractable = null;

                // 清理InteractionService的当前对象
                GameObject interactableGameObject = (interactable as MonoBehaviour)?.gameObject;
                if (interactableGameObject != null && _interactionService != null && _interactionService.CurrentInteractable == interactableGameObject)
                {
                    _interactionService.ClearCurrentInteractable();
                }

                // 尝试从InteractionService获取下一个最近的可交互对象
                var nextInteractable = _interactionService?.GetNearestInteractable();
                if (nextInteractable != null)
                {
                    _currentInteractable = nextInteractable;
                    ShowInteractionUI(nextInteractable);
                    
                    // 同时设置InteractionService的当前对象
                    GameObject nextGameObject = (nextInteractable as MonoBehaviour)?.gameObject;
                    if (nextGameObject != null)
                    {
                        _interactionService.SetCurrentInteractable(nextGameObject, $"Press E to interact with {nextInteractable.GetInteractableName()}");
                    }
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

            // 如果可交互对象有UI显示方法, 调用它
            var gunMono = interactable as WeaponMonoBehaviour;
            if (gunMono != null)
            {
                gunMono.ShowInteractionUI();
            }

            var ammoMono = interactable as AmmoMonoBehaviour;
            if (ammoMono != null)
            {
                ammoMono.ShowInteractionUI();
            }

            var infoMono = interactable as InfoMonoBehaviour;
            if (infoMono != null)
            {
                infoMono.ShowInteractionUI();
            }
        }

        /// <summary>
        /// 隐藏交互UI
        /// </summary>
        /// <param name="interactable">可交互对象</param>
        private void HideInteractionUI(IInteractable interactable)
        {
            if (interactable == null) return;

            // 如果可交互对象有UI隐藏方法, 调用它
            var gunMono = interactable as WeaponMonoBehaviour;
            if (gunMono != null)
            {
                gunMono.HideInteractionUI();
            }

            var ammoMono = interactable as AmmoMonoBehaviour;
            if (ammoMono != null)
            {
                ammoMono.HideInteractionUI();
            }
            
            var infoMono = interactable as InfoMonoBehaviour;
            if (infoMono != null)
            {
                infoMono.HideInteractionUI();
            }
        }

        /// <summary>
        /// 获取当前可交互对象
        /// </summary>
        /// <returns>当前可交互对象, 如果没有则为null</returns>
        public IInteractable GetCurrentInteractable()
        {
            return _currentInteractable;
        }

        /// <summary>
        /// 清除当前可交互对象(例如当对象被拾取后)
        /// </summary>
        public void ClearCurrentInteractable()
        {
            if (_currentInteractable != null)
            {
                HideInteractionUI(_currentInteractable);
                _currentInteractable = null;
            }

        }

        /// <summary>
        /// 设置交互层级遮罩
        /// </summary>
        /// <param name="layerMask">层级遮罩</param>
        public void SetInteractionLayerMask(LayerMask layerMask)
        {
            _interactionLayerMask = layerMask;
        }

        /// <summary>
        /// 处理InteractionService的当前可交互对象变化事件
        /// 保持PlayerInteractTrigger与InteractionService的状态同步
        /// </summary>
        /// <param name="interactableGameObject">新的可交互游戏对象</param>
        /// <param name="interactionText">交互文本</param>
        private void OnInteractionServiceChanged(GameObject interactableGameObject, string interactionText)
        {
            // 如果InteractionService清除了当前对象, 我们也清除
            if (interactableGameObject == null)
            {
                if (_currentInteractable != null)
                {
                    HideInteractionUI(_currentInteractable);
                    _currentInteractable = null;

                    // 尝试从范围内的对象中找到下一个可交互对象
                    var nextInteractable = _interactionService?.GetNearestInteractable();
                    if (nextInteractable != null)
                    {
                        _currentInteractable = nextInteractable;
                        ShowInteractionUI(nextInteractable);
                    }
                }
            }
        }

        /// <summary>
        /// 清理触发器, 取消事件订阅
        /// </summary>
        public void Cleanup()
        {
            if (_interactionService != null)
            {
                _interactionService.OnInteractableChanged -= OnInteractionServiceChanged;
            }

            if (_currentInteractable != null)
            {
                HideInteractionUI(_currentInteractable);
                _currentInteractable = null;
            }

            _isInitialized = false;
        }

        /// <summary>
        /// Unity OnDestroy - 确保清理资源
        /// </summary>
        void OnDestroy()
        {
            Cleanup();
        }
    }
}
