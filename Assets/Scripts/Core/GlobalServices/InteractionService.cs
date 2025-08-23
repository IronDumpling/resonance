using UnityEngine;
using System.Collections.Generic;
using Resonance.Interfaces.Services;
using Resonance.Items;

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

        // IGameService Properties
        public int Priority => 30; // After PlayerService (20) since we need the player to be available
        public SystemState State { get; private set; } = SystemState.Uninitialized;

        // Properties
        public GameObject CurrentInteractable => _currentInteractable;
        public bool HasInteractable => _currentInteractable != null;
        public string InteractionText => _currentInteractionText;

        // Events
        public event System.Action<GameObject, string> OnInteractableChanged;
        public event System.Action<GameObject, Transform> OnInteractionPerformed;

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
            _currentInteractable = null;
            _currentInteractionText = "";
            
            OnInteractableChanged = null;
            OnInteractionPerformed = null;

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

        public bool PerformInteraction(Transform playerTransform)
        {
            if (!HasInteractable || playerTransform == null)
            {
                Debug.LogWarning("InteractionService: No interactable or player transform is null");
                return false;
            }

            GameObject interactable = _currentInteractable;
            bool interactionSuccess = false;

            // 尝试与Gun交互
            GunMonoBehaviour gun = interactable.GetComponent<GunMonoBehaviour>();
            if (gun != null && gun.CanInteract())
            {
                interactionSuccess = HandleGunInteraction(gun, playerTransform);
            }
            
            // 这里可以添加其他类型的交互逻辑
            // 例如：门、开关、NPC等

            if (interactionSuccess)
            {
                OnInteractionPerformed?.Invoke(interactable, playerTransform);
                Debug.Log($"InteractionService: Successfully interacted with {interactable.name}");
            }
            else
            {
                Debug.LogWarning($"InteractionService: Failed to interact with {interactable.name}");
            }

            return interactionSuccess;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 处理Gun交互
        /// </summary>
        /// <param name="gun">Gun组件</param>
        /// <param name="playerTransform">玩家Transform</param>
        /// <returns>是否成功交互</returns>
        private bool HandleGunInteraction(GunMonoBehaviour gun, Transform playerTransform)
        {
            // 获取玩家控制器
            var playerMono = playerTransform.GetComponent<Resonance.Player.PlayerMonoBehaviour>();
            if (playerMono == null || !playerMono.IsInitialized)
            {
                Debug.LogError("InteractionService: Player not found or not initialized");
                return false;
            }

            var playerController = playerMono.Controller;
            if (playerController?.WeaponManager == null)
            {
                Debug.LogError("InteractionService: Player weapon manager not found");
                return false;
            }

            // 检查玩家是否已有武器
            if (playerController.HasWeapon)
            {
                Debug.LogWarning("InteractionService: Player already has a weapon");
                // 这里可以实现武器替换逻辑
                return false;
            }

            // 拾取武器
            var gunData = gun.PickupWeapon(playerTransform);
            if (gunData != null)
            {
                // 装备武器到玩家
                playerController.WeaponManager.EquipWeapon(gunData);
                
                // 清除当前交互对象
                ClearCurrentInteractable();
                
                Debug.Log($"InteractionService: Player picked up {gunData.weaponName}");
                return true;
            }

            return false;
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
