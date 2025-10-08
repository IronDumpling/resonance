using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.UI;
using Resonance.Core;
using Resonance.Items;
using Resonance.Utilities;
using Resonance.Utilities.GridSystem;
using Resonance.Interfaces.Services;
using Resonance.Player;
using Resonance.Player.Core;
using Resonance.Player.Data;
using Resonance.Player.Inventory;
using Resonance.Core.StateMachine.States;
using System.Linq;
using System.Collections.Generic;

namespace Resonance.UI
{
    /// <summary>
    /// InventoryPanel - Player Inventory Panel
    /// Contains three main parts:
    /// 1. WaveModulePanel - Display the current equipped CrystalModule and Wave Form
    /// 2. PlayerStatusPanel - Display player status information
    /// 3. InventoryGridPanel - Contains GridSystem and ItemInfoPanel
    /// </summary>
    public class InventoryPanel : UIPanel
    {
        [Header("Panel References")]
        [SerializeField] private GameObject _waveModulePanel;
        [SerializeField] private PlayerStatusPanel _playerStatusPanel;
        [SerializeField] private GameObject _inventoryGridPanel;
        
        [Header("Grid System")]
        [SerializeField] private GridSystem _gridSystem;
        
        [Header("Item Info Panel")]
        [SerializeField] private GameObject _itemInfoPanel;
        [SerializeField] private Image _itemInfoImage;
        [SerializeField] private TextMeshProUGUI _itemInfoName;
        [SerializeField] private TextMeshProUGUI _itemInfoContent;
        [SerializeField] private GameObject _itemInfoButtonContainer;
        [SerializeField] private Button _itemUseButton;
        [SerializeField] private Button _itemCombineButton;
        [SerializeField] private Button _itemDropButton;
        
        [Header("Wave Module Panel")]
        [SerializeField] private TextMeshProUGUI _waveModuleName;
        [SerializeField] private TextMeshProUGUI _waveModuleDescription;
        [SerializeField] private Image _waveModuleIcon;
        
        // Services and Controllers
        private IInputService _inputService;
        private IPlayerService _playerService;
        private PlayerController _playerController;
        private PlayerInventory _playerInventory;
        private InventoryOperationManager _gridOperationManager;
        
        // State tracking
        private bool _isInitialized = false;
        private GridCellData _selectedItem;
        private Dictionary<int, GridCellData> _inventoryItems = new Dictionary<int, GridCellData>();
        
        // Input state tracking
        private Vector2 _currentMoveInput = Vector2.zero;
        private float _moveInputCooldown = 0.2f; // Cooldown between moves
        private float _lastMoveTime = 0f;

        protected override void Awake()
        {
            base.Awake();

            _panelName = "InventoryPanel";
            _layer = UILayer.Game;
            _hideOnStart = true;
        }

        protected override void Start()
        {
            base.Start();

            AutoDiscoverUIComponents();
            SetupEventListeners();
            InitializeWithServices();
        }

        private void AutoDiscoverUIComponents()
        {
            // Auto-find panels if not assigned
            if (_waveModulePanel == null)
                _waveModulePanel = FindChildGameObject("WaveModulePanel");
            if (_playerStatusPanel == null)
                _playerStatusPanel = GetComponentInChildren<PlayerStatusPanel>();
            if (_inventoryGridPanel == null)
                _inventoryGridPanel = FindChildGameObject("InventoryGridPanel");
            
            // Auto-find grid system
            if (_gridSystem == null)
                _gridSystem = GetComponentInChildren<GridSystem>();
            
            // Auto-find item info panel
            if (_itemInfoPanel == null)
                _itemInfoPanel = FindChildGameObject("InfoPanel");
            if (_itemInfoImage == null)
                _itemInfoImage = FindChildComponent<Image>(_itemInfoPanel, "InfoImage");
            if (_itemInfoName == null)
                _itemInfoName = FindChildComponent<TextMeshProUGUI>(_itemInfoPanel, "InfoName");
            if (_itemInfoContent == null)
                _itemInfoContent = FindChildComponent<TextMeshProUGUI>(_itemInfoPanel, "InfoContent");
            if (_itemInfoButtonContainer == null)
                _itemInfoButtonContainer = FindChildGameObject("InfoButtonContainer");
            if (_itemUseButton == null)
                _itemUseButton = FindChildComponent<Button>(_itemInfoPanel, "UseButton");
            if (_itemCombineButton == null)
                _itemCombineButton = FindChildComponent<Button>(_itemInfoPanel, "CombineButton");
            if (_itemDropButton == null)
                _itemDropButton = FindChildComponent<Button>(_itemInfoPanel, "DropButton");
            
            // Auto-find wave module components
            if (_waveModuleName == null && _waveModulePanel != null)
                _waveModuleName = FindChildComponent<TextMeshProUGUI>(_waveModulePanel, "ModuleName");
            if (_waveModuleDescription == null && _waveModulePanel != null)
                _waveModuleDescription = FindChildComponent<TextMeshProUGUI>(_waveModulePanel, "ModuleDescription");
            if (_waveModuleIcon == null && _waveModulePanel != null)
                _waveModuleIcon = FindChildComponent<Image>(_waveModulePanel, "ModuleIcon");
        }

        private void SetupEventListeners()
        {
            // Subscribe to InventoryState events
            InventoryState.OnInventoryStarted += OnInventoryStarted;
            
            Debug.Log("InventoryPanel: Event listeners setup complete");
        }

        private void InitializeWithServices()
        {
            // Get services
            _inputService = ServiceRegistry.Get<IInputService>();
            _playerService = ServiceRegistry.Get<IPlayerService>();
            
            if (_inputService == null)
            {
                Debug.LogError("InventoryPanel: InputService not found");
                return;
            }
            
            if (_playerService == null)
            {
                Debug.LogError("InventoryPanel: PlayerService not found");
                return;
            }
            
            // Check if CurrentPlayer exists and is initialized
            if (_playerService.CurrentPlayer != null && _playerService.CurrentPlayer.IsInitialized)
            {
                _playerController = _playerService.CurrentPlayer.Controller;
                if (_playerController != null)
                {
                    _playerInventory = _playerController.Inventory;
                    
                    // Get InventoryOperationManager from PlayerController
                    _gridOperationManager = _playerController.InventoryOperationManager;
                    
                    InitializeGridSystem();
                    SubscribeToPlayerEvents();
                    SubscribeToInputEvents();
                    _isInitialized = true;
                    
                    Debug.Log("InventoryPanel: Initialized successfully");
                    return;
                }
            }
            
            // Player not ready yet, subscribe to registration event and retry
            Debug.LogWarning("InventoryPanel: Player not ready yet, waiting for player registration");
            _playerService.OnPlayerRegistered += OnPlayerRegistered;
            
            // Also try again in a few frames as fallback
            Invoke(nameof(RetryInitialization), 0.1f);
        }

        private void OnPlayerRegistered(PlayerMonoBehaviour player)
        {
            Debug.Log("InventoryPanel: Player registered, attempting initialization");
            
            // Unsubscribe from the event
            if (_playerService != null)
            {
                _playerService.OnPlayerRegistered -= OnPlayerRegistered;
            }
            
            // Try to initialize now that player is registered
            if (!_isInitialized)
            {
                InitializeWithServices();
            }
        }

        private void RetryInitialization()
        {
            if (!_isInitialized)
            {
                InitializeWithServices();
            }
        }

        private void InitializeGridSystem()
        {
            Debug.Log($"InventoryPanel: InitializeGridSystem called");
            
            if (_gridSystem == null)
            {
                Debug.LogError("InventoryPanel: GridSystem not found");
                return;
            }
            
            // Get grid size from PlayerInventory (which comes from PlayerBaseStats)
            int gridWidth = _playerInventory != null ? _playerInventory.GridWidth : 5;
            int gridHeight = _playerInventory != null ? _playerInventory.GridHeight : 5;
            
            // Initialize grid with proper size
            _gridSystem.InitializeGrid(gridWidth, gridHeight);
            
            // Subscribe to grid system events
            _gridSystem.OnItemSelected += OnItemSelected;
            _gridSystem.OnItemDeselected += OnItemDeselected;
            _gridSystem.OnItemMoved += OnItemMoved;
            _gridSystem.OnItemRotated += OnItemRotated;
            
            // Initialize grid with player inventory items
            LoadInventoryItemsToGrid();
            
            Debug.Log($"InventoryPanel: Grid system initialized with size {gridWidth}x{gridHeight}");
        }

        private void SubscribeToPlayerEvents()
        {
            if (_playerInventory == null) return;
            
            // Subscribe to inventory grid events
            _playerInventory.OnItemAddedToGrid += OnInventoryItemAddedToGrid;
            _playerInventory.OnItemRemovedFromGrid += OnInventoryItemRemovedFromGrid;
            _playerInventory.OnInventoryChanged += OnInventoryChanged;
        }
        
        private void SubscribeToInputEvents()
        {
            if (_inputService == null) return;
            
            // Subscribe to inventory input events
            _inputService.OnMoveItem += OnMoveItemInput;
            _inputService.OnRotateItemLeft += OnRotateItemLeftInput;
            _inputService.OnRotateItemRight += OnRotateItemRightInput;
            
            Debug.Log("InventoryPanel: Subscribed to input events");
        }

        private void UnsubscribeFromPlayerEvents()
        {
            if (_playerInventory != null)
            {
                _playerInventory.OnItemAddedToGrid -= OnInventoryItemAddedToGrid;
                _playerInventory.OnItemRemovedFromGrid -= OnInventoryItemRemovedFromGrid;
                _playerInventory.OnInventoryChanged -= OnInventoryChanged;
            }
            
            if (_gridSystem != null)
            {
                _gridSystem.OnItemSelected -= OnItemSelected;
                _gridSystem.OnItemDeselected -= OnItemDeselected;
                _gridSystem.OnItemMoved -= OnItemMoved;
                _gridSystem.OnItemRotated -= OnItemRotated;
            }
            
            if (_playerService != null)
            {
                _playerService.OnPlayerRegistered -= OnPlayerRegistered;
            }
        }
        
        private void UnsubscribeFromInputEvents()
        {
            if (_inputService != null)
            {
                _inputService.OnMoveItem -= OnMoveItemInput;
                _inputService.OnRotateItemLeft -= OnRotateItemLeftInput;
                _inputService.OnRotateItemRight -= OnRotateItemRightInput;
            }
        }

        #region Event Handlers

        private void OnInventoryStarted()
        {
            Debug.Log("InventoryPanel: Inventory started");
            UpdateAllUI();
        }

        private void OnItemSelected(GridCellData item)
        {
            _selectedItem = item;
            UpdateItemInfoPanel(item);
            Debug.Log($"InventoryPanel: Item selected - {item?.ItemName ?? "None"}");
        }

        private void OnItemDeselected(GridCellData item)
        {
            if (_selectedItem == item)
            {
                _selectedItem = null;
                UpdateItemInfoPanel(null);
            }
            Debug.Log($"InventoryPanel: Item deselected - {item?.ItemName ?? "None"}");
        }
        
        #region Input Event Handlers
        
        private void OnMoveItemInput(Vector2 moveInput)
        {
            // Only respond if an item is selected
            if (_selectedItem == null)
            {
                return;
            }
            
            // Store input for processing in Update
            _currentMoveInput = moveInput;
        }
        
        private void OnRotateItemLeftInput()
        {
            // Only respond if an item is selected
            if (_selectedItem == null)
            {
                Debug.Log("InventoryPanel: Cannot rotate - no item selected");
                return;
            }
            
            Debug.Log($"InventoryPanel: Rotating item {_selectedItem.ItemName} left (counter-clockwise)");
            
            // Rotate in GridSystem (counter-clockwise)
            if (_gridSystem.RotateItem(_selectedItem, clockwise: false))
            {
                Debug.Log($"InventoryPanel: Successfully rotated {_selectedItem.ItemName} left");
            }
            else
            {
                Debug.LogWarning($"InventoryPanel: Failed to rotate {_selectedItem.ItemName} - no space");
            }
        }
        
        private void OnRotateItemRightInput()
        {
            // Only respond if an item is selected
            if (_selectedItem == null)
            {
                Debug.Log("InventoryPanel: Cannot rotate - no item selected");
                return;
            }
            
            Debug.Log($"InventoryPanel: Rotating item {_selectedItem.ItemName} right (clockwise)");
            
            // Rotate in GridSystem (clockwise)
            if (_gridSystem.RotateItem(_selectedItem, clockwise: true))
            {
                Debug.Log($"InventoryPanel: Successfully rotated {_selectedItem.ItemName} right");
            }
            else
            {
                Debug.LogWarning($"InventoryPanel: Failed to rotate {_selectedItem.ItemName} - no space");
            }
        }
        
        #endregion

        private void OnItemMoved(GridCellData item)
        {
            Debug.Log($"InventoryPanel: Item moved - {item.ItemName}");
            // Update player inventory if needed
            SyncGridToInventory();
        }

        private void OnItemRotated(GridCellData item)
        {
            Debug.Log($"InventoryPanel: Item rotated - {item.ItemName}");
            // Update player inventory if needed
            SyncGridToInventory();
        }

        private void OnInventoryItemAddedToGrid(GridCellData itemData, Vector2Int position)
        {
            Debug.Log($"InventoryPanel: Inventory item added to grid - ID: {itemData.ItemID}, Name: {itemData.ItemName}, Position: {position}");
            // Add item to grid if it's not already there
            AddInventoryItemToGrid(itemData.ItemID, itemData.ItemType);
        }

        private void OnInventoryItemRemovedFromGrid(GridCellData itemData, Vector2Int position)
        {
            Debug.Log($"InventoryPanel: Inventory item removed from grid - ID: {itemData.ItemID}, Name: {itemData.ItemName}, Position: {position}");
            // Remove item from grid
            RemoveInventoryItemFromGrid(itemData.ItemID);
        }

        private void OnInventoryChanged()
        {
            Debug.Log("InventoryPanel: Inventory changed");
            // Refresh grid display
            RefreshGridDisplay();
        }

        #endregion

        #region Grid Management

        private void LoadInventoryItemsToGrid()
        {
            Debug.Log($"InventoryPanel: LoadInventoryItemsToGrid called");
            
            if (_playerInventory == null || _gridSystem == null) 
            {
                Debug.LogWarning($"InventoryPanel: Cannot load items - _playerInventory={(_playerInventory != null ? "EXISTS" : "NULL")},"+
                                $"_gridSystem={(_gridSystem != null ? "EXISTS" : "NULL")}");
                return;
            }
            
            Debug.Log($"InventoryPanel: GridSystem.IsInitialized = {_gridSystem.IsInitialized}");
            
            var allItems = _playerInventory.GetAllItems();
            Debug.Log($"InventoryPanel: Total items in inventory: {allItems.Count}");
            
            // Clear existing grid items
            _gridSystem.ClearAllItems();
            _inventoryItems.Clear();
            
            // Load all items from the new grid-based inventory
            Debug.Log($"InventoryPanel: About to load {allItems.Count} items from inventory");
            foreach (var gridCellData in allItems)
            {
                Debug.Log($"InventoryPanel: Processing item: ID={gridCellData.ItemID}, Name={gridCellData.ItemName}, Position={gridCellData.GridPosition}");
                
                // Place item at its stored position (directly using GridCellData)
                // This will create visuals since GridSystem is now initialized
                Debug.Log($"InventoryPanel: About to place item {gridCellData.ItemName} at {gridCellData.GridPosition}");
                if (_gridSystem.PlaceItem(gridCellData, gridCellData.GridPosition))
                {
                    _inventoryItems[gridCellData.ItemID] = gridCellData;
                    Debug.Log($"InventoryPanel: Successfully placed item {gridCellData.ItemName} at {gridCellData.GridPosition}");
                }
                else
                {
                    Debug.LogWarning($"InventoryPanel: Failed to place item {gridCellData.ItemName} at {gridCellData.GridPosition}");
                }
            }
            
            Debug.Log($"InventoryPanel: Successfully loaded {_inventoryItems.Count} items to grid");
        }

        private void AddInventoryItemToGrid(int itemID, ItemType itemType)
        {
            // If GridSystem not initialized yet, skip - items will be loaded when panel opens
            if (_gridSystem == null || !_gridSystem.IsInitialized)
            {
                Debug.Log($"InventoryPanel: GridSystem not initialized yet. Item {itemID} will be loaded when inventory opens.");
                return;
            }
            
            if (_inventoryItems.ContainsKey(itemID))
            {
                Debug.LogWarning($"InventoryPanel: Item {itemID} already exists in grid");
                return;
            }
            
            // Get item data from PlayerInventory
            var gridCellData = _playerInventory.GetItemByID(itemID);
            if (gridCellData == null)
            {
                Debug.LogWarning($"InventoryPanel: Item {itemID} not found in PlayerInventory");
                return;
            }
            
            // Place item at its stored position (directly using GridCellData)
            if (_gridSystem.PlaceItem(gridCellData, gridCellData.GridPosition))
            {
                _inventoryItems[itemID] = gridCellData;
                Debug.Log($"InventoryPanel: Added item {gridCellData.ItemName} to grid at {gridCellData.GridPosition}");
            }
            else
            {
                Debug.LogWarning($"InventoryPanel: Failed to place item {gridCellData.ItemName} at {gridCellData.GridPosition}");
            }
        }

        private void RemoveInventoryItemFromGrid(int itemID)
        {
            if (_gridSystem == null || !_inventoryItems.ContainsKey(itemID)) return;
            
            GridCellData gridItem = _inventoryItems[itemID];
            _gridSystem.RemoveItem(gridItem);
            _inventoryItems.Remove(itemID);
            
            Debug.Log($"InventoryPanel: Removed item {gridItem.ItemName} from grid");
        }


        private void SyncGridToInventory()
        {
            if (_playerInventory == null || _gridSystem == null) return;
            
            // Sync grid item positions/rotations back to PlayerInventory
            var allGridItems = _gridSystem.GetAllItems();
            foreach (var gridItem in allGridItems)
            {
                var inventoryItem = _playerInventory.GetItemByID(gridItem.ItemID);
                if (inventoryItem != null)
                {
                    // Update position if changed
                    if (inventoryItem.GridPosition != gridItem.GridPosition)
                    {
                        _playerInventory.MoveItemInGrid(gridItem.ItemID, gridItem.GridPosition);
                    }
                    
                    // Update rotation if changed
                    int targetRotation = gridItem.IsRotated ? 90 : 0;
                    if (inventoryItem.Rotation != targetRotation)
                    {
                        _playerInventory.RotateItemInGrid(gridItem.ItemID);
                    }
                }
            }
            
            Debug.Log("InventoryPanel: Synced grid changes to inventory");
        }

        private void RefreshGridDisplay()
        {
            LoadInventoryItemsToGrid();
        }

        #endregion

        #region UI Updates

        private void UpdateAllUI()
        {
            UpdateWaveModulePanel();
            UpdatePlayerStatusPanel();
            UpdateItemInfoPanel(_selectedItem);
        }

        private void UpdateWaveModulePanel()
        {
            // Placeholder implementation
            if (_waveModuleName != null)
                _waveModuleName.text = "No Module Equipped";
            if (_waveModuleDescription != null)
                _waveModuleDescription.text = "No crystal module is currently equipped.";
            if (_waveModuleIcon != null)
                _waveModuleIcon.sprite = null;
        }

        private void UpdatePlayerStatusPanel()
        {
            // PlayerStatusPanel handles its own updates
            if (_playerStatusPanel != null)
            {
                _playerStatusPanel.gameObject.SetActive(true);
            }
        }

        private void UpdateItemInfoPanel(GridCellData item)
        {
            if (_itemInfoPanel == null) return;
            
            if (item != null)
            {
                // Create InfoDataAsset from GridCellData
                _itemInfoName.text = item.ItemName;
                // _itemInfoContent.text = item.itemDescription;
                _itemInfoImage.sprite = item.ItemIcon;
                _itemInfoButtonContainer.SetActive(true);
                // _itemUseButton.onClick.AddListener(OnItemUseButtonClicked);
                // _itemCombineButton.onClick.AddListener(OnItemCombineButtonClicked);
                // _itemDropButton.onClick.AddListener(OnItemDropButtonClicked);
            }
            else
            {
                _itemInfoName.text = null;
                _itemInfoContent.text = null;
                _itemInfoImage.sprite = null;
                _itemInfoButtonContainer.SetActive(false);
                // _itemUseButton.onClick.RemoveListener(OnItemUseButtonClicked);
                // _itemCombineButton.onClick.RemoveListener(OnItemCombineButtonClicked);
                // _itemDropButton.onClick.RemoveListener(OnItemDropButtonClicked);
            }
        }

        #endregion

        #region Utility Methods

        private GameObject FindChildGameObject(string childName)
        {
            // First try to find in direct children
            Transform child = transform.Find(childName);
            if (child != null) return child.gameObject;
            
            // Then try to find in Panel child
            Transform panel = transform.Find("Panel");
            if (panel != null)
            {
                child = panel.Find(childName);
                if (child != null) return child.gameObject;
            }
            
            return null;
        }

        private T FindChildComponent<T>(GameObject parent, string childName) where T : Component
        {
            if (parent == null) return null;
            
            Transform child = parent.transform.Find(childName);
            if (child != null)
            {
                return child.GetComponent<T>();
            }
            
            return null;
        }

        #endregion

        #region UIPanel Overrides

        protected override void OnInitialize()
        {
            Debug.Log("InventoryPanel: OnInitialize called");
        }

        protected override void OnShow()
        {
            Debug.Log($"InventoryPanel: OnShow called");
            
            // Reload all items to ensure visuals are created (in case items were picked up before panel was initialized)
            if (_isInitialized && _gridSystem != null && _gridSystem.IsInitialized)
            {
                Debug.Log($"InventoryPanel: All conditions met, reloading inventory items");
                LoadInventoryItemsToGrid();
            }
            else
            {
                Debug.LogWarning($"InventoryPanel: Cannot reload items - _isInitialized={_isInitialized}," +    
                $"_gridSystem={(_gridSystem != null ? "EXISTS" : "NULL")}, _gridSystem.IsInitialized={(_gridSystem?.IsInitialized ?? false)}");
            }
            
            UpdateAllUI();
        }

        protected override void OnHide()
        {
            Debug.Log("InventoryPanel: Hidden");
        }

        protected override void OnCleanup()
        {
            UnsubscribeFromPlayerEvents();
            UnsubscribeFromInputEvents();
            _isInitialized = false;
            Debug.Log("InventoryPanel: Cleaned up");
        }

        #endregion
        
        #region Input Processing
        
        /// <summary>
        /// Process movement input for selected item
        /// </summary>
        private void ProcessMoveInput()
        {
            if (_selectedItem == null || _currentMoveInput == Vector2.zero)
            {
                return;
            }
            
            // Convert normalized input to grid movement (one cell at a time)
            Vector2Int moveDirection = Vector2Int.zero;
            
            // Prioritize horizontal or vertical movement based on which is stronger
            if (Mathf.Abs(_currentMoveInput.x) > Mathf.Abs(_currentMoveInput.y))
            {
                // Horizontal movement
                moveDirection.x = _currentMoveInput.x > 0 ? 1 : -1;
            }
            else
            {
                // Vertical movement (inverted Y because grid Y goes down)
                moveDirection.y = _currentMoveInput.y > 0 ? -1 : 1;
            }
            
            // Calculate new position
            Vector2Int newPosition = _selectedItem.GridPosition + moveDirection;
            
            Debug.Log($"InventoryPanel: Attempting to move {_selectedItem.ItemName} from {_selectedItem.GridPosition} to {newPosition}");
            
            // Try to move item in GridSystem
            if (_gridSystem.MoveItem(_selectedItem, newPosition))
            {
                Debug.Log($"InventoryPanel: Successfully moved {_selectedItem.ItemName} to {newPosition}");
            }
            else
            {
                Debug.Log($"InventoryPanel: Cannot move {_selectedItem.ItemName} to {newPosition} - position blocked or out of bounds");
            }
        }
        
        #endregion

        #region Unity Lifecycle
        
        private void Update()
        {
            // Process movement input with cooldown
            if (_selectedItem != null && _currentMoveInput != Vector2.zero && _gridSystem != null)
            {
                // Check if enough time has passed since last move
                if (Time.time - _lastMoveTime >= _moveInputCooldown)
                {
                    ProcessMoveInput();
                    _lastMoveTime = Time.time;
                }
            }
        }

        private void OnDestroy()
        {
            CleanupEventListeners();
            UnsubscribeFromPlayerEvents();
            UnsubscribeFromInputEvents();
        }

        private void CleanupEventListeners()
        {
            // Unsubscribe from InventoryState events
            InventoryState.OnInventoryStarted -= OnInventoryStarted;

            Debug.Log("InventoryPanel: Event listeners cleaned up");
        }

        #endregion
    }
}