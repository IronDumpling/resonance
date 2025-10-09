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
    /// Interactable Ammo object in the scene
    /// Players can pick up and add to ammo inventory
    /// 
    /// Visual System Responsibilities:
    /// - _pickupVisual: The visual representation for the ammo in the world (pickup state)
    /// - This handles pickup animations (bob, rotation) and interaction triggers
    /// - When picked up, the ammo count is added to player's ammo inventory
    /// </summary>
    public class AmmoMonoBehaviour : MonoBehaviour, IPickupable, IPausable
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
        public AmmoDataAsset AmmoData => _ammoDataAsset;
        public bool IsPickedUp => _isPickedUp;
        public string InteractionText => _interactionText;

        void Start()
        {
            // Validate Ammo data asset
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
            
            // Get interaction service and register as interactable object
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
        /// Set audio service reference
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
                    Debug.Log($"AmmoMonoBehaviour: Found InteractUI child object: {interactUIChild.name}");
                }
            }
            
            if (_interactUI == null)
            {
                Debug.LogWarning($"AmmoMonoBehaviour: No InteractUI found on {gameObject.name}. UI interaction will be disabled.");
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
        /// Reset ammo state (for re-generation or testing)
        /// </summary>
        public void ResetAmmo()
        {
            _isPickedUp = false;
            // _isInteracting = false;
            gameObject.SetActive(true);
        }

        #endregion

        /// <summary>
        /// Try to add this ammo to inventory
        /// Creates complete GridItem data for the ammo
        /// </summary>
        public bool TryAddToInventory(out GridItem gridItem, out string failureReason)
        {
            gridItem = null;
            failureReason = "";
            
            // Get player controller
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null || !playerService.CurrentPlayer.IsInitialized)
            {
                failureReason = "Player not initialized";
                return false;
            }
            
            var playerController = playerService.CurrentPlayer.Controller;
            var inventory = playerController.Inventory;
            
            if (inventory == null)
            {
                failureReason = "Inventory is null";
                return false;
            }
            
            // Check if inventory has space (ammo uses ConsumableManager's grid size)
            Vector2Int emptyPos = inventory.FindEmptySpace(_ammoDataAsset.gridWidth, _ammoDataAsset.gridHeight);
            if (emptyPos.x < 0 || emptyPos.y < 0)
            {
                failureReason = $"No space in inventory for {_ammoDataAsset.ammoName} ({_ammoDataAsset.gridWidth}x{_ammoDataAsset.gridHeight})";
                return false;
            }
            
            // Create complete GridItem with all necessary data
            gridItem = new GridItem
            {
                ItemID = GetInstanceID(), // Use unique ID
                ItemName = _ammoDataAsset.ammoName,
                ItemType = ItemType.Consumable,
                GridWidth = _ammoDataAsset.gridWidth,
                GridHeight = _ammoDataAsset.gridHeight,
                ItemPrefab = _ammoDataAsset.itemPrefab,
                AssetPath = GetAssetPath(_ammoDataAsset),
                ItemIcon = _ammoDataAsset.ammoIcon,
                Quantity = _ammoDataAsset.ammoCount,
                MaxStackQuantity = _ammoDataAsset.maxStackQuantity, // Ammo can stack
                Durability = 1f
            };
            
            // Store original asset for InfoPanel display
            gridItem.CustomData["originalAsset"] = _ammoDataAsset;
            gridItem.CustomData["ammoType"] = _ammoDataAsset.ammoType;
            
            return true;
        }
        
        /// <summary>
        /// Called when inventory is full and pickup fails
        /// Item stays in world for player to pick up after organizing inventory
        /// </summary>
        public void OnInventoryFull()
        {
            Debug.LogWarning($"AmmoMonoBehaviour: Inventory full! Please organize your inventory to pick up {_ammoDataAsset.ammoName}");
            
            // Reset pickup state but keep item in world
            _isPickedUp = false;
            
            // Could show special VFX or UI hint here
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
        /// <returns>Whether pickup is successful</returns>
        private bool PerformPickup()
        {
            if (_isPickedUp) return false;

            _isPickedUp = true;
            // _isInteracting = false;

            PlayPickupAudio(transform.position);
            
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

            Debug.Log($"AmmoMonoBehaviour: PerformPickup complete");
            
            return true;
        }

        /// <summary>
        /// Play pickup audio
        /// </summary>
        /// <param name="pickupPosition">Pickup position</param>
        private void PlayPickupAudio(Vector3 pickupPosition)
        {
            if (_audioService == null) return;

            // Use the same pickup sound as weapon, but slightly higher pitch to indicate it's ammo
            AudioClipType audioClipType = AudioClipType.ItemPickup;
            _audioService.PlaySFX3D(audioClipType, pickupPosition, 0.7f, 1.2f); // slightly higher pitch
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