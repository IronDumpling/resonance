using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Resonance.Gameplay.Items;
using Resonance.Shared.Types;
using Resonance.Utilities.GridSystem;
using Resonance.Gameplay.Player.Data;
using Resonance.Gameplay.Player.Core;

namespace Resonance.Gameplay.Player.Inventory
{
    /// <summary>
    /// Unified player inventory system - pure data layer
    /// Store all items in a grid, without handling business logic
    /// </summary>
    public class PlayerInventory
    {
        #region Core data storage - Grid-based Storage
        
        // Grid size
        private int _gridWidth;
        private int _gridHeight;
        
        // Grid data storage (2D array, each cell may be occupied by a part of an item)
        private GridItem[,] _gridCells;
        
        // Item quick lookup (itemID → item data)
        private Dictionary<int, GridItem> _itemsById;
        
        // Occupancy mapping (each cell position → item ID occupying it)
        private Dictionary<Vector2Int, int> _cellOccupancy;

        // Equipped weapon tracking
        private int _equippedWeaponID;
        
        #endregion
        
        #region Event system - Data Change Events
        
        public System.Action OnInventoryChanged; // General change event
        
        // Grid operation events
        public System.Action<GridItem, Vector2Int> OnItemAddedToGrid;
        public System.Action<GridItem, Vector2Int> OnItemRemovedFromGrid;
        public System.Action<GridItem, Vector2Int, Vector2Int> OnItemMovedInGrid; // item, oldPos, newPos
        public System.Action<GridItem, int> OnItemRotatedInGrid; // item, newRotation
        public System.Action<GridItem, int> OnItemQuantityChanged; // item, newQuantity
        
        // Weapon-specific events (for WeaponManager)
        public System.Action<int> OnWeaponEquipped; // weaponID
        public System.Action OnWeaponUnequipped;
        
        #endregion
        
        #region Properties
        
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public int MaxSlots => _gridWidth * _gridHeight;
        public int UsedSlots => _itemsById.Count;
        public bool IsFull => UsedSlots >= MaxSlots; // Simplified judgment
        
        #endregion

        #region Initialization
        
        public PlayerInventory(int gridWidth, int gridHeight)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            
            // Initialize grid data
            _gridCells = new GridItem[_gridWidth, _gridHeight];
            _itemsById = new Dictionary<int, GridItem>();
            _cellOccupancy = new Dictionary<Vector2Int, int>();
            
            // Initialize equipped weapon tracking
            _equippedWeaponID = -1;
            
            Debug.Log($"PlayerInventory: Initialized with {_gridWidth}x{_gridHeight} grid");
        }
        
        #endregion
        
        #region Core Grid Operations
        
        /// <summary>
        /// Add item to grid
        /// </summary>
        public bool AddItemToGrid(GridItem itemData, Vector2Int position, int rotation = 0)
        {
            if (itemData == null)
            {
                Debug.LogError("PlayerInventory: Cannot add null item");
                return false;
            }
            
            itemData.GridPosition = position;
            itemData.Rotation = rotation;
            
            // Validate if can place
            if (!CanPlaceItemAt(itemData, position))
            {
                Debug.LogWarning($"PlayerInventory: Cannot place {itemData.ItemName} at {position}");
                return false;
            }
            
            // Occupy grid
            OccupyGridCells(itemData);
            
            // Add to dictionary
            _itemsById[itemData.ItemID] = itemData;
            
            // Trigger event
            OnItemAddedToGrid?.Invoke(itemData, position);
            OnInventoryChanged?.Invoke();
            
            // If this is ammo, trigger OnAmmoChanged for UI updates
            if (itemData.ItemType == ItemType.Consumable && itemData.CustomData.ContainsKey("ammoType"))
            {
                string ammoType = itemData.CustomData["ammoType"].ToString();
                int oldTotal = GetAmmoCount(ammoType) - itemData.Quantity; // Calculate old total
                int newTotal = GetAmmoCount(ammoType);
                OnAmmoChanged?.Invoke(ammoType, oldTotal, newTotal);
                Debug.Log($"PlayerInventory: Ammo added - {ammoType}: {oldTotal} -> {newTotal}");
            }
            
            Debug.Log($"PlayerInventory: Added {itemData.ItemName} to grid at {position}, rotation: {rotation}");
            return true; 
        }
        
        /// <summary>
        /// Remove item from grid
        /// </summary>
        public bool RemoveItemFromGrid(int itemID)
        {
            if (!_itemsById.TryGetValue(itemID, out var itemData))
            {
                Debug.LogWarning($"PlayerInventory: Item {itemID} not found");
                return false;
            }
            
            Vector2Int oldPosition = itemData.GridPosition;
            
            // Store ammo info before removing (for event trigger)
            bool isAmmo = itemData.ItemType == ItemType.Consumable && itemData.CustomData.ContainsKey("ammoType");
            string ammoType = null;
            int oldTotal = 0;
            int removedQuantity = itemData.Quantity;
            
            if (isAmmo)
            {
                ammoType = itemData.CustomData["ammoType"].ToString();
                oldTotal = GetAmmoCount(ammoType);
            }
            
            // Clear occupancy
            ClearGridCells(itemData);
            
            // Remove from dictionary
            _itemsById.Remove(itemID);
            
            // Trigger event
            OnItemRemovedFromGrid?.Invoke(itemData, oldPosition);
            OnInventoryChanged?.Invoke();
            
            // If this is ammo, trigger OnAmmoChanged for UI updates
            if (isAmmo)
            {
                int newTotal = GetAmmoCount(ammoType);
                OnAmmoChanged?.Invoke(ammoType, oldTotal, newTotal);
                Debug.Log($"PlayerInventory: Ammo removed - {ammoType}: {oldTotal} -> {newTotal}");
            }
            
            Debug.Log($"PlayerInventory: Removed {itemData.ItemName} from grid");
            return true; 
        }
        
        /// <summary>
        /// Move item in grid
        /// </summary>
        public bool MoveItemInGrid(int itemID, Vector2Int newPosition)
        {
            if (!_itemsById.TryGetValue(itemID, out var itemData))
            {
                Debug.LogWarning($"PlayerInventory: Item {itemID} not found");
                return false;
            }
            
            Vector2Int oldPosition = itemData.GridPosition;
            
            // Temporarily clear occupancy
            ClearGridCells(itemData);
            
            // Update position
            itemData.GridPosition = newPosition;
            
            // Validate new position
            if (!CanPlaceItemAt(itemData, newPosition))
            {
                // Restore old position
                itemData.GridPosition = oldPosition;
                OccupyGridCells(itemData);
                Debug.LogWarning($"PlayerInventory: Cannot move {itemData.ItemName} to {newPosition}");
                return false;
            }
            
            // Occupy new position
            OccupyGridCells(itemData);
            
            // Trigger event
            OnItemMovedInGrid?.Invoke(itemData, oldPosition, newPosition);
            OnInventoryChanged?.Invoke();
            
            Debug.Log($"PlayerInventory: Moved {itemData.ItemName} from {oldPosition} to {newPosition}");
            return true;
        }
        
        /// <summary>
        /// Rotate item in grid
        /// </summary>
        public bool RotateItemInGrid(int itemID)
        {
            if (!_itemsById.TryGetValue(itemID, out var itemData))
            {
                Debug.LogWarning($"PlayerInventory: Item {itemID} not found");
                return false;
            }
            
            int oldRotation = itemData.Rotation;
            int newRotation = (oldRotation + 90) % 360;
            
            // Temporarily clear occupancy
            ClearGridCells(itemData);
            
            // Update rotation
            itemData.Rotation = newRotation;
            
            // 验证旋转后是否可以放置
            if (!CanPlaceItemAt(itemData, itemData.GridPosition))
            {
                // 恢复旧旋转
                itemData.Rotation = oldRotation;
                OccupyGridCells(itemData);
                Debug.LogWarning($"PlayerInventory: Cannot rotate {itemData.ItemName} at {itemData.GridPosition}");
                return false;
            }
            
            // Occupy new position
            OccupyGridCells(itemData);
            
            // Trigger event
            OnItemRotatedInGrid?.Invoke(itemData, newRotation);
            OnInventoryChanged?.Invoke();
            
            Debug.Log($"PlayerInventory: Rotated {itemData.ItemName} from {oldRotation}° to {newRotation}°");
            return true;
        }
        
        /// <summary>
        /// Update item quantity
        /// </summary>
        public bool UpdateItemQuantity(int itemID, int newQuantity)
        {
            if (!_itemsById.TryGetValue(itemID, out var itemData))
            {
                Debug.LogWarning($"PlayerInventory: Item {itemID} not found");
                return false;
            }
            
            if (newQuantity < 0 || newQuantity > itemData.MaxStackQuantity)
            {
                Debug.LogWarning($"PlayerInventory: Invalid quantity {newQuantity} for {itemData.ItemName}");
                return false;
            }
            
            int oldQuantity = itemData.Quantity;
            itemData.Quantity = newQuantity;
            
            // If quantity is zero, remove item
            if (newQuantity == 0)
            {
                return RemoveItemFromGrid(itemID);
            }
            
            // Trigger event
            OnItemQuantityChanged?.Invoke(itemData, newQuantity);
            OnInventoryChanged?.Invoke();
            
            // If this is ammo, trigger OnAmmoChanged for UI updates
            if (itemData.ItemType == ItemType.Consumable && itemData.CustomData.ContainsKey("ammoType"))
            {
                string ammoType = itemData.CustomData["ammoType"].ToString();
                int oldTotal = GetAmmoCount(ammoType) - newQuantity + oldQuantity; // Calculate old total
                int newTotal = GetAmmoCount(ammoType);
                OnAmmoChanged?.Invoke(ammoType, oldTotal, newTotal);
                Debug.Log($"PlayerInventory: Ammo changed - {ammoType}: {oldTotal} -> {newTotal}");
            }
            
            Debug.Log($"PlayerInventory: Updated {itemData.ItemName} quantity to {newQuantity}");
            return true;
        }
        
        #endregion
        
        #region Query Methods
        
        /// <summary>
        /// Get item at position
        /// </summary>
        public GridItem GetItemAtPosition(Vector2Int position)
        {
            if (!IsValidPosition(position)) return null;
            
            if (_cellOccupancy.TryGetValue(position, out int itemID))
            {
                return _itemsById.GetValueOrDefault(itemID);
            }
            
            return null;
        }
        
        /// <summary>
        /// Get item by ID
        /// </summary>
        public GridItem GetItemByID(int itemID)
        {
            return _itemsById.GetValueOrDefault(itemID);
        }
        
        /// <summary>
        /// Get all items
        /// </summary>
        public List<GridItem> GetAllItems()
        {
            return new List<GridItem>(_itemsById.Values);
        }
        
        /// <summary>
        /// Get all items by type
        /// </summary>
        public List<GridItem> GetItemsByType(ItemType itemType)
        {
            return _itemsById.Values.Where(item => item.ItemType == itemType).ToList();
        }
        
        /// <summary>
        /// Check if has item
        /// </summary>
        public bool HasItem(int itemID)
        {
            return _itemsById.ContainsKey(itemID);
        }
        
        /// <summary>
        /// Find empty space
        /// </summary>
        public Vector2Int FindEmptySpace(int width, int height) 
        { 
            for (int y = 0; y <= _gridHeight - height; y++)
            {
                for (int x = 0; x <= _gridWidth - width; x++)
                {
                    Vector2Int testPos = new Vector2Int(x, y);
                    bool canPlace = true;
                    
                    for (int dx = 0; dx < width; dx++)
                    {
                        for (int dy = 0; dy < height; dy++)
                        {
                            Vector2Int checkPos = new Vector2Int(x + dx, y + dy);
                            if (_cellOccupancy.ContainsKey(checkPos))
                            {
                                canPlace = false;
                                break;
                            }
                        }
                        if (!canPlace) break;
                    }
                    
                    if (canPlace)
                    {
                        return testPos;
                    }
                }
            }
            
            return new Vector2Int(-1, -1); // No empty space
        }
        
        #endregion

        #region Validation Methods
        
        /// <summary>
        /// Validate if can place item
        /// </summary>
        public bool CanPlaceItemAt(GridItem itemData, Vector2Int position)
        {
            if (itemData == null) return false;
            
            int width = itemData.GetCurrentWidth();
            int height = itemData.GetCurrentHeight();
            
            // Check if out of bounds
            if (!IsWithinBounds(position.x, position.y, width, height))
            {
                return false;
            }
            
            // Check if occupied
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int checkPos = new Vector2Int(position.x + x, position.y + y);
                    
                    // If the position is occupied, and not occupied by itself, then cannot place
                    if (_cellOccupancy.TryGetValue(checkPos, out int occupyingItemID))
                    {
                        if (occupyingItemID != itemData.ItemID)
                        {
                            return false;
                        }
                    }
                }
            }
            
            return true;
        }
        
        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < _gridWidth &&
                   position.y >= 0 && position.y < _gridHeight;
        }
        
        private bool IsWithinBounds(int x, int y, int width, int height)
        {
            return x >= 0 && x + width <= _gridWidth &&
                   y >= 0 && y + height <= _gridHeight;
        }
        
        #endregion
        
        #region Internal Helper Methods
        
        /// <summary>
        /// Occupy grid cells
        /// </summary>
        private void OccupyGridCells(GridItem itemData)
        {
            var positions = itemData.GetOccupiedPositions();
            foreach (var pos in positions)
            {
                _cellOccupancy[pos] = itemData.ItemID;
            }
        }
        
        /// <summary>
        /// Clear grid cells occupancy
        /// </summary>
        private void ClearGridCells(GridItem itemData)
        {
            var positions = itemData.GetOccupiedPositions();
            foreach (var pos in positions)
            {
                _cellOccupancy.Remove(pos);
            }
        }
        
        #endregion

        #region Weapon Equipment Status
        
        /// <summary>
        /// Equip weapon (sync with WeaponManager)
        /// </summary>
        public bool EquipWeapon(int weaponID)
        {
            Debug.Log($"PlayerInventory: EquipWeapon called for weaponID: {weaponID}");
            
            if (!_itemsById.TryGetValue(weaponID, out var weaponData))
            {
                Debug.LogWarning($"PlayerInventory: Cannot equip weapon {weaponID} - not in inventory");
                return false;
            }
            
            if (weaponData.ItemType != ItemType.Weapon)
            {
                Debug.LogWarning($"PlayerInventory: Item {weaponID} is not a weapon");
                return false;
            }
            
            // Unequip current weapon
            if (_equippedWeaponID != -1 && _equippedWeaponID != weaponID)
            {
                Debug.Log($"PlayerInventory: Unequipping current weapon: {_equippedWeaponID}");
                UnequipCurrentWeapon();
            }
            
            _equippedWeaponID = weaponID;
            weaponData.IsEquipped = true;
            
            OnWeaponEquipped?.Invoke(weaponID);
            Debug.Log($"PlayerInventory: Equipped weapon {weaponID}");
            return true;
        }
        
        /// <summary>
        /// Unequip current weapon
        /// </summary>
        public void UnequipCurrentWeapon()
        {
            if (_equippedWeaponID == -1) return;
            
            int oldWeaponID = _equippedWeaponID;
            
            // Clear IsEquipped flag in grid system
            if (_itemsById.TryGetValue(oldWeaponID, out var weaponData))
            {
                weaponData.IsEquipped = false;
                Debug.Log($"PlayerInventory: Cleared IsEquipped flag for weapon {oldWeaponID}");
            }
            
            _equippedWeaponID = -1;
            
            OnWeaponUnequipped?.Invoke();
            Debug.Log("PlayerInventory: Unequipped current weapon");
        }
        
        public int GetEquippedWeaponID() => _equippedWeaponID;
        
        #endregion

        #region 弹药管理 - Ammo Management
        
        // Ammo events
        public System.Action<string, int, int> OnAmmoChanged; // ammoType, oldAmount, newAmount
        
        /// <summary>
        /// 消耗弹药
        /// </summary>
        /// <param name="ammoType">弹药类型</param>
        /// <param name="amount">消耗数量</param>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeAmmo(string ammoType, int amount)
        {
            if (string.IsNullOrEmpty(ammoType) || amount <= 0)
                return false;
            
            int oldAmount = GetAmmoCount(ammoType);
            if (oldAmount < amount)
            {
                Debug.LogWarning($"PlayerInventory: Not enough {ammoType} ammo - need {amount}, have {oldAmount}");
                return false;
            }
                
            // Find and consume ammo from grid system
            var ammoItems = GetItemsByType(ItemType.Consumable)
                .Where(item => item.CustomData.ContainsKey("ammoType") && 
                              item.CustomData["ammoType"].ToString() == ammoType)
                .OrderBy(item => item.Quantity) // Consume from smallest stacks first
                .ToList();
            
            int remainingToConsume = amount;
            
            foreach (var ammoItem in ammoItems)
            {
                if (remainingToConsume <= 0) break;
                
                int consumeFromThis = Mathf.Min(remainingToConsume, ammoItem.Quantity);
                int newQuantity = ammoItem.Quantity - consumeFromThis;
                
                // UpdateItemQuantity will auto-remove if quantity reaches 0
                UpdateItemQuantity(ammoItem.ItemID, newQuantity);
                remainingToConsume -= consumeFromThis;
                
                Debug.Log($"PlayerInventory: Consumed {consumeFromThis} {ammoType} from stack. New quantity: {newQuantity}");
            }
            
            int newAmount = GetAmmoCount(ammoType);
            Debug.Log($"PlayerInventory: Consumed {amount} {ammoType} ammo. Remaining: {newAmount}");
            
            // OnAmmoChanged is already triggered by UpdateItemQuantity
            return true;
        }
        
        /// <summary>
        /// Grid-base Has Ammo
        /// </summary>
        /// <param name="ammoType">弹药类型</param>
        /// <param name="amount">需要的数量</param>
        /// <returns>是否有足够弹药</returns>
        public bool HasAmmo(string ammoType, int amount = 1)
        {
            if (string.IsNullOrEmpty(ammoType) || amount <= 0)
                return false;
                
            // NEW: Check ammo from grid-based system
            return GetAmmoCount(ammoType) >= amount;
        }
        
        /// <summary>
        /// Get ammo count from grid system
        /// </summary>
        public int GetAmmoCount(string ammoType)
        {
            if (string.IsNullOrEmpty(ammoType))
                return 0;
            
            var consumableItems = GetItemsByType(ItemType.Consumable)
                .Where(item => item.CustomData.ContainsKey("ammoType") && 
                              item.CustomData["ammoType"].ToString() == ammoType);
            
            int totalCount = consumableItems.Sum(item => item.Quantity);
            
            Debug.Log($"PlayerInventory.GetAmmoCount: {ammoType} -> {totalCount} (from grid system)");
            return totalCount;
        }

        #endregion

        #region Save/Load System - Grid Based

        /// <summary>
        /// Get save data for grid-based inventory
        /// </summary>
        public GridInventorySaveData GetSaveData()
        {
            var saveData = new GridInventorySaveData
            {
                gridWidth = _gridWidth,
                gridHeight = _gridHeight,
                items = new List<GridCellSaveData>()
            };
            
            foreach (var item in _itemsById.Values)
            {
                var cellSaveData = new GridCellSaveData
                {
                    itemID = item.ItemID,
                    ItemName = item.ItemName,
                    itemType = item.ItemType.ToString(),
                    consumableType = item.ConsumableType.ToString(),
                    quantity = item.Quantity,
                    maxStackQuantity = item.MaxStackQuantity,
                    gridWidth = item.GridWidth,
                    gridHeight = item.GridHeight,
                    rotation = item.Rotation,
                    gridPosition = item.GridPosition,
                    isEquipped = item.IsEquipped,
                    assetPath = item.AssetPath,
                    durability = item.Durability,
                    customData = SerializableDictionary.FromDictionary(item.CustomData)
                };
                
                // Note: originalAsset (WeaponDataAsset) cannot be serialized in customData
                // It will need to be reloaded from Resources using AssetPath or name
                
                saveData.items.Add(cellSaveData);
            }
            
            Debug.Log($"PlayerInventory: Saved {saveData.items.Count} items from grid inventory");
            return saveData;
        }

        /// <summary>
        /// Load from save data for grid-based inventory
        /// </summary>
        public void LoadFromSaveData(GridInventorySaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("PlayerInventory: Cannot load from null save data");
                return;
            }
            
            // Clear current grid
            _itemsById.Clear();
            _cellOccupancy.Clear();
            
            // Verify grid size matches
            if (saveData.gridWidth != _gridWidth || saveData.gridHeight != _gridHeight)
            {
                Debug.LogWarning($"PlayerInventory: Grid size mismatch! Save: {saveData.gridWidth}x{saveData.gridHeight}, Current: {_gridWidth}x{_gridHeight}");
                // Could resize grid here if needed
            }
            
            // Load all items
            if (saveData.items != null)
            {
                foreach (var cellSaveData in saveData.items)
                {
                    // Parse ItemType from string
                    if (!System.Enum.TryParse<ItemType>(cellSaveData.itemType, out var itemType))
                    {
                        Debug.LogWarning($"PlayerInventory: Invalid item type '{cellSaveData.itemType}' for item {cellSaveData.itemID}");
                        continue;
                    }
                    
                    if (!System.Enum.TryParse<ConsumableType>(cellSaveData.consumableType, out var consumableType))
                    {
                        Debug.LogWarning($"PlayerInventory: Invalid consumable type '{cellSaveData.consumableType}' for item {cellSaveData.itemID}");
                        continue;
                    }
                    
                    // Create GridItem
                    var gridItem = new GridItem
                    {
                        ItemID = cellSaveData.itemID,
                        ItemName = cellSaveData.ItemName,
                        ItemType = itemType,
                        ConsumableType = consumableType,
                        Quantity = cellSaveData.quantity,
                        MaxStackQuantity = cellSaveData.maxStackQuantity,
                        GridWidth = cellSaveData.gridWidth,
                        GridHeight = cellSaveData.gridHeight,
                        Rotation = cellSaveData.rotation,
                        GridPosition = cellSaveData.gridPosition,
                        IsEquipped = cellSaveData.isEquipped,
                        AssetPath = cellSaveData.assetPath,
                        Durability = cellSaveData.durability,
                        CustomData = cellSaveData.customData.ToDictionary()
                    };
                    
                    // Reload ItemPrefab and ItemIcon from AssetPath
                    LoadVisualDataFromAssetPath(gridItem, cellSaveData.assetPath);
                    
                    // Add to grid (without triggering events during load)
                    bool added = AddItemToGrid(gridItem, gridItem.GridPosition, gridItem.Rotation);
                    if (!added)
                    {
                        Debug.LogWarning($"PlayerInventory: Failed to load item {gridItem.ItemName} at {gridItem.GridPosition}");
                    }
                }
            }
            
            OnInventoryChanged?.Invoke();
            Debug.Log($"PlayerInventory: Loaded {_itemsById.Count} items to grid inventory");
        }
        
        /// <summary>
        /// Load ItemPrefab and ItemIcon from AssetPath
        /// </summary>
        private void LoadVisualDataFromAssetPath(GridItem gridItem, string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning($"PlayerInventory: No AssetPath for {gridItem.ItemName}. Visual data cannot be loaded.");
                return;
            }
            
            Debug.Log($"PlayerInventory: Loading visual data from AssetPath: {assetPath}");
            
            // Convert Unity asset path to Resources path
            string resourcesPath = ConvertToResourcesPath(assetPath);
            
            if (string.IsNullOrEmpty(resourcesPath))
            {
                Debug.LogWarning($"PlayerInventory: Failed to convert AssetPath to Resources path: {assetPath}");
                return;
            }
            
            Debug.Log($"PlayerInventory: Resources path: {resourcesPath}");


            switch (gridItem.ItemType)
            {
                case ItemType.Weapon:
                    // Load WeaponDataAsset
                    var gunData = Resources.Load<WeaponDataAsset>(resourcesPath);
                    if (gunData != null)
                    {
                        gridItem.ItemPrefab = gunData.itemPrefab;
                        gridItem.ItemIcon = gunData.weaponIcon;
                        Debug.Log($"PlayerInventory: Loaded weapon visual data - ItemPrefab={(gunData.itemPrefab != null ? gunData.itemPrefab.name : "NULL")},"+
                                $"ItemIcon={(gunData.weaponIcon != null ? gunData.weaponIcon.name : "NULL")}");
                    }
                    else
                    {
                        Debug.LogWarning($"PlayerInventory: Failed to load WeaponDataAsset from Resources path: {resourcesPath}");
                    }
                    break;
                case ItemType.Consumable:
                    switch (gridItem.ConsumableType)
                    {
                        case ConsumableType.EnergyBottle:
                            break;
                        case ConsumableType.Healant:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
        
        /// <summary>
        /// Convert Unity asset path to Resources.Load compatible path
        /// From: "Assets/Resources/Data/Items/PistoData.asset"
        /// To: "Data/Items/PistoData"
        /// </summary>
        private string ConvertToResourcesPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return "";
            
            // Remove "Assets/Resources/" prefix
            const string resourcesPrefix = "Assets/Resources/";
            if (assetPath.StartsWith(resourcesPrefix))
            {
                assetPath = assetPath.Substring(resourcesPrefix.Length);
            }
            
            // Remove file extension
            int lastDotIndex = assetPath.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                assetPath = assetPath.Substring(0, lastDotIndex);
            }
            
            return assetPath;
        }

        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get the resource path of the ScriptableObject
        /// </summary>
        private string GetAssetPath(ScriptableObject asset)
        {
            if (asset == null) return "";
            
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(asset);
            #else
            return $"Assets/Resources/Data/Items/{asset.name}Data.asset";
            #endif
        }
        
        /// <summary>
        /// load ScriptableObject from path
        /// </summary>
        private T LoadAssetFromPath<T>(string path) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(path)) 
            {
                Debug.LogWarning($"PlayerInventory: LoadAssetFromPath: path is null or empty");
                return null;
            }
            
            Debug.Log($"PlayerInventory: LoadAssetFromPath: Loading from path: {path}");
            
            #if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Debug.Log($"PlayerInventory: LoadAssetFromPath: Editor result: {asset?.name ?? "NULL"}");
            return asset;
            #else
            // Load from Resources at runtime, need to remove extension and Resources path
            string resourcePath = path;
            if (path.StartsWith("Assets/Resources/"))
            {
                resourcePath = path.Substring("Assets/Resources/".Length);
            }
            if (resourcePath.EndsWith(".asset"))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - ".asset".Length);
            }
            
            Debug.Log($"PlayerInventory: LoadAssetFromPath: Resource path: {resourcePath}");
            var resource = Resources.Load<T>(resourcePath);
            Debug.Log($"PlayerInventory: LoadAssetFromPath: Resource result: {resource?.name ?? "NULL"}");
            return resource;
            #endif
        }
        
        #endregion
    }
}
