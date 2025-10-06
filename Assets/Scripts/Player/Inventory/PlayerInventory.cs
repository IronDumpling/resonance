using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Resonance.Items;
using Resonance.Player.Data;
using Resonance.Player.Core;

namespace Resonance.Player.Inventory
{
    /// <summary>
    /// Item type enum
    /// </summary>
    public enum ItemType
    {
        Consumable,    // Ammo, etc.
        Tool,          // Key, etc.
        Module,        // Wave Module
        Weapon         // Pistol, etc.
    }
    
    /// <summary>
    /// 格子单元数据 - 存储单个格子中的物品完整信息
    /// 这是纯数据结构，不包含任何业务逻辑
    /// </summary>
    [System.Serializable]
    public class GridCellData
    {
        // 基础信息
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public ItemType ItemType { get; set; }
        
        // 堆叠信息
        public int Quantity { get; set; }
        public int MaxStackSize { get; set; }
        
        // 空间信息
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public int Rotation { get; set; } // 0, 90, 180, 270
        public Vector2Int GridPosition { get; set; } // 起始位置
        
        // 装备状态
        public bool IsEquipped { get; set; }
        
        // 武器特有数据
        public int CurrentAmmo { get; set; }
        public string AmmoType { get; set; }
        public int MaxAmmo { get; set; }
        
        // 额外数据
        public string AssetPath { get; set; }
        public float Durability { get; set; }
        public Dictionary<string, object> CustomData { get; set; }
        
        public GridCellData()
        {
            CustomData = new Dictionary<string, object>();
            GridPosition = new Vector2Int(-1, -1);
            Quantity = 1;
            MaxStackSize = 1;
            Durability = 1f;
        }
        
        /// <summary>
        /// 计算当前宽度（考虑旋转）
        /// </summary>
        public int GetCurrentWidth()
        {
            return (Rotation == 90 || Rotation == 270) ? GridHeight : GridWidth;
        }
        
        /// <summary>
        /// 计算当前高度（考虑旋转）
        /// </summary>
        public int GetCurrentHeight()
        {
            return (Rotation == 90 || Rotation == 270) ? GridWidth : GridHeight;
        }
        
        /// <summary>
        /// 获取占用的所有格子位置
        /// </summary>
        public List<Vector2Int> GetOccupiedPositions()
        {
            var positions = new List<Vector2Int>();
            if (GridPosition.x < 0 || GridPosition.y < 0) return positions;
            
            int width = GetCurrentWidth();
            int height = GetCurrentHeight();
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    positions.Add(new Vector2Int(GridPosition.x + x, GridPosition.y + y));
                }
            }
            return positions;
        }
    }

    /// <summary>
    /// Extended item data structure
    /// Supports unified management of different types of items
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        // Basic properties
        public int ItemID { get; set; }
        public ItemType ItemType { get; set; }
        public int Quantity { get; set; }
        public float Durability { get; set; }
        
        // Grid system properties (reserved)
        public int GridWidth { get; set; }   // Item occupied grid width
        public int GridHeight { get; set; }  // Item occupied grid height
        public Vector2Int GridPosition { get; set; } // Position in the grid (-1,-1 means not placed)
        
        // Weapon specific properties
        public string AssetPath { get; set; }     // ScriptableObject resource path
        public int CurrentAmmo { get; set; }      // Current ammo count
        public Dictionary<string, object> CustomData { get; set; } // Custom data
        
        public InventoryItem(int itemID, ItemType itemType, int quantity = 1, float durability = 1f)
        {
            ItemID = itemID;
            ItemType = itemType;
            Quantity = quantity;
            Durability = durability;
            GridWidth = 1;
            GridHeight = 1;
            GridPosition = new Vector2Int(-1, -1); // Not placed state
            CustomData = new Dictionary<string, object>();
        }
    }

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
        private GridCellData[,] _gridCells;
        
        // Item quick lookup (itemID → item data)
        private Dictionary<int, GridCellData> _itemsById;
        
        // Occupancy mapping (each cell position → item ID occupying it)
        private Dictionary<Vector2Int, int> _cellOccupancy;

        // Legacy system compatibility (will be phased out)
        private List<InventoryItem> _items;
        private int _equippedWeaponID;
        private Dictionary<string, int> _ammoInventory;
        
        #endregion
        
        #region Event system - Data Change Events
        
        public System.Action OnInventoryChanged; // General change event
        
        // Grid operation events
        public System.Action<GridCellData, Vector2Int> OnItemAddedToGrid;
        public System.Action<GridCellData, Vector2Int> OnItemRemovedFromGrid;
        public System.Action<GridCellData, Vector2Int, Vector2Int> OnItemMovedInGrid; // item, oldPos, newPos
        public System.Action<GridCellData, int> OnItemRotatedInGrid; // item, newRotation
        public System.Action<GridCellData, int> OnItemQuantityChanged; // item, newQuantity
        
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
            _gridCells = new GridCellData[_gridWidth, _gridHeight];
            _itemsById = new Dictionary<int, GridCellData>();
            _cellOccupancy = new Dictionary<Vector2Int, int>();
            
            // Initialize legacy system
            _items = new List<InventoryItem>();
            _equippedWeaponID = -1;
            _ammoInventory = new Dictionary<string, int>();
            
            Debug.Log($"PlayerInventory: Initialized with {_gridWidth}x{_gridHeight} grid");
        }
        
        #endregion
        
        #region Core Grid Operations
        
        /// <summary>
        /// Add item to grid
        /// </summary>
        public bool AddItemToGrid(GridCellData itemData, Vector2Int position, int rotation = 0)
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
            
            // NEW: If this is ammo, trigger OnAmmoChanged for UI updates
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
            
            // If this was ammo, trigger OnAmmoChanged for UI updates
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
            
            if (newQuantity < 0 || newQuantity > itemData.MaxStackSize)
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
            
            // NEW: If this is ammo, trigger OnAmmoChanged for UI updates
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
        public GridCellData GetItemAtPosition(Vector2Int position)
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
        public GridCellData GetItemByID(int itemID)
        {
            return _itemsById.GetValueOrDefault(itemID);
        }
        
        /// <summary>
        /// Get all items
        /// </summary>
        public List<GridCellData> GetAllItems()
        {
            return new List<GridCellData>(_itemsById.Values);
        }
        
        /// <summary>
        /// Get all items by type
        /// </summary>
        public List<GridCellData> GetItemsByType(ItemType itemType)
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
        public bool CanPlaceItemAt(GridCellData itemData, Vector2Int position)
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
        private void OccupyGridCells(GridCellData itemData)
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
        private void ClearGridCells(GridCellData itemData)
        {
            var positions = itemData.GetOccupiedPositions();
            foreach (var pos in positions)
            {
                _cellOccupancy.Remove(pos);
            }
        }
        
        #endregion

        #region 武器管理 - Weapon Management (Deprecated - Use WeaponManager)
        
        /// <summary>
        /// Add weapon to inventory
        /// </summary>
        public bool AddWeapon(GunDataAsset weaponAsset)
        {
            if (weaponAsset == null) 
            {
                Debug.LogError("PlayerInventory: Cannot add null weapon");
                return false;
            }
            
            int weaponID = weaponAsset.GetInstanceID();
            Debug.Log($"🎒 [INVENTORY] AddWeapon called for {weaponAsset.weaponName} (ID: {weaponID})");
            
            // Check if already exists
            if (HasWeapon(weaponID))
            {
                Debug.LogWarning($"PlayerInventory: Weapon {weaponAsset.weaponName} already in inventory");
                return false;
            }
            
            // Check if inventory is full
            if (IsFull)
            {
                Debug.LogWarning("PlayerInventory: Cannot add weapon - inventory full");
                return false;
            }
            
            // Create weapon item
            var weaponItem = new InventoryItem(weaponID, ItemType.Weapon, 1, 1f)
            {
                AssetPath = GetAssetPath(weaponAsset),
                CurrentAmmo = weaponAsset.CurrentAmmo,
                GridWidth = weaponAsset.gridWidth,
                GridHeight = weaponAsset.gridHeight
            };
            
            // Weapon specific data
            weaponItem.CustomData["weaponName"] = weaponAsset.weaponName;
            weaponItem.CustomData["ammoType"] = weaponAsset.ammoType;
            weaponItem.CustomData["maxAmmo"] = weaponAsset.maxAmmo;
            weaponItem.CustomData["originalAsset"] = weaponAsset; // Save original reference
            
            Debug.Log($"PlayerInventory: Created weapon item: {weaponItem.ItemID}, AssetPath: {weaponItem.AssetPath}");
            
            _items.Add(weaponItem);
            OnInventoryChanged?.Invoke();
            
            Debug.Log($"PlayerInventory: Added weapon {weaponAsset.weaponName} to inventory");
            return true;
        }
        
        /// <summary>
        /// 装备武器（与WeaponManager同步）
        /// </summary>
        public bool EquipWeapon(int weaponID)
        {
            Debug.Log($"PlayerInventory: EquipWeapon called for weaponID: {weaponID}");
            
            // Check in new grid system first
            if (_itemsById.TryGetValue(weaponID, out var weaponData))
            {
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
                
                // Set IsEquipped flag in grid system
                weaponData.IsEquipped = true;
                
                OnWeaponEquipped?.Invoke(weaponID);
                Debug.Log($"PlayerInventory: Equipped weapon {weaponID} (IsEquipped flag set)");
                return true;
            }
            
            // Legacy system fallback
            if (!HasWeapon(weaponID))
            {
                Debug.LogWarning($"PlayerInventory: Cannot equip weapon {weaponID} - not in inventory");
                return false;
            }
            
            // Unequip current weapon
            if (_equippedWeaponID != -1)
            {
                Debug.Log($"PlayerInventory: Unequipping current weapon: {_equippedWeaponID}");
                UnequipCurrentWeapon();
            }
            
            _equippedWeaponID = weaponID;
            
            OnWeaponEquipped?.Invoke(weaponID);
            Debug.Log($"PlayerInventory: Equipped weapon {weaponID} (legacy system)");
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
        
        /// <summary>
        /// Get current equipped weapon
        /// </summary>
        public GunDataAsset GetEquippedWeapon()
        {
            if (_equippedWeaponID == -1)  return null;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
            }
            
            var weaponItem = _items.FirstOrDefault(item => 
                item.ItemID == _equippedWeaponID && item.ItemType == ItemType.Weapon);
            
            if (weaponItem == null) 
            {
                Debug.LogWarning($"PlayerInventory: Weapon not found! Looking for ID {_equippedWeaponID} with type Weapon");
                return null;
            }
            
            // 尝试从CustomData中获取原始GunDataAsset引用
            if (weaponItem.CustomData.ContainsKey("originalAsset") && weaponItem.CustomData["originalAsset"] is GunDataAsset originalAsset)
            {
                return originalAsset;
            }
            
            // 如果CustomData中没有，尝试从路径加载
            var gunAsset = LoadAssetFromPath<GunDataAsset>(weaponItem.AssetPath);
            
            return gunAsset;
        }
        
        /// <summary>
        /// 更新武器弹药数量（由WeaponManager调用）
        /// </summary>
        public void UpdateWeaponAmmo(int weaponID, int currentAmmo)
        {
            var weaponItem = _items.FirstOrDefault(item => 
                item.ItemID == weaponID && item.ItemType == ItemType.Weapon);
            
            if (weaponItem != null)
            {
                weaponItem.CurrentAmmo = currentAmmo;
                Debug.Log($"PlayerInventory: Updated weapon {weaponID} ammo to {currentAmmo}");
            }
        }
        
        public bool HasWeapon(int weaponID) => _items.Any(item => 
            item.ItemID == weaponID && item.ItemType == ItemType.Weapon);
        
        public int GetEquippedWeaponID() => _equippedWeaponID;
        
        /// <summary>
        /// 获取所有拥有的武器
        /// </summary>
        public List<InventoryItem> GetAllWeapons()
        {
            return _items.Where(item => item.ItemType == ItemType.Weapon).ToList();
        }
        
        #endregion

        #region 消耗品管理 - Consumable Management
        
        /// <summary>
        /// 添加消耗品到背包
        /// </summary>
        public bool AddConsumable(int itemID, int quantity = 1)
        {
            // 检查是否已存在相同类型的消耗品
            var existingItem = _items.FirstOrDefault(item => 
                item.ItemID == itemID && item.ItemType == ItemType.Consumable);
            
            if (existingItem != null)
            {
                // 堆叠现有物品
                existingItem.Quantity += quantity;
                Debug.Log($"PlayerInventory: Added {quantity} consumable {itemID} (stacked)");
            }
            else
            {
                // 检查背包空间
                if (IsFull)
                {
                    Debug.LogWarning("PlayerInventory: Cannot add consumable - inventory full");
                    return false;
                }

                // 添加新物品
                var newItem = new InventoryItem(itemID, ItemType.Consumable, quantity);
                _items.Add(newItem);
                Debug.Log($"PlayerInventory: Added new consumable {itemID} x{quantity}");
            }

            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// 移除消耗品
        /// </summary>
        public bool RemoveConsumable(int itemID, int quantity = 1)
        {
            var item = _items.FirstOrDefault(i => i.ItemID == itemID && i.ItemType == ItemType.Consumable);
            if (item == null)
            {
                Debug.LogWarning($"PlayerInventory: Cannot remove consumable {itemID} - not found");
                return false;
            }

            if (item.Quantity < quantity)
            {
                Debug.LogWarning($"PlayerInventory: Cannot remove {quantity} of consumable {itemID} - only have {item.Quantity}");
                return false;
            }

            item.Quantity -= quantity;
            if (item.Quantity <= 0)
            {
                _items.Remove(item);
            }

            OnInventoryChanged?.Invoke();
            Debug.Log($"PlayerInventory: Removed {quantity} of consumable {itemID}");
            return true;
        }
        
        /// <summary>
        /// 检查是否有指定数量的消耗品
        /// </summary>
        public bool HasConsumable(int itemID, int quantity = 1)
        {
            var item = _items.FirstOrDefault(i => i.ItemID == itemID && i.ItemType == ItemType.Consumable);
            return item != null && item.Quantity >= quantity;
        }
        
        /// <summary>
        /// 获取消耗品数量
        /// </summary>
        public int GetConsumableQuantity(int itemID)
        {
            var item = _items.FirstOrDefault(i => i.ItemID == itemID && i.ItemType == ItemType.Consumable);
            return item?.Quantity ?? 0;
        }
        
        /// <summary>
        /// 获取所有消耗品
        /// </summary>
        public List<InventoryItem> GetAllConsumables()
        {
            return _items.Where(item => item.ItemType == ItemType.Consumable).ToList();
        }

        #endregion

        #region 弹药管理 - Ammo Management (Legacy - Use ConsumableManager)
        
        // 弹药事件 (for backward compatibility)
        public System.Action<string, int> OnAmmoAdded; // ammoType, amount added
        public System.Action<string, int, int> OnAmmoChanged; // ammoType, oldAmount, newAmount
        
        /// <summary>
        /// 添加弹药到库存
        /// </summary>
        /// <param name="ammoType">弹药类型</param>
        /// <param name="amount">数量</param>
        /// <returns>是否成功添加</returns>
        public bool AddAmmo(string ammoType, int amount)
        {
            if (string.IsNullOrEmpty(ammoType) || amount <= 0)
            {
                Debug.LogWarning($"PlayerInventory: Invalid ammo parameters - type: {ammoType}, amount: {amount}");
                return false;
            }
            
            int oldAmount = _ammoInventory.GetValueOrDefault(ammoType, 0);
            int newAmount = oldAmount + amount;
            _ammoInventory[ammoType] = newAmount;
            
            Debug.Log($"PlayerInventory: Added {amount} {ammoType} ammo. Total: {newAmount}");
            
            // 触发事件
            OnAmmoAdded?.Invoke(ammoType, amount);
            OnAmmoChanged?.Invoke(ammoType, oldAmount, newAmount);
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        
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
        /// Grid-based Get Ammo Count
        /// </summary>
        /// <param name="ammoType">弹药类型</param>
        /// <returns>弹药数量</returns>
        public int GetAmmoCount(string ammoType)
        {
            if (string.IsNullOrEmpty(ammoType))
                return 0;
            
            // NEW: Get ammo count from grid-based system
            var consumableItems = GetItemsByType(ItemType.Consumable)
                .Where(item => item.CustomData.ContainsKey("ammoType") && 
                              item.CustomData["ammoType"].ToString() == ammoType);
            
            int totalCount = consumableItems.Sum(item => item.Quantity);
            
            Debug.Log($"PlayerInventory.GetAmmoCount: {ammoType} -> {totalCount} (from grid system)");
            return totalCount;
        }
        
        /// <summary>
        /// 设置弹药数量（用于测试或特殊情况）
        /// </summary>
        /// <param name="ammoType">弹药类型</param>
        /// <param name="count">新的数量</param>
        public void SetAmmoCount(string ammoType, int count)
        {
            if (string.IsNullOrEmpty(ammoType))
                return;
            
            int oldAmount = _ammoInventory.GetValueOrDefault(ammoType, 0);
            int newAmount = Mathf.Max(0, count);
            _ammoInventory[ammoType] = newAmount;
            
            Debug.Log($"PlayerInventory: Set {ammoType} ammo to {newAmount}");
            
            // 触发事件（如果数量有变化）
            if (oldAmount != newAmount)
            {
                OnAmmoChanged?.Invoke(ammoType, oldAmount, newAmount);
                OnInventoryChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 获取所有弹药类型和数量
        /// </summary>
        /// <returns>弹药字典的副本</returns>
        public Dictionary<string, int> GetAllAmmo()
        {
            return new Dictionary<string, int>(_ammoInventory);
        }
        
        /// <summary>
        /// 获取所有有库存的弹药类型
        /// </summary>
        /// <returns>弹药类型列表</returns>
        public List<string> GetAvailableAmmoTypes()
        {
            var types = new List<string>();
            
            foreach (var kvp in _ammoInventory)
            {
                if (kvp.Value > 0)
                {
                    types.Add(kvp.Key);
                }
            }
            
            return types;
        }
        
        /// <summary>
        /// 检查是否有任何弹药
        /// </summary>
        /// <returns>是否有弹药</returns>
        public bool HasAnyAmmo()
        {
            foreach (var kvp in _ammoInventory)
            {
                if (kvp.Value > 0)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取总弹药数量
        /// </summary>
        /// <returns>所有类型弹药的总数</returns>
        public int GetTotalAmmoCount()
        {
            int total = 0;
            foreach (var kvp in _ammoInventory)
            {
                total += kvp.Value;
            }
            
            return total;
        }
        
        /// <summary>
        /// 清空所有弹药（用于测试或特殊事件）
        /// </summary>
        public void ClearAllAmmo()
        {
            var oldAmmo = new Dictionary<string, int>(_ammoInventory);
            _ammoInventory.Clear();
            
            // 触发每种弹药的变化事件
            foreach (var kvp in oldAmmo)
            {
                if (kvp.Value > 0)
                {
                    OnAmmoChanged?.Invoke(kvp.Key, kvp.Value, 0);
                }
            }
            
            OnInventoryChanged?.Invoke();
            Debug.Log("PlayerInventory: All ammo cleared");
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
                    itemName = item.ItemName,
                    itemType = item.ItemType.ToString(),
                    quantity = item.Quantity,
                    maxStackSize = item.MaxStackSize,
                    gridWidth = item.GridWidth,
                    gridHeight = item.GridHeight,
                    rotation = item.Rotation,
                    gridPosition = item.GridPosition,
                    isEquipped = item.IsEquipped,
                    currentAmmo = item.CurrentAmmo,
                    ammoType = item.AmmoType,
                    maxAmmo = item.MaxAmmo,
                    assetPath = item.AssetPath,
                    durability = item.Durability,
                    customData = SerializableDictionary.FromDictionary(item.CustomData)
                };
                
                // Note: originalAsset (GunDataAsset) cannot be serialized in customData
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
                    
                    // Create GridCellData
                    var gridCellData = new GridCellData
                    {
                        ItemID = cellSaveData.itemID,
                        ItemName = cellSaveData.itemName,
                        ItemType = itemType,
                        Quantity = cellSaveData.quantity,
                        MaxStackSize = cellSaveData.maxStackSize,
                        GridWidth = cellSaveData.gridWidth,
                        GridHeight = cellSaveData.gridHeight,
                        Rotation = cellSaveData.rotation,
                        GridPosition = cellSaveData.gridPosition,
                        IsEquipped = cellSaveData.isEquipped,
                        CurrentAmmo = cellSaveData.currentAmmo,
                        AmmoType = cellSaveData.ammoType,
                        MaxAmmo = cellSaveData.maxAmmo,
                        AssetPath = cellSaveData.assetPath,
                        Durability = cellSaveData.durability,
                        CustomData = cellSaveData.customData.ToDictionary()
                    };
                    
                    // Add to grid (without triggering events during load)
                    bool added = AddItemToGrid(gridCellData, gridCellData.GridPosition, gridCellData.Rotation);
                    if (!added)
                    {
                        Debug.LogWarning($"PlayerInventory: Failed to load item {gridCellData.ItemName} at {gridCellData.GridPosition}");
                    }
                }
            }
            
            OnInventoryChanged?.Invoke();
            Debug.Log($"PlayerInventory: Loaded {_itemsById.Count} items to grid inventory");
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
