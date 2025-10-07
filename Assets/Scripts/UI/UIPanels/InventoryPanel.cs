using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.UI;
using Resonance.Core;
using Resonance.Items;
using Resonance.Utilities;
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
        
        [Header("Wave Module Panel")]
        [SerializeField] private TextMeshProUGUI _waveModuleName;
        [SerializeField] private TextMeshProUGUI _waveModuleDescription;
        [SerializeField] private Image _waveModuleIcon;
        
        // Services and Controllers
        private IInputService _inputService;
        private IPlayerService _playerService;
        private PlayerController _playerController;
        private PlayerInventory _playerInventory;
        
        // State tracking
        private bool _isInitialized = false;
        private GridItem _selectedItem;
        private Dictionary<int, GridItem> _inventoryItems = new Dictionary<int, GridItem>();

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
                    InitializeGridSystem();
                    SubscribeToPlayerEvents();
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

        #region Event Handlers

        private void OnInventoryStarted()
        {
            Debug.Log("InventoryPanel: Inventory started");
            UpdateAllUI();
        }

        private void OnItemSelected(GridItem item)
        {
            _selectedItem = item;
            UpdateItemInfoPanel(item);
            Debug.Log($"InventoryPanel: Item selected - {item?.itemName ?? "None"}");
        }

        private void OnItemDeselected(GridItem item)
        {
            if (_selectedItem == item)
            {
                _selectedItem = null;
                UpdateItemInfoPanel(null);
            }
            Debug.Log($"InventoryPanel: Item deselected - {item?.itemName ?? "None"}");
        }

        private void OnItemMoved(GridItem item)
        {
            Debug.Log($"InventoryPanel: Item moved - {item.itemName}");
            // Update player inventory if needed
            SyncGridToInventory();
        }

        private void OnItemRotated(GridItem item)
        {
            Debug.Log($"InventoryPanel: Item rotated - {item.itemName}");
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
                
                // Convert GridCellData to GridItem for UI display
                var gridItem = ConvertToGridItem(gridCellData);
                Debug.Log($"InventoryPanel: Converted to GridItem: {gridItem?.itemName ?? "NULL"}");
                
                if (gridItem != null)
                {
                    // Place item at its stored position
                    // This will create visuals since GridSystem is now initialized
                    Debug.Log($"InventoryPanel: About to place item {gridItem.itemName} at {gridCellData.GridPosition}");
                    if (_gridSystem.PlaceItem(gridItem, gridCellData.GridPosition))
                    {
                        _inventoryItems[gridCellData.ItemID] = gridItem;
                        Debug.Log($"InventoryPanel: Successfully placed item {gridItem.itemName} at {gridCellData.GridPosition}");
                    }
                    else
                    {
                        Debug.LogWarning($"InventoryPanel: Failed to place item {gridItem.itemName} at {gridCellData.GridPosition}");
                    }
                }
                else
                {
                    Debug.LogWarning($"InventoryPanel: Failed to convert GridCellData to GridItem for ID={gridCellData.ItemID}");
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
            
            // Convert to GridItem for UI display
            GridItem gridItem = ConvertToGridItem(gridCellData);
            if (gridItem == null) return;
            
            // Place item at its stored position
            if (_gridSystem.PlaceItem(gridItem, gridCellData.GridPosition))
            {
                _inventoryItems[itemID] = gridItem;
                Debug.Log($"InventoryPanel: Added item {gridItem.itemName} to grid at {gridCellData.GridPosition}");
            }
            else
            {
                Debug.LogWarning($"InventoryPanel: Failed to place item {gridItem.itemName} at {gridCellData.GridPosition}");
            }
        }

        private void RemoveInventoryItemFromGrid(int itemID)
        {
            if (_gridSystem == null || !_inventoryItems.ContainsKey(itemID)) return;
            
            GridItem gridItem = _inventoryItems[itemID];
            _gridSystem.RemoveItem(gridItem);
            _inventoryItems.Remove(itemID);
            
            Debug.Log($"InventoryPanel: Removed item {gridItem.itemName} from grid");
        }

        /// <summary>
        /// Convert GridCellData to GridItem for UI display
        /// </summary>
        private GridItem ConvertToGridItem(GridCellData cellData)
        {
            if (cellData == null) return null;
            
            var gridItem = new GridItem(
                cellData.ItemID,
                cellData.ItemName,
                cellData.ItemType,
                cellData.GridWidth,
                cellData.GridHeight,
                cellData.ItemPrefab
            );
            
            // Set position and rotation
            gridItem.SetGridPosition(cellData.GridPosition);
            if (cellData.Rotation == 90 || cellData.Rotation == 270)
            {
                gridItem.Rotate(); // Apply rotation if needed
            }
            
            // Set visual data
            gridItem.itemIcon = cellData.ItemIcon;
            gridItem.itemPrefab = cellData.ItemPrefab;
            
            // Set stack data
            gridItem.quantity = cellData.Quantity;
            gridItem.maxStackQuantity = cellData.MaxStackQuantity;
            
            // For weapons, store ammo info
            if (cellData.ItemType == ItemType.Weapon)
            {
                gridItem.customData["currentAmmo"] = cellData.CurrentAmmo;
                gridItem.customData["maxAmmo"] = cellData.MaxAmmo;
                gridItem.customData["ammoType"] = cellData.AmmoType;
            }
            
            return gridItem;
        }

        private void SyncGridToInventory()
        {
            if (_playerInventory == null || _gridSystem == null) return;
            
            // Sync grid item positions/rotations back to PlayerInventory
            var allGridItems = _gridSystem.GetAllItems();
            foreach (var gridItem in allGridItems)
            {
                var inventoryItem = _playerInventory.GetItemByID(gridItem.itemID);
                if (inventoryItem != null)
                {
                    // Update position if changed
                    if (inventoryItem.GridPosition != gridItem.gridPosition)
                    {
                        _playerInventory.MoveItemInGrid(gridItem.itemID, gridItem.gridPosition);
                    }
                    
                    // Update rotation if changed
                    int targetRotation = gridItem.isRotated ? 90 : 0;
                    if (inventoryItem.Rotation != targetRotation)
                    {
                        _playerInventory.RotateItemInGrid(gridItem.itemID);
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
            // UpdatePlayerStatusPanel();
            // UpdateItemInfoPanel(_selectedItem);
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

        private void UpdateItemInfoPanel(GridItem item)
        {
            if (_itemInfoPanel == null) return;
            
            if (item != null)
            {
                // Create InfoDataAsset from GridItem
                var infoData = CreateInfoDataFromGridItem(item);
                // _itemInfoPanel.DisplayInfoData(infoData);
            }
            else
            {
                // _itemInfoPanel.DisplayInfoData(null);
            }
        }

        private InfoDataAsset CreateInfoDataFromGridItem(GridItem item)
        {
            // Create a temporary InfoDataAsset for display
            var infoData = ScriptableObject.CreateInstance<InfoDataAsset>();
            // infoData.infoName = item.itemName;
            // infoData.infoContent = $"Type: {item.itemType}\nSize: {item.CurrentWidth}x{item.CurrentHeight}\nID: {item.itemID}";
            
            // // Add quantity and ammo info if available
            // if (item.customData.ContainsKey("quantity"))
            // {
            //     infoData.infoContent += $"\nQuantity: {item.customData["quantity"]}";
            // }
            // if (item.itemType == ItemType.Weapon && item.customData.ContainsKey("currentAmmo"))
            // {
            //     infoData.infoContent += $"\nAmmo: {item.customData["currentAmmo"]}/{item.customData["maxAmmo"]}";
            // }
            
            // infoData.infoImage = item.itemIcon;
            
            return infoData;
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
            _isInitialized = false;
            Debug.Log("InventoryPanel: Cleaned up");
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            CleanupEventListeners();
            UnsubscribeFromPlayerEvents();
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