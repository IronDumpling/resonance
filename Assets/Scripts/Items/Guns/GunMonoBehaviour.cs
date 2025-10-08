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
    public class GunMonoBehaviour : MonoBehaviour, IPickupable, IPausable
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
        
        // Whether it has been picked up
        private bool _isPickedUp = false;
        
        // Interaction state
        // private bool _isInteracting = false;
        
        // Animation related
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
            // Validate Gun data asset
            if (_gunDataAsset == null)
            {
                Debug.LogError($"GunMonoBehaviour: No GunDataAsset assigned to {gameObject.name}!");
                return;
            }

            // Record original position for animation
            _originalPosition = transform.position;
            
            // If no pickup visual model is specified, use itself
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
            // Clean up interaction service registration
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
            
            // Perform visual animations
            PerformVisualAnimations();
        }

        #region Setup

        /// <summary>
        /// Setup audio service reference
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
        /// Setup interaction UI - find and set InteractUI and Text components
        /// </summary>
        private void SetupInteractionUI()
        {
            // Find InteractUI child object (if not manually assigned)
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
            
            // Find Text component
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

        #region IPickupable Implementation

        // IInteractable base methods
        public bool CanInteract()
        {
            return !_isPickedUp;
        }

        public float GetInteractionDuration()
        {
            return _interactionDuration;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

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
        /// Reset weapon state (for re-generation or testing)
        /// </summary>
        public void ResetWeapon()
        {
            _isPickedUp = false;
            // _isInteracting = false;
            gameObject.SetActive(true);
        }

        #endregion

        /// <summary>
        /// Try to add this weapon to inventory
        /// Creates GridItem data and checks if inventory has space
        /// </summary>
        public bool TryAddToInventory(out GridItem gridItem, out string failureReason)
        {
            gridItem = null;
            failureReason = "";
            
            // Create runtime copy of weapon data
            var gunCopy = _gunDataAsset.CreateRuntimeCopy();
            if (gunCopy == null)
            {
                failureReason = "Failed to create weapon copy";
                return false;
            }
            
            // Get player controller
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null || !playerService.CurrentPlayer.IsInitialized)
            {
                failureReason = "Player not initialized";
                return false;
            }
            
            var playerController = playerService.CurrentPlayer.Controller;
            var inventory = playerController.Inventory;
            
            // Check if inventory has space
            Vector2Int emptyPos = inventory.FindEmptySpace(gunCopy.gridWidth, gunCopy.gridHeight);
            if (emptyPos.x < 0 || emptyPos.y < 0)
            {
                failureReason = $"No space in inventory for {gunCopy.weaponName} ({gunCopy.gridWidth}x{gunCopy.gridHeight})";
                return false;
            }
            
            // Create GridItem
            int weaponID = gunCopy.GetInstanceID();
            gridItem = new GridItem
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
            gridItem.CustomData["originalAsset"] = gunCopy;
            gridItem.CustomData["weaponName"] = gunCopy.weaponName;
            
            return true;
        }
        
        /// <summary>
        /// Called when inventory is full and pickup fails
        /// Item stays in world for player to pick up after organizing inventory
        /// </summary>
        public void OnInventoryFull()
        {
            Debug.LogWarning($"GunMonoBehaviour: Inventory full! Please organize your inventory to pick up {_gunDataAsset.weaponName}");
            
            // Reset pickup state but keep item in world
            _isPickedUp = false;
            
            // Could show special VFX or UI hint here
            // For example: make the item glow to indicate "failed to pickup"
        }
        
        /// <summary>
        /// Destroy this pickup item from the world
        /// Called after successful pickup
        /// </summary>
        public void DestroyPickupItem()
        {
            PerformPickup(); // This handles all cleanup
        }

        #region Private Methods

        /// <summary>
        /// Internal pickup logic - handles cleanup and visual effects
        /// </summary>
        /// <returns>Weapon data copy</returns>
        private GunDataAsset PerformPickup()
        {
            if (_isPickedUp) return null;

            _isPickedUp = true;
            // _isInteracting = false;

            PlayPickupAudio(transform.position);
            
            // Create runtime copy
            GunDataAsset gunCopy = _gunDataAsset.CreateRuntimeCopy();
            
            // Stop all animations
            StopAllCoroutines();
            
            // Remove from interaction service
            if (_interactionService != null)
            {
                _interactionService.UnregisterInteractable(gameObject);
                if (_interactionService.CurrentInteractable == gameObject)
                {
                    _interactionService.ClearCurrentInteractable();
                }
            }
            
            // Hide pickup visual object
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
        /// Play pickup audio
        /// </summary>
        /// <param name="pickupPosition">Pickup position</param>
        private void PlayPickupAudio(Vector3 pickupPosition)
        {
            if (_audioService == null) return;

            AudioClipType audioClipType = AudioClipType.PistoArming;
            _audioService.PlaySFX3D(audioClipType, pickupPosition, 0.8f, 1f);
        }

        /// <summary>
        /// Get the asset path of the ScriptableObject
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
