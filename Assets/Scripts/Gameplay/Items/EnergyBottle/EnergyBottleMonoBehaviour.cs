using UnityEngine;
using TMPro;
using Resonance.Interfaces.Services;
using Resonance.Interfaces.Objects;
using Resonance.Utilities;
using Resonance.Utilities.Types;
using Resonance.Utilities.GridSystem;
using Resonance.Core;
using Resonance.Core.GlobalServices;

namespace Resonance.Items
{
    /// <summary>
    /// Energy Bottle MonoBehaviour - Handles the visual and interaction system for energy bottles
    /// Responsibilities: pickup, interact, visual animations
    /// </summary>
    public class EnergyBottleMonoBehaviour : MonoBehaviour, IPickupable, IPausable
    {
        [Header("Energy Bottle Configuration")]
        [SerializeField] private EnergyBottleDataAsset _energyBottleDataAsset;
        
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
        
        // State
        private bool _isPickedUp = false;
        private Vector3 _originalPosition;
        private float _bobTimer = 0f;
        
        // Services
        private IInteractionService _interactionService;
        private IAudioService _audioService;
        private ISelectivePauseService _selectivePauseService;

        // Properties
        public EnergyBottleDataAsset EnergyBottleData => _energyBottleDataAsset;
        public bool IsPickedUp => _isPickedUp;
        public string InteractionText => _interactionText;

        void Start()
        {
            // Validate data asset
            if (_energyBottleDataAsset == null)
            {
                Debug.LogError($"EnergyBottleMonoBehaviour: No EnergyBottleDataAsset assigned to {gameObject.name}!");
                return;
            }

            // Record original position for animation
            _originalPosition = transform.position;
            
            // If no pickup visual model is specified, use itself
            if (_pickupVisual == null)
            {
                _pickupVisual = gameObject;
            }

            // Setup services
            SetupAudioService();
            SetupInteractionUI();
            RegisterInteractionService();
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

            if (_selectivePauseService != null)
            {
                _selectivePauseService.UnregisterPausable(this);
            }
        }

        void Update()
        {
            if (_isPickedUp || _pickupVisual == null) return;

            // Rotation animation
            if (_rotateWhenIdle)
            {
                _pickupVisual.transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
            }

            // Bob up and down animation
            if (_bobUpAndDown)
            {
                _bobTimer += Time.deltaTime * _bobSpeed;
                float yOffset = Mathf.Sin(_bobTimer) * _bobHeight;
                transform.position = _originalPosition + new Vector3(0f, yOffset, 0f);
            }
        }

        #region Setup Methods

        private void SetupAudioService()
        {
            _audioService = ServiceRegistry.Get<IAudioService>();
            if (_audioService == null)
            {
                Debug.LogWarning("EnergyBottleMonoBehaviour: AudioService not found");
            }
        }

        private void SetupInteractionUI()
        {
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }

            if (_interactTextComponent != null)
            {
                _interactTextComponent.text = _interactionText;
            }
        }

        private void RegisterInteractionService()
        {
            _interactionService = ServiceRegistry.Get<IInteractionService>();
            if (_interactionService == null)
            {
                Debug.LogWarning("EnergyBottleMonoBehaviour: InteractionService not found");
                return;
            }

            _interactionService.RegisterInteractable(this.gameObject);
        }

        private void RegisterWithPauseService()
        {
            _selectivePauseService = ServiceRegistry.Get<ISelectivePauseService>();
            if (_selectivePauseService == null)
            {
                Debug.LogWarning("EnergyBottleMonoBehaviour: SelectivePauseService not found");
                return;
            }

            _selectivePauseService.RegisterPausable(this);
        }

        #endregion

        #region IPickupable Implementation

        /// <summary>
        /// Try to add this energy bottle to inventory
        /// Creates GridItem data and prepares for pickup
        /// </summary>
        public bool TryAddToInventory(out GridItem gridItem, out string failureReason)
        {
            gridItem = null;
            failureReason = "";

            if (_isPickedUp)
            {
                failureReason = "Item already picked up";
                return false;
            }

            if (_energyBottleDataAsset == null)
            {
                failureReason = "Missing data asset";
                return false;
            }

            // Create grid item for inventory
            int itemID = _energyBottleDataAsset.GetInstanceID();
            gridItem = new GridItem
            {
                ItemID = itemID,
                ItemType = ItemType.Consumable,
                ConsumableType = ConsumableType.EnergyBottle,
                GridWidth = _energyBottleDataAsset.gridWidth,
                GridHeight = _energyBottleDataAsset.gridHeight,
                Quantity = 1,
                MaxStackQuantity = _energyBottleDataAsset.maxStackQuantity,
                ItemIcon = _energyBottleDataAsset.itemIcon,
                ItemPrefab = _energyBottleDataAsset.itemPrefab,
                AssetPath = GetAssetPath(_energyBottleDataAsset),
                Durability = 1f
            };
            gridItem.CustomData["originalAsset"] = _energyBottleDataAsset;
            gridItem.CustomData["itemName"] = _energyBottleDataAsset.itemName;

            return true;
        }

        /// <summary>
        /// Called when inventory is full and pickup fails
        /// Item stays in world for player to pick up later
        /// </summary>
        public void OnInventoryFull()
        {
            Debug.LogWarning($"EnergyBottleMonoBehaviour: Inventory full, cannot pick up {_energyBottleDataAsset?.itemName}");
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
            if (_isPickedUp) return;
            
            _isPickedUp = true;
            
            // Play pickup audio
            PlayPickupAudio();
            
            // Remove from interaction service
            if (_interactionService != null)
            {
                _interactionService.UnregisterInteractable(gameObject);
                if (_interactionService.CurrentInteractable == gameObject)
                {
                    _interactionService.ClearCurrentInteractable();
                }
            }
            
            // Hide visual
            if (_pickupVisual != null)
            {
                _pickupVisual.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            Debug.Log($"EnergyBottleMonoBehaviour: Picked up {_energyBottleDataAsset.itemName}");
        }

        #endregion

        #region IInteractable Implementation

        // IInteractable base methods
        public bool CanInteract()
        {
            return !_isPickedUp && _energyBottleDataAsset != null;
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
            return _energyBottleDataAsset?.itemName ?? "Unknown Energy Bottle";
        }

        #endregion

        #region IPausable Implementation

        private bool _isPaused = false;

        public bool IsPaused => _isPaused;

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        #endregion

        #region Helper Methods

        private void PlayPickupAudio()
        {
            if (_audioService != null)
            {
                _audioService.PlaySFX3D(AudioClipType.ItemPickup, transform.position, 0.6f, 1f);
            }
        }

        private string GetAssetPath(ScriptableObject asset)
        {
            if (asset == null) return "";
            
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(asset);
            #else
            return asset.name;
            #endif
        }

        #endregion
    }
}

