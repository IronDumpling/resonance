using UnityEngine;
using TMPro;
using Resonance.Interfaces.Services;
using Resonance.Interfaces.Objects;
using Resonance.Utilities;
using Resonance.Core;
using Resonance.Core.GlobalServices;

namespace Resonance.Items
{
    /// <summary>
    /// 场景中可交互的Ammo物体
    /// 玩家可以拾取并添加到弹药库存中
    /// 
    /// Visual System Responsibilities:
    /// - _pickupVisual: The visual representation for the ammo in the world (pickup state)
    /// - This handles pickup animations (bob, rotation) and interaction triggers
    /// - When picked up, the ammo count is added to player's ammo inventory
    /// </summary>
    public class AmmoMonoBehaviour : MonoBehaviour, IInteractable, IPausable
    {
        [Header("Ammo Configuration")]
        [SerializeField] private AmmoDataAsset _ammoDataAsset;

        [Header("Interaction")]
        [SerializeField] private string _interactionText = "E";
        [SerializeField] private float _interactionDuration = 0.1f;
        
        [Header("Pickup Visual")]
        [SerializeField] private GameObject _pickupVisual;
        [SerializeField] private bool _rotateWhenIdle = true;
        [SerializeField] private float _rotationSpeed = 45f; 
        [SerializeField] private bool _bobUpAndDown = true;
        [SerializeField] private float _bobSpeed = 3f;     
        [SerializeField] private float _bobHeight = 0.15f; 
        
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
                
        // Services
        private IInteractionService _interactionService;
        private IAudioService _audioService;

        // Properties
        public AmmoDataAsset AmmoData => _ammoDataAsset;
        public bool IsPickedUp => _isPickedUp;
        public string InteractionText => _interactionText;

        void Start()
        {
            // 验证Ammo数据资产
            if (_ammoDataAsset == null)
            {
                Debug.LogError($"AmmoMonoBehaviour: No AmmoDataAsset assigned to {gameObject.name}!");
                return;
            }

            if (!_ammoDataAsset.ValidateData())
            {
                Debug.LogError($"AmmoMonoBehaviour: Invalid AmmoDataAsset on {gameObject.name}!");
                return;
            }

            // 记录原始位置用于动画
            _originalPosition = transform.position;
            
            // 如果没有指定拾取视觉模型，使用自身
            if (_pickupVisual == null)
            {
                _pickupVisual = gameObject;
            }

            // Setup audio service
            SetupAudioService();
            
            // Setup interaction UI
            SetupInteractionUI();
            
            // Get interaction service and register as interactable object
            RegisterInteractionService();

            // Register with SelectivePauseService
            RegisterWithPauseService();
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
            if (_isPickedUp || _isPaused) return;
            
            // 执行视觉动画
            PerformVisualAnimations();
        }

        #region Setup
        
        /// <summary>
        /// 设置音频服务引用
        /// </summary>
        private void SetupAudioService()
        {
            _audioService = ServiceRegistry.Get<IAudioService>();
            if (_audioService == null)
            {
                Debug.LogWarning("AmmoMonoBehaviour: AudioService not found. Audio effects will be disabled.");
            }
            else
            {
                Debug.Log("AmmoMonoBehaviour: AudioService connected successfully");
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
                    Debug.Log($"AmmoMonoBehaviour: Found InteractUI child object: {interactUIChild.name}");
                }
            }
            
            if (_interactUI == null)
            {
                Debug.LogWarning($"AmmoMonoBehaviour: No InteractUI found on {gameObject.name}. UI interaction will be disabled.");
                return;
            }
            
            // 查找Text组件
            if (_interactTextComponent == null)
            {
                Transform textChild = _interactUI.transform.Find("Text");
                if (textChild != null)
                {
                    _interactTextComponent = textChild.GetComponent<TextMeshProUGUI>();
                }
            }
            
            if (_interactTextComponent == null)
            {
                Debug.LogWarning($"AmmoMonoBehaviour: No TextMeshProUGUI component found in InteractUI on {gameObject.name}");
            }
            else
            {
                Debug.Log($"AmmoMonoBehaviour: Found TextMeshProUGUI component for interaction UI");
                _interactTextComponent.text = _interactionText;
            }
            
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }

            Debug.Log($"AmmoMonoBehaviour: Interaction UI setup complete");
        }

        private void RegisterInteractionService()
        {
            _interactionService = ServiceRegistry.Get<IInteractionService>();
            if (_interactionService != null)
            {
                _interactionService.RegisterInteractable(gameObject);
            }
        }

        private void RegisterWithPauseService()
        {
            var pauseService = ServiceRegistry.Get<ISelectivePauseService>();
            if (pauseService != null)
            {
                pauseService.RegisterPausable(this);
                Debug.Log("AmmoMonoBehaviour: Registered with SelectivePauseService");
            }
            else
            {
                Debug.LogWarning("AmmoMonoBehaviour: SelectivePauseService not found, pause functionality will not work");
            }

            Debug.Log("AmmoMonoBehaviour: Initialized with base stats, weapon manager, state machine, and action controller");
        }

        #endregion

        /// <summary>
        /// Perform visual animations
        /// </summary>
        private void PerformVisualAnimations()
        {
            if (_pickupVisual == null) return;

            Vector3 currentPosition = _originalPosition;
            Vector3 currentRotation = _pickupVisual.transform.eulerAngles;

            // Up and down animation
            if (_bobUpAndDown)
            {
                _bobTimer += Time.deltaTime * _bobSpeed;
                float bobOffset = Mathf.Sin(_bobTimer) * _bobHeight;
                currentPosition.y = _originalPosition.y + bobOffset;
            }

            // Rotation animation
            if (_rotateWhenIdle)
            {
                currentRotation.y += _rotationSpeed * Time.deltaTime;
            }

            // Apply transform
            transform.position = currentPosition;
            _pickupVisual.transform.eulerAngles = currentRotation;
        }

        #region IInteractable Implementation

        /// <summary>
        /// Check if this ammo can currently be interacted with
        /// </summary>
        /// <returns>True if interaction is possible</returns>
        public bool CanInteract()
        {
            // Can interact if not picked up 
            return !_isPickedUp;
        }

        /// <summary>
        /// Get the interaction duration for picking up this ammo
        /// </summary>
        /// <returns>Duration of the interaction in seconds</returns>
        public float GetInteractionDuration()
        {
            return _interactionDuration;
        }

        /// <summary>
        /// Get the world position of this ammo
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
                Debug.LogWarning($"AmmoMonoBehaviour: Cannot start interaction with {_ammoDataAsset.ammoName}");
                return;
            }

            _isInteracting = true;

            Debug.Log($"AmmoMonoBehaviour: Started interaction with {_ammoDataAsset.ammoName}");

            // Show interaction UI
            if (_interactUI != null)
            {
                _interactUI.SetActive(true);
            }

            // TODO: Play interaction start effects (visual/audio feedback)
        }

        /// <summary>
        /// Complete the interaction successfully - pickup the ammo
        /// </summary>
        public void CompleteInteraction()
        {
            if (!_isInteracting)
            {
                Debug.LogWarning($"AmmoMonoBehaviour: CompleteInteraction called but not interacting with {_ammoDataAsset.ammoName}");
                return;
            }

            Debug.Log($"AmmoMonoBehaviour: Completing interaction with {_ammoDataAsset.ammoName}");

            // Get current player from PlayerService
            var playerService = ServiceRegistry.Get<IPlayerService>();
            Transform playerTransform = null;
            
            if (playerService?.CurrentPlayer != null)
            {
                playerTransform = playerService.CurrentPlayer.transform;
            }

            // Perform the actual pickup
            bool pickupSuccess = PerformPickup();
            
            if (pickupSuccess && playerTransform != null)
            {
                // Try to add ammo to player's new inventory system
                var playerMono = playerTransform.GetComponent<Resonance.Player.PlayerMonoBehaviour>();
                if (playerMono != null && playerMono.IsInitialized)
                {
                    var playerController = playerMono.Controller;
                    if (playerController != null)
                    {
                        // 使用新的统一背包系统添加弹药
                        var inventory = playerController.Inventory;
                        if (inventory != null)
                        {
                            // 添加弹药到统一背包系统
                            bool added = inventory.AddConsumable(_ammoDataAsset.GetInstanceID(), _ammoDataAsset.ammoCount);
                            if (added)
                            {
                                Debug.Log($"AmmoMonoBehaviour: Successfully added {_ammoDataAsset.ammoCount} {_ammoDataAsset.ammoType} ammo to new inventory system");
                                
                                // 同时添加到传统的弹药库存系统（保持兼容性）
                                var playerStats = playerController.Stats;
                                if (playerStats?.ammoInventory != null)
                                {
                                    playerStats.ammoInventory.AddAmmo(_ammoDataAsset.ammoType, _ammoDataAsset.ammoCount);
                                    
                                    // Log current inventory state for debugging
                                    int currentCount = playerStats.ammoInventory.GetAmmoCount(_ammoDataAsset.ammoType);
                                    Debug.Log($"AmmoMonoBehaviour: Player now has {currentCount} total {_ammoDataAsset.ammoType} ammo in legacy system");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"AmmoMonoBehaviour: Failed to add {_ammoDataAsset.ammoType} ammo to inventory - inventory full");
                            }
                        }
                        else
                        {
                            Debug.LogError("AmmoMonoBehaviour: Player's inventory not found");
                        }
                    }
                    else
                    {
                        Debug.LogError("AmmoMonoBehaviour: Player's Controller is null");
                    }
                }
                else
                {
                    Debug.LogError("AmmoMonoBehaviour: Player not found or not initialized");
                }
            }

            _isInteracting = false;

            // Hide interaction UI
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }

            // Show info panel for the ammo
            ShowAmmoInfo();

            Debug.Log($"AmmoMonoBehaviour: CompleteInteraction complete");
        }

        /// <summary>
        /// Cancel the interaction
        /// </summary>
        public void CancelInteraction()
        {
            if (!_isInteracting) return;

            Debug.Log($"AmmoMonoBehaviour: Cancelled interaction with {_ammoDataAsset.ammoName}");

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
        /// <returns>Ammo name</returns>
        public string GetInteractableName()
        {
            return _ammoDataAsset?.ammoName ?? "Unknown Ammo";
        }

        #endregion

        #region IPausable Implementation

        private bool _isPaused = false;

        public bool IsPaused => _isPaused;

        public void Pause()
        {
            if (_isPaused) return;
            
            _isPaused = true;
            Debug.Log("AmmoMonoBehaviour: Paused - animations stopped");
            
            // Note: This only pauses visual animations in Update()
            // UI interactions and pickup functionality remain active
        }

        public void Resume()
        {
            if (!_isPaused) return;
            
            _isPaused = false;
            Debug.Log("AmmoMonoBehaviour: Resumed - animations restarted");
            
            // Animations will resume in the next Update() call
        }

        #endregion

        #region Info Display

        /// <summary>
        /// Show info panel for this ammo using the unified InfoDisplay system
        /// </summary>
        private void ShowAmmoInfo()
        {
            if (_ammoDataAsset == null)
            {
                Debug.LogError("AmmoMonoBehaviour: Cannot show ammo info with null AmmoDataAsset");
                return;
            }

            // Use the unified InfoDisplayService
            InfoDisplayService.ShowInfo(_ammoDataAsset);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the interaction UI (called by PlayerInteractTrigger when player enters range)
        /// </summary>
        public void ShowInteractionUI()
        {
            if (_interactUI != null)
            {
                _interactUI.SetActive(true);
            }
        }

        /// <summary>
        /// Hide the interaction UI (called by PlayerInteractTrigger when player leaves range)
        /// </summary>
        public void HideInteractionUI()
        {
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }
        }

        /// <summary>
        /// 重置弹药状态（用于重新生成或测试）
        /// </summary>
        public void ResetAmmo()
        {
            _isPickedUp = false;
            _isInteracting = false;
            gameObject.SetActive(true);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 拾取弹药（内部使用）
        /// </summary>
        /// <returns>是否成功拾取</returns>
        private bool PerformPickup()
        {
            if (_isPickedUp) return false;

            _isPickedUp = true;
            _isInteracting = false;

            PlayPickupAudio(transform.position);
            
            // 停止所有动画
            StopAllCoroutines();
            
            // 从交互服务中移除
            if (_interactionService != null)
            {
                _interactionService.UnregisterInteractable(gameObject);
                if (_interactionService.CurrentInteractable == gameObject)
                {
                    _interactionService.ClearCurrentInteractable();
                }
            }
            
            // 隐藏拾取视觉对象
            if (_pickupVisual != null)
            {
                _pickupVisual.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            Debug.Log($"AmmoMonoBehaviour: PerformPickup complete");
            
            return true;
        }

        /// <summary>
        /// 播放拾取音频
        /// </summary>
        /// <param name="pickupPosition">拾取位置</param>
        private void PlayPickupAudio(Vector3 pickupPosition)
        {
            if (_audioService == null) return;

            // 使用与武器相同的拾取音效，但音调稍高一些表示是弹药
            AudioClipType audioClipType = AudioClipType.ItemPickup;
            _audioService.PlaySFX3D(audioClipType, pickupPosition, 0.7f, 1.2f); // 稍微高一些的音调
        }

        #endregion
    }
}