using UnityEngine;
using Resonance.Player.Inventory;

namespace Resonance.Player.Inventory
{
    /// <summary>
    /// InventoryOperationManager - Manage grid operations
    /// Responsibilities: move items, rotate items, stack items, collision detection
    /// </summary>
    public class InventoryOperationManager
    {
        private PlayerInventory _inventory;
        private ConsumableManager _consumableManager;
        
        // Events
        public System.Action<int, Vector2Int, Vector2Int> OnItemMoved; // itemID, oldPos, newPos
        public System.Action<int, int> OnItemRotated; // itemID, newRotation
        public System.Action<int, int> OnItemsStacked; // sourceID, targetID
        
        public InventoryOperationManager(PlayerInventory inventory, ConsumableManager consumableManager)
        {
            _inventory = inventory;
            _consumableManager = consumableManager;
            Debug.Log("InventoryOperationManager: Initialized");
        }
        
        #region 移动操作 - Move Operations
        
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
        
        #region 旋转操作 - Rotate Operations
        
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
        
        #region Auto Arrange
        
        /// <summary>
        /// Auto arrange inventory (optional feature)
        /// </summary>
        public void AutoArrangeInventory()
        {
            Debug.Log("InventoryOperationManager: Auto-arranging inventory (not implemented yet)");
            // TODO: Implement auto arrange logic
            // 1. Get all items
            // 2. Sort by type and size
            // 3. Re-place from top-left
            // 4. Auto stack同类弹药
        }
        
        #endregion
        
        #region Cleanup
        
        public void Cleanup()
        {
            OnItemMoved = null;
            OnItemRotated = null;
            OnItemsStacked = null;
        }
        
        #endregion
    }
}

