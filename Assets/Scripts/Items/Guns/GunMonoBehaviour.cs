using UnityEngine;
using TMPro;
using Resonance.Interfaces.Services;
using Resonance.Interfaces.Objects;
using Resonance.Utilities;
using Resonance.Utilities.GridSystem;
using Resonance.Core;
using Resonance.Core.GlobalServices;

namespace Resonance.Items
{
    /// <summary>
    /// Gun MonoBehaviour - Handles the visual and interaction system for guns
    /// Responsibilities: pickup, equip, interact with gun, visual animations
    /// </summary>
    public class GunMonoBehaviour : MonoBehaviour, IInteractable, IPausable
    {
        [Header("Gun Configuration")]
        [SerializeField] private GunDataAsset _gunDataAsset;
        
        [Header("Interaction")]
        [SerializeField] private string _interactionText = "E";
        [SerializeField] private float _interactionDuration = 0.2f; 
        
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
                
        // Services
        private IInteractionService _interactionService;
        private IAudioService _audioService;

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
            
            // Register interaction service
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
                Transform textChild = _interactUI.transform.Find("Text");
                if (textChild != null)
                {
                    _interactTextComponent = textChild.GetComponent<TextMeshProUGUI>();
                }
            }
            
            if (_interactTextComponent == null)
            {
                Debug.LogWarning($"GunMonoBehaviour: No TextMeshProUGUI component found in InteractUI on {gameObject.name}");
            }
            else
            {
                Debug.Log($"GunMonoBehaviour: Found TextMeshProUGUI component for interaction UI");
                _interactTextComponent.text = _interactionText;
            }
            
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }

            Debug.Log($"GunMonoBehaviour: Interaction UI setup complete");
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
                Debug.Log("GunMonoBehaviour: Registered with SelectivePauseService");
            }
            else
            {
                Debug.LogWarning("GunMonoBehaviour: SelectivePauseService not found, pause functionality will not work");
            }
        }

        #endregion

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
            // Can interact if not picked up 
            return !_isPickedUp;
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
            var gunCopy = PerformPickup();
            
            if (gunCopy != null && playerTransform != null)
            {
                // Try to equip the weapon to the player using the new inventory system
                var playerMono = playerTransform.GetComponent<Resonance.Player.PlayerMonoBehaviour>();
                if (playerMono != null && playerMono.IsInitialized)
                {
                    /* ======= TODO Temporary Auto-Equip ====== */
                    var playerController = playerMono.Controller;
                    if (playerController != null)
                    {
                        var inventory = playerController.Inventory;
                        var weaponManager = playerController.WeaponManager;
                        
                        if (inventory != null && weaponManager != null)
                        {
                            // Step 1: Create GridCellData for the weapon
                            int weaponID = gunCopy.GetInstanceID();
                            var weaponData = new GridCellData
                            {
                                ItemID = weaponID,
                                ItemName = gunCopy.weaponName,
                                ItemType = ItemType.Weapon,
                                GridWidth = gunCopy.gridWidth,
                                GridHeight = gunCopy.gridHeight,
                                CurrentAmmo = gunCopy.CurrentAmmo,
                                AmmoType = gunCopy.ammoType,
                                MaxAmmo = gunCopy.maxAmmo,
                                AssetPath = GetAssetPath(_gunDataAsset),
                                ItemPrefab = gunCopy.itemPrefab,
                                ItemIcon = gunCopy.weaponIcon, 
                                Quantity = 1,
                                MaxStackQuantity = 1,
                                Durability = 1f
                            };
                            weaponData.CustomData["originalAsset"] = gunCopy;
                            weaponData.CustomData["weaponName"] = gunCopy.weaponName;
                            
                            // Step 2: Find empty space in inventory
                            Vector2Int emptyPos = inventory.FindEmptySpace(weaponData.GridWidth, weaponData.GridHeight);
                            if (emptyPos.x >= 0 && emptyPos.y >= 0)
                            {
                                // Step 3: Add to inventory grid
                                bool added = inventory.AddItemToGrid(weaponData, emptyPos);
                                if (added)
                                {
                                    Debug.Log($"GunMonoBehaviour: Added {gunCopy.weaponName} to inventory at {emptyPos}");
                                    
                                    // Step 4: Equip weapon (TEMPORARY - auto equip on pickup)
                                    // TODO: In future, let player manually equip from inventory panel
                                    bool equipped = weaponManager.EquipWeapon(weaponID);
                                    if (equipped)
                                    {
                                        Debug.Log($"GunMonoBehaviour: Successfully equipped {gunCopy.weaponName} (TEMPORARY AUTO-EQUIP)");
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"GunMonoBehaviour: Added to inventory but failed to equip {gunCopy.weaponName}");
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"GunMonoBehaviour: Failed to add {gunCopy.weaponName} to inventory");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"GunMonoBehaviour: No space in inventory for {gunCopy.weaponName}");
                            }
                        }
                        else
                        {
                            Debug.LogError("GunMonoBehaviour: Player's Inventory or WeaponManager is null");
                        }
                    }
                    else
                    {
                        Debug.LogError("GunMonoBehaviour: Player's Controller is null");
                    }
                    /* ======= TODO Temporary Auto-Equip ====== */
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

            // Show info panel for the gun
            ShowGunInfo();

            Debug.Log($"GunMonoBehaviour: CompleteInteraction complete");
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

        #region IPausable Implementation

        private bool _isPaused = false;

        public bool IsPaused => _isPaused;

        public void Pause()
        {
            if (_isPaused) return;
            
            _isPaused = true;
            Debug.Log("GunMonoBehaviour: Paused - animations stopped");
            
            // Note: This only pauses visual animations in Update()
            // UI interactions and pickup functionality remain active
        }

        public void Resume()
        {
            if (!_isPaused) return;
            
            _isPaused = false;
            Debug.Log("GunMonoBehaviour: Resumed - animations restarted");
            
            // Animations will resume in the next Update() call
        }

        #endregion

        #region Info Display

        /// <summary>
        /// Show info panel for this gun using the unified InfoDisplay system
        /// </summary>
        private void ShowGunInfo()
        {
            if (_gunDataAsset == null)
            {
                Debug.LogError("GunMonoBehaviour: Cannot show gun info with null GunDataAsset");
                return;
            }

            // Use the unified InfoDisplayService
            InfoDisplayService.ShowInfo(_gunDataAsset);
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
        /// 重置武器状态（用于重新生成或测试）
        /// </summary>
        public void ResetWeapon()
        {
            _isPickedUp = false;
            _isInteracting = false;
            gameObject.SetActive(true);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 拾取武器（新系统内部使用）
        /// </summary>
        /// <returns>武器数据的副本</returns>
        private GunDataAsset PerformPickup()
        {
            if (_isPickedUp) return null;

            _isPickedUp = true;
            _isInteracting = false;

            PlayPickupAudio(transform.position);
            
            // 创建运行时副本
            GunDataAsset gunCopy = _gunDataAsset.CreateRuntimeCopy();
            
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

            Debug.Log($"GunMonoBehaviour: PerformPickup complete");
            
            return gunCopy;
        }

        /// <summary>
        /// 播放拾取音频
        /// </summary>
        /// <param name="pickupPosition">拾取位置</param>
        private void PlayPickupAudio(Vector3 pickupPosition)
        {
            if (_audioService == null) return;

            AudioClipType audioClipType = AudioClipType.PistoArming;
            _audioService.PlaySFX3D(audioClipType, pickupPosition, 0.8f, 1f);
        }

        /// <summary>
        /// 获取ScriptableObject的资源路径
        /// </summary>
        private string GetAssetPath(ScriptableObject asset)
        {
            if (asset == null) return "";
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(asset);
            #else
            return asset.name; // Runtime fallback
            #endif
        }

        #endregion
    }
}
