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
        [SerializeField] private GameObject _gridSlotPrefab;
        
        [Header("Item Info Panel")]
        [SerializeField] private InfoPanel _itemInfoPanel;
        
        [Header("Wave Module Panel (Placeholder)")]
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
                _itemInfoPanel = GetComponentInChildren<InfoPanel>();
            
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
            if (_gridSystem == null)
            {
                Debug.LogError("InventoryPanel: GridSystem not found");
                return;
            }
            
            // Subscribe to grid system events
            _gridSystem.OnItemSelected += OnItemSelected;
            _gridSystem.OnItemDeselected += OnItemDeselected;
            _gridSystem.OnItemMoved += OnItemMoved;
            _gridSystem.OnItemRotated += OnItemRotated;
            
            // Initialize grid with player inventory items
            LoadInventoryItemsToGrid();
            
            Debug.Log("InventoryPanel: Grid system initialized");
        }

        private void SubscribeToPlayerEvents()
        {
            if (_playerInventory == null) return;
            
            // Subscribe to inventory events
            _playerInventory.OnItemAdded += OnInventoryItemAdded;
            _playerInventory.OnItemRemoved += OnInventoryItemRemoved;
            _playerInventory.OnInventoryChanged += OnInventoryChanged;
        }

        private void UnsubscribeFromPlayerEvents()
        {
            if (_playerInventory != null)
            {
                _playerInventory.OnItemAdded -= OnInventoryItemAdded;
                _playerInventory.OnItemRemoved -= OnInventoryItemRemoved;
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

        private void OnInventoryItemAdded(int itemID, ItemType itemType)
        {
            Debug.Log($"InventoryPanel: Inventory item added - ID: {itemID}, Type: {itemType}");
            // Add item to grid if it's not already there
            AddInventoryItemToGrid(itemID, itemType);
        }

        private void OnInventoryItemRemoved(int itemID, ItemType itemType)
        {
            Debug.Log($"InventoryPanel: Inventory item removed - ID: {itemID}, Type: {itemType}");
            // Remove item from grid
            RemoveInventoryItemFromGrid(itemID);
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
            if (_playerInventory == null || _gridSystem == null) return;
            
            // Clear existing grid items
            _gridSystem.ClearAllItems();
            _inventoryItems.Clear();
            
            // Load weapons
            var weapons = _playerInventory.GetAllWeapons();
            foreach (var weaponItem in weapons)
            {
                AddInventoryItemToGrid(weaponItem.ItemID, ItemType.Weapon);
            }
            
            // Load consumables
            var consumables = _playerInventory.GetAllConsumables();
            foreach (var consumableItem in consumables)
            {
                AddInventoryItemToGrid(consumableItem.ItemID, ItemType.Consumable);
            }
            
            Debug.Log($"InventoryPanel: Loaded {_inventoryItems.Count} items to grid");
        }

        private void AddInventoryItemToGrid(int itemID, ItemType itemType)
        {
            if (_gridSystem == null || _inventoryItems.ContainsKey(itemID)) return;
            
            // Create GridItem from inventory data
            GridItem gridItem = CreateGridItemFromInventory(itemID, itemType);
            if (gridItem == null) return;
            
            // Find empty space for the item
            Vector2Int emptySpace = _gridSystem.FindEmptySpace(gridItem.CurrentWidth, gridItem.CurrentHeight);
            if (emptySpace.x >= 0 && emptySpace.y >= 0)
            {
                // Place item in grid
                if (_gridSystem.PlaceItem(gridItem, emptySpace))
                {
                    _inventoryItems[itemID] = gridItem;
                    Debug.Log($"InventoryPanel: Added item {gridItem.itemName} to grid at {emptySpace}");
                }
            }
            else
            {
                Debug.LogWarning($"InventoryPanel: No space for item {gridItem.itemName}");
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

        private GridItem CreateGridItemFromInventory(int itemID, ItemType itemType)
        {
            if (_playerInventory == null) return null;
            
            switch (itemType)
            {
                case ItemType.Weapon:
                    var weaponItem = _playerInventory.GetAllWeapons().FirstOrDefault(w => w.ItemID == itemID);
                    if (weaponItem != null)
                    {
                        return new GridItem(itemID, GetWeaponName(weaponItem), ItemType.Weapon, 
                                           weaponItem.GridWidth, weaponItem.GridHeight);
                    }
                    break;
                    
                case ItemType.Consumable:
                    var consumableItem = _playerInventory.GetAllConsumables().FirstOrDefault(c => c.ItemID == itemID);
                    if (consumableItem != null)
                    {
                        return new GridItem(itemID, $"Consumable_{itemID}", ItemType.Consumable, 1, 1);
                    }
                    break;
            }
            
            return null;
        }

        private string GetWeaponName(InventoryItem weaponItem)
        {
            if (weaponItem.CustomData.ContainsKey("weaponName"))
            {
                return weaponItem.CustomData["weaponName"].ToString();
            }
            return $"Weapon_{weaponItem.ItemID}";
        }

        private void SyncGridToInventory()
        {
            // This method would sync grid changes back to the player inventory
            // For now, we'll keep it simple and just log
            Debug.Log("InventoryPanel: Syncing grid changes to inventory");
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

        private void UpdateItemInfoPanel(GridItem item)
        {
            if (_itemInfoPanel == null) return;
            
            if (item != null)
            {
                // Create InfoDataAsset from GridItem
                var infoData = CreateInfoDataFromGridItem(item);
                _itemInfoPanel.DisplayInfoData(infoData);
            }
            else
            {
                _itemInfoPanel.DisplayInfoData(null);
            }
        }

        private InfoDataAsset CreateInfoDataFromGridItem(GridItem item)
        {
            // Create a temporary InfoDataAsset for display
            var infoData = ScriptableObject.CreateInstance<InfoDataAsset>();
            // infoData.infoName = item.itemName;
            // infoData.infoContent = $"Type: {item.itemType}\nSize: {item.CurrentWidth}x{item.CurrentHeight}\nID: {item.itemID}";
            // infoData.infoImage = item.itemIcon; // Uncomment when you have item icons
            
            return infoData;
        }

        #endregion

        #region Input Handling

        private void HandleInventoryInput()
        {
            if (_inputService == null || !_inputService.IsInventoryMode) return;
            
            // Handle item movement
            Vector2 moveInput = Vector2.zero;
            // This would be connected to the input service events
            
            // Handle item rotation
            // This would be connected to the input service events
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
            Debug.Log("InventoryPanel: Shown");
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