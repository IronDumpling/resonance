using UnityEngine;
using TMPro;
using Resonance.Interfaces.Services;
using Resonance.Interfaces.Objects;
using Resonance.Utilities;

namespace Resonance.Items
{
    /// <summary>
    /// 场景中可交互的Gun物体
    /// 玩家可以拾取并装备到武器管理器中
    /// 
    /// Visual System Responsibilities:
    /// - _pickupVisual: The visual representation for the gun in the world (pickup state)
    /// - This handles pickup animations (bob, rotation) and interaction triggers
    /// - When equipped, the gun data is passed to WeaponManager but no visual weapon is shown on player yet
    /// - Future: Add equipped weapon visual system to player for when gun is equipped
    /// </summary>
    public class GunMonoBehaviour : MonoBehaviour, IInteractable
    {
        [Header("Gun Configuration")]
        [SerializeField] private GunDataAsset _gunDataAsset;
        
        [Header("Interaction")]
        [SerializeField] private string _interactionText = "Press E";
        [SerializeField] private float _interactionDuration = 1.0f; // Duration to pick up the weapon
        
        [Header("Pickup Visual")]
        [SerializeField] private GameObject _pickupVisual;
        [SerializeField] private bool _rotateWhenIdle = true;
        [SerializeField] private float _rotationSpeed = 30f;
        [SerializeField] private bool _bobUpAndDown = true;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobHeight = 0.2f;
        
        [Header("Interaction UI")]
        [SerializeField] private GameObject _interactUI;
        [SerializeField] private TextMeshProUGUI _interactTextComponent;
        
        // 是否已被拾取
        private bool _isPickedUp = false;
        
        // 交互状态
        private bool _isInteracting = false;
        
        // 动画相关
        private Vector3 _originalPosition;
        private float _bobTimer = 0f;
        
        // 交互检测 (由InteractionService管理)
        
        // Services
        private IInteractionService _interactionService;
        private IAudioService _audioService;

        // Events
        public System.Action<GunMonoBehaviour, Transform> OnPickedUp;

        // Properties
        public GunDataAsset GunData => _gunDataAsset;
        public bool IsPickedUp => _isPickedUp;
        public string InteractionText => _interactionText;

        void Start()
        {
            // 验证Gun数据资产
            if (_gunDataAsset == null)
            {
                Debug.LogError($"GunMonoBehaviour: No GunDataAsset assigned to {gameObject.name}!");
                return;
            }

            Debug.Log($"GunMonoBehaviour: Using gun data asset: {_gunDataAsset.weaponName}");

            // 记录原始位置用于动画
            _originalPosition = transform.position;
            
            // 如果没有指定拾取视觉模型，使用自身
            if (_pickupVisual == null)
            {
                _pickupVisual = gameObject;
            }

            // 设置音频服务
            SetupAudioService();
            
            // 设置交互UI
            SetupInteractionUI();
            
            // 获取交互服务并注册为可交互对象
            _interactionService = ServiceRegistry.Get<IInteractionService>();
            if (_interactionService != null)
            {
                _interactionService.RegisterInteractable(gameObject);
                Debug.Log($"GunMonoBehaviour: {_gunDataAsset.weaponName} registered with InteractionService");
            }
            else
            {
                Debug.LogWarning("GunMonoBehaviour: InteractionService not found");
            }

            Debug.Log($"GunMonoBehaviour: {_gunDataAsset.weaponName} ready for pickup via new InteractAction system");
        }

        void OnDestroy()
        {
            // 清理交互服务注册
            if (_interactionService != null)
            {
                _interactionService.UnregisterInteractable(gameObject);
                if (_interactionService.CurrentInteractable == gameObject)
                {
                    _interactionService.ClearCurrentInteractable();
                }
            }
        }

        void Update()
        {
            if (_isPickedUp) return;
            
            // 执行视觉动画
            PerformVisualAnimations();
        }

        /// <summary>
        /// 设置音频服务引用
        /// </summary>
        private void SetupAudioService()
        {
            _audioService = ServiceRegistry.Get<IAudioService>();
            if (_audioService == null)
            {
                Debug.LogWarning("GunMonoBehaviour: AudioService not found. Audio effects will be disabled.");
            }
            else
            {
                Debug.Log("GunMonoBehaviour: AudioService connected successfully");
            }
        }

        /// <summary>
        /// 设置交互UI - 查找并设置InteractUI和Text组件
        /// </summary>
        private void SetupInteractionUI()
        {
            // 查找InteractUI子对象（如果没有手动分配）
            if (_interactUI == null)
            {
                Transform interactUIChild = transform.Find("InteractUI");
                if (interactUIChild != null)
                {
                    _interactUI = interactUIChild.gameObject;
                    Debug.Log($"GunMonoBehaviour: Found InteractUI child object: {interactUIChild.name}");
                }
            }
            
            if (_interactUI == null)
            {
                Debug.LogWarning($"GunMonoBehaviour: No InteractUI found on {gameObject.name}. UI interaction will be disabled.");
                return;
            }
            
            // 查找Text组件
            if (_interactTextComponent == null)
            {
                _interactTextComponent = _interactUI.GetComponentInChildren<TextMeshProUGUI>();
                
                // 如果没找到，尝试查找名为"Text"的子对象
                if (_interactTextComponent == null)
                {
                    Transform textChild = _interactUI.transform.Find("Text");
                    if (textChild != null)
                    {
                        _interactTextComponent = textChild.GetComponent<TextMeshProUGUI>();
                    }
                }
            }
            
            if (_interactTextComponent == null)
            {
                Debug.LogWarning($"GunMonoBehaviour: No TextMeshProUGUI component found in InteractUI on {gameObject.name}");
            }
            else
            {
                Debug.Log($"GunMonoBehaviour: Found TextMeshProUGUI component for interaction UI");
                // 设置初始文本内容
                _interactTextComponent.text = _interactionText;
            }
            
            // 初始状态：隐藏UI
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }
        }

        /// <summary>
        /// 执行视觉动画
        /// </summary>
        private void PerformVisualAnimations()
        {
            if (_pickupVisual == null) return;

            Vector3 currentPosition = _originalPosition;
            Vector3 currentRotation = _pickupVisual.transform.eulerAngles;

            // 上下浮动动画
            if (_bobUpAndDown)
            {
                _bobTimer += Time.deltaTime * _bobSpeed;
                float bobOffset = Mathf.Sin(_bobTimer) * _bobHeight;
                currentPosition.y = _originalPosition.y + bobOffset;
            }

            // 旋转动画
            if (_rotateWhenIdle)
            {
                currentRotation.y += _rotationSpeed * Time.deltaTime;
            }

            // 应用变换
            transform.position = currentPosition;
            _pickupVisual.transform.eulerAngles = currentRotation;
        }

        #region IInteractable Implementation

        /// <summary>
        /// Check if this gun can currently be interacted with
        /// </summary>
        /// <returns>True if interaction is possible</returns>
        public bool CanInteract()
        {
            return !_isPickedUp && !_isInteracting;
        }

        /// <summary>
        /// Get the interaction duration for picking up this weapon
        /// </summary>
        /// <returns>Duration of the interaction in seconds</returns>
        public float GetInteractionDuration()
        {
            return _interactionDuration;
        }

        /// <summary>
        /// Get the world position of this gun
        /// </summary>
        /// <returns>World position</returns>
        public Vector3 GetPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// Start the interaction process
        /// </summary>
        public void StartInteraction()
        {
            if (!CanInteract())
            {
                Debug.LogWarning($"GunMonoBehaviour: Cannot start interaction with {_gunDataAsset.weaponName}");
                return;
            }

            _isInteracting = true;

            Debug.Log($"GunMonoBehaviour: Started interaction with {_gunDataAsset.weaponName}");

            // Show interaction UI
            if (_interactUI != null)
            {
                _interactUI.SetActive(true);
            }

            // TODO: Play interaction start effects (visual/audio feedback)
        }

        /// <summary>
        /// Complete the interaction successfully - pickup the weapon
        /// </summary>
        public void CompleteInteraction()
        {
            if (!_isInteracting)
            {
                Debug.LogWarning($"GunMonoBehaviour: CompleteInteraction called but not interacting with {_gunDataAsset.weaponName}");
                return;
            }

            Debug.Log($"GunMonoBehaviour: Completing interaction with {_gunDataAsset.weaponName}");

            // Get current player from PlayerService
            var playerService = ServiceRegistry.Get<IPlayerService>();
            Transform playerTransform = null;
            
            if (playerService?.CurrentPlayer != null)
            {
                playerTransform = playerService.CurrentPlayer.transform;
            }

            // Perform the actual pickup
            var gunCopy = PickupWeapon(playerTransform);
            
            if (gunCopy != null && playerTransform != null)
            {
                // Try to equip the weapon to the player
                var playerMono = playerTransform.GetComponent<Resonance.Player.PlayerMonoBehaviour>();
                if (playerMono != null && playerMono.IsInitialized)
                {
                    var weaponManager = playerMono.Controller.WeaponManager;
                    if (weaponManager != null)
                    {
                        bool hasEquippedWeapon = weaponManager.HasEquippedWeapon;
                        if (hasEquippedWeapon)
                        {
                            Debug.Log($"GunMonoBehaviour: Successfully equipped {gunCopy.weaponName} to player");
                        }
                        else
                        {
                            Debug.LogWarning($"GunMonoBehaviour: Failed to equip {gunCopy.weaponName} to player");
                        }
                    }
                    else
                    {
                        Debug.LogError("GunMonoBehaviour: Player's WeaponManager is null");
                    }
                }
                else
                {
                    Debug.LogError("GunMonoBehaviour: Player not found or not initialized");
                }
            }

            _isInteracting = false;

            // Hide interaction UI
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }
        }

        /// <summary>
        /// Cancel the interaction
        /// </summary>
        public void CancelInteraction()
        {
            if (!_isInteracting) return;

            Debug.Log($"GunMonoBehaviour: Cancelled interaction with {_gunDataAsset.weaponName}");

            _isInteracting = false;

            // Hide interaction UI
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }

            // TODO: Stop interaction effects
        }

        /// <summary>
        /// Get a descriptive name for this interactable
        /// </summary>
        /// <returns>Weapon name</returns>
        public string GetInteractableName()
        {
            return _gunDataAsset?.weaponName ?? "Unknown Weapon";
        }

        #endregion

        #region Legacy Interaction Methods

        /// <summary>
        /// 拾取武器
        /// </summary>
        /// <param name="player">拾取的玩家Transform</param>
        /// <returns>武器数据的副本</returns>
        public GunDataAsset PickupWeapon(Transform player = null)
        {
            if (_isPickedUp)
            {
                Debug.LogWarning("GunMonoBehaviour: Weapon already picked up");
                return null;
            }

            _isPickedUp = true;
            _isInteracting = false;

            PlayPickuoAudio(transform.position);
            
            // 创建运行时副本
            GunDataAsset gunCopy = _gunDataAsset.CreateRuntimeCopy();
            
            // 触发拾取事件
            OnPickedUp?.Invoke(this, player);
            
            // 停止所有动画
            StopAllCoroutines();
            
            // 隐藏拾取视觉对象
            if (_pickupVisual != null)
            {
                _pickupVisual.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
            
            Debug.Log($"GunMonoBehaviour: {_gunDataAsset.weaponName} picked up by {(player ? player.name : "unknown player")}");
            
            return gunCopy;
        }

        /// <summary>
        /// 播放拾取音频
        /// </summary>
        /// <param name="pickupPosition">拾取位置</param>
        private void PlayPickuoAudio(Vector3 pickupPosition)
        {
            if (_audioService == null) return;

            AudioClipType audioClipType = AudioClipType.ItemPickup;
            _audioService.PlaySFX3D(audioClipType, pickupPosition, 0.8f, 1f);

            Debug.Log($"GunMonoBehaviour: Played pickup audio {audioClipType} at {pickupPosition}");
        }

        /// <summary>
        /// 重置武器状态（用于重新生成或测试）
        /// </summary>
        public void ResetWeapon()
        {
            _isPickedUp = false;
            _isInteracting = false;
            gameObject.SetActive(true);
            Debug.Log($"GunMonoBehaviour: {_gunDataAsset.weaponName} reset");
        }

        /// <summary>
        /// 设置交互文本内容（运行时更改）
        /// </summary>
        /// <param name="newText">新的交互文本</param>
        public void SetInteractionText(string newText)
        {
            _interactionText = newText;
            if (_interactTextComponent != null)
            {
                _interactTextComponent.text = _interactionText;
            }
            Debug.Log($"GunMonoBehaviour: Updated interaction text to '{newText}'");
        }

        /// <summary>
        /// Show the interaction UI (called by InteractionService when player enters range)
        /// </summary>
        public void ShowInteractionUI()
        {
            if (_interactUI != null)
            {
                _interactUI.SetActive(true);
                Debug.Log($"GunMonoBehaviour: Showed interaction UI for {_gunDataAsset.weaponName}");
            }
        }

        /// <summary>
        /// Hide the interaction UI (called by InteractionService when player leaves range)
        /// </summary>
        public void HideInteractionUI()
        {
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
                Debug.Log($"GunMonoBehaviour: Hid interaction UI for {_gunDataAsset.weaponName}");
            }
        }

        #endregion
    }
}
