using UnityEngine;
using Resonance.Gameplay.Player.Inventory;
using Resonance.Gameplay.Player.Inventory.Operations;
using Resonance.Shared.Interfaces.Operations;
using Resonance.Shared.Types;
using Resonance.Systems.GridSystem;
using System.Collections.Generic;

namespace Resonance.Gameplay.Player.Inventory
{
    /// <summary>
    /// InventoryOperationManager - Unified manager for both grid and item operations
    /// Responsibilities: 
    /// 1. Grid operations: move, rotate, stack items
    /// 2. Item operations: use, drop, combine items (delegated to handlers)
    /// </summary>
    public class InventoryOperationManager
    {
        private PlayerInventory _inventory;
        private ConsumableManager _consumableManager;
        private WaveOutputManager _waveOutputManager;
        
        // Operation Handlers (Strategy Pattern)
        private Dictionary<ItemType, BaseItemOperationHandler> _operationHandlers;
        
        // Events - Grid Operations
        public System.Action<int, Vector2Int, Vector2Int> OnItemMoved; // itemID, oldPos, newPos
        public System.Action<int, int> OnItemRotated; // itemID, newRotation
        public System.Action<int, int> OnItemsStacked; // sourceID, targetID
        
        // Events - Item Operations
        public System.Action<GridItem> OnItemUsed;
        public System.Action<GridItem> OnItemDropped;
        public System.Action<GridItem, GridItem> OnItemsCombined;
        
        public InventoryOperationManager(
            PlayerInventory inventory, 
            WaveOutputManager waveOutputManager,
            ConsumableManager consumableManager)
        {
            _inventory = inventory;
            _waveOutputManager = waveOutputManager;
            _consumableManager = consumableManager;
            
            InitializeOperationHandlers();
            
            Debug.Log("InventoryOperationManager: Initialized with operation handlers");
        }
        
        /// <summary>
        /// Initialize operation handlers for different item types
        /// </summary>
        private void InitializeOperationHandlers()
        {
            _operationHandlers = new Dictionary<ItemType, BaseItemOperationHandler>();
            
            // Register handlers for each item type
            _operationHandlers[ItemType.WaveOutput] = new WaveOutputOperationHandler(
                _inventory, _waveOutputManager, _consumableManager);
            
            _operationHandlers[ItemType.Consumable] = new AmmoOperationHandler(
                _inventory, _waveOutputManager, _consumableManager);
            
            // TODO Future handlers can be added here:
            // _operationHandlers[ItemType.Tool] = new ToolOperationHandler(...);
            // _operationHandlers[ItemType.Module] = new ModuleOperationHandler(...);
            
            Debug.Log($"InventoryOperationManager: Registered {_operationHandlers.Count} operation handlers");
        }
        
        /// <summary>
        /// Get operation handler for specific item type
        /// </summary>
        private BaseItemOperationHandler GetHandler(ItemType itemType)
        {
            if (_operationHandlers.TryGetValue(itemType, out var handler))
            {
                return handler;
            }
            
            Debug.LogWarning($"InventoryOperationManager: No handler found for ItemType {itemType}");
            return null;
        }
        
        #region Move Operations
        
        /// <summary>
        /// Move item to new position
        /// </summary>
        public bool MoveItem(int itemID, Vector2Int newPosition)
        {
            var item = _inventory.GetItemByID(itemID);
            if (item == null)
            {
                Debug.LogWarning($"InventoryOperationManager: Item {itemID} not found");
                return false;
            }
            
            Vector2Int oldPosition = item.GridPosition;
            
            // Validate and move
            if (_inventory.MoveItemInGrid(itemID, newPosition))
            {
                OnItemMoved?.Invoke(itemID, oldPosition, newPosition);
                Debug.Log($"InventoryOperationManager: Moved item {item.ItemName} from {oldPosition} to {newPosition}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if can move to specified position
        /// </summary>
        public bool CanMoveItemTo(int itemID, Vector2Int newPosition)
        {
            var item = _inventory.GetItemByID(itemID);
            if (item == null) return false;
            
            return _inventory.CanPlaceItemAt(item, newPosition);
        }
        
        #endregion
        
        #region Rotate Operations
        
        /// <summary>
        /// Rotate item
        /// </summary>
        public bool RotateItem(int itemID)
        {
            var item = _inventory.GetItemByID(itemID);
            if (item == null)
            {
                Debug.LogWarning($"InventoryOperationManager: Item {itemID} not found");
                return false;
            }
            
            int oldRotation = item.Rotation;
            
            // Validate and rotate
            if (_inventory.RotateItemInGrid(itemID))
            {
                OnItemRotated?.Invoke(itemID, item.Rotation);
                Debug.Log($"InventoryOperationManager: Rotated item {item.ItemName} from {oldRotation}° to {item.Rotation}°");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if can rotate
        /// </summary>
        public bool CanRotateItem(int itemID)
        {
            var item = _inventory.GetItemByID(itemID);
            if (item == null) return false;
            
            // If it's a square item (width = height), rotation is meaningless but allowed
            if (item.GridWidth == item.GridHeight) return true;
            
            // Check if can place after simulating rotation
            int oldRotation = item.Rotation;
            item.Rotation = (oldRotation + 90) % 360;
            bool canPlace = _inventory.CanPlaceItemAt(item, item.GridPosition);
            item.Rotation = oldRotation; // Restore
            
            return canPlace;
        }
        
        #endregion
        
        #region Stack Operations
        
        /// <summary>
        /// Stack items (drag source item to target item)
        /// </summary>
        public bool StackItems(int sourceItemID, int targetItemID)
        {
            var sourceItem = _inventory.GetItemByID(sourceItemID);
            var targetItem = _inventory.GetItemByID(targetItemID);
            
            if (sourceItem == null || targetItem == null)
            {
                Debug.LogWarning("InventoryOperationManager: Source or target item not found");
                return false;
            }
            
            // Check if can stack
            if (!_consumableManager.CanStackItems(sourceItem, targetItem))
            {
                Debug.LogWarning($"InventoryOperationManager: Cannot stack {sourceItem.ItemName} with {targetItem.ItemName}");
                return false;
            }
            
            // Execute stacking
            if (_consumableManager.TryStackItems(sourceItemID, targetItemID))
            {
                OnItemsStacked?.Invoke(sourceItemID, targetItemID);
                Debug.Log($"InventoryOperationManager: Stacked {sourceItem.ItemName} onto {targetItem.ItemName}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if two items can be stacked
        /// </summary>
        public bool CanStackItems(int sourceItemID, int targetItemID)
        {
            var sourceItem = _inventory.GetItemByID(sourceItemID);
            var targetItem = _inventory.GetItemByID(targetItemID);
            
            if (sourceItem == null || targetItem == null) return false;
            
            return _consumableManager.CanStackItems(sourceItem, targetItem);
        }
        
        #endregion
        
        #region Drag & Drop Logic
        
        /// <summary>
        /// Handle drag end
        /// Determine if it's a move, stack, or cancel
        /// </summary>
        public bool HandleDrop(int draggedItemID, Vector2Int dropPosition)
        {
            // Check if target position has item
            var itemAtDropPos = _inventory.GetItemAtPosition(dropPosition);
            
            if (itemAtDropPos != null && itemAtDropPos.ItemID != draggedItemID)
            {
                // Try to stack
                if (CanStackItems(draggedItemID, itemAtDropPos.ItemID))
                {
                    return StackItems(draggedItemID, itemAtDropPos.ItemID);
                }
                else
                {
                    Debug.LogWarning("InventoryOperationManager: Cannot drop - position occupied");
                    return false;
                }
            }
            else
            {
                // Move to empty position
                return MoveItem(draggedItemID, dropPosition);
            }
        }
        
        /// <summary>
        /// Handle drag preview (check if can place)
        /// </summary>
        public bool CanDrop(int draggedItemID, Vector2Int dropPosition)
        {
            var draggedItem = _inventory.GetItemByID(draggedItemID);
            if (draggedItem == null) return false;
            
            // Check target position
            var itemAtDropPos = _inventory.GetItemAtPosition(dropPosition);
            
            if (itemAtDropPos != null && itemAtDropPos.ItemID != draggedItemID)
            {
                // Check if can stack
                return CanStackItems(draggedItemID, itemAtDropPos.ItemID);
            }
            else
            {
                // Check if can move to empty position
                return CanMoveItemTo(draggedItemID, dropPosition);
            }
        }
        
        #endregion
        
        #region Item Operations - Use/Drop/Combine
        
        // ==================== USE Operation ====================
        
        /// <summary>
        /// Check if item can be used
        /// </summary>
        public bool CanUse(GridItem item)
        {
            if (item == null) return false;
            
            var handler = GetHandler(item.ItemType);
            if (handler is IItemUsable usable)
            {
                return usable.CanUse(item);
            }
            
            return false;
        }
        
        /// <summary>
        /// Use item (equip weapon, activate tool, etc.)
        /// </summary>
        public void UseItem(GridItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("InventoryOperationManager: Cannot use null item");
                return;
            }
            
            var handler = GetHandler(item.ItemType);
            if (handler is IItemUsable usable)
            {
                if (usable.CanUse(item))
                {
                    usable.Use(item);
                    OnItemUsed?.Invoke(item);
                    Debug.Log($"InventoryOperationManager: Used item {item.ItemName}");
                }
                else
                {
                    Debug.LogWarning($"InventoryOperationManager: Cannot use item {item.ItemName}");
                }
            }
            else
            {
                Debug.LogWarning($"InventoryOperationManager: Item {item.ItemName} is not usable");
            }
        }
        
        /// <summary>
        /// Get use button text for UI display
        /// </summary>
        public string GetUseButtonText(GridItem item)
        {
            if (item == null) return "Use";
            
            var handler = GetHandler(item.ItemType);
            if (handler is IItemUsable usable)
            {
                return usable.GetUseButtonText(item);
            }
            
            return "Use";
        }

        // ==================== COMBINE Operation ====================
        
        /// <summary>
        /// Check if two items can be combined
        /// </summary>
        public bool CanCombine(GridItem sourceItem, GridItem targetItem)
        {
            if (sourceItem == null || targetItem == null) return false;
            
            var handler = GetHandler(sourceItem.ItemType);
            if (handler is IItemCombinable combinable)
            {
                return combinable.CanCombine(sourceItem, targetItem);
            }
            
            return false;
        }
        
        /// <summary>
        /// Combine two items (stack, merge, etc.)
        /// </summary>
        public void CombineItems(GridItem sourceItem, GridItem targetItem)
        {
            if (sourceItem == null || targetItem == null)
            {
                Debug.LogWarning("InventoryOperationManager: Cannot combine null items");
                return;
            }
            
            var handler = GetHandler(sourceItem.ItemType);
            if (handler is IItemCombinable combinable)
            {
                if (combinable.CanCombine(sourceItem, targetItem))
                {
                    combinable.Combine(sourceItem, targetItem);
                    OnItemsCombined?.Invoke(sourceItem, targetItem);
                    Debug.Log($"InventoryOperationManager: Combined {sourceItem.ItemName} with {targetItem.ItemName}");
                }
                else
                {
                    Debug.LogWarning($"InventoryOperationManager: Cannot combine {sourceItem.ItemName} with {targetItem.ItemName}");
                }
            }
            else
            {
                Debug.LogWarning($"InventoryOperationManager: Item {sourceItem.ItemName} is not combinable");
            }
        }
        
        // ==================== DROP Operation ====================
        
        /// <summary>
        /// Check if item can be dropped
        /// </summary>
        public bool CanDrop(GridItem item)
        {
            if (item == null) return false;
            
            var handler = GetHandler(item.ItemType);
            if (handler is IItemDroppable droppable)
            {
                return droppable.CanDrop(item);
            }
            
            return false;
        }
        
        /// <summary>
        /// Drop item
        /// Permanently destroy item
        /// </summary>
        public void DropItem(GridItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("InventoryOperationManager: Cannot drop null item");
                return;
            }
            
            var handler = GetHandler(item.ItemType);
            if (handler is IItemDroppable droppable)
            {
                if (droppable.CanDrop(item))
                {
                    droppable.Drop(item);
                    OnItemDropped?.Invoke(item);
                    Debug.Log($"InventoryOperationManager: Dropped item {item.ItemName}");
                }
                else
                {
                    Debug.LogWarning($"InventoryOperationManager: Cannot drop item {item.ItemName}");
                }
            }
            else
            {
                Debug.LogWarning($"InventoryOperationManager: Item {item.ItemName} is not droppable");
            }
        }
        
        #endregion
        
        #region Cleanup
        
        public void Cleanup()
        {
            // Grid operation events
            OnItemMoved = null;
            OnItemRotated = null;
            OnItemsStacked = null;
            
            // Item operation events
            OnItemUsed = null;
            OnItemDropped = null;
            OnItemsCombined = null;
            
            // Clear handlers
            _operationHandlers?.Clear();
        }
        
        #endregion
    }
}

