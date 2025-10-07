using UnityEngine;
using UnityEngine.UI;
using Resonance.Player.Core;
using Resonance.Player.Inventory;
using System.Collections.Generic;
using System.Linq;

namespace Resonance.Utilities
{
    /// <summary>
    /// 可复用的网格系统实现
    /// 支持物品放置、移动、旋转等操作
    /// </summary>
    public class GridSystem : MonoBehaviour, IGridSystem
    {
        [Header("Visual Container")]
        [SerializeField] private Transform _slotVisualContainer;
        [SerializeField] private Transform _itemVisualContainer;

        [Header("Grid Configuration")]
        [SerializeField] private float _slotSize = 64f;
        [SerializeField] private float _slotSpacing = 2f;
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private Color _gridColor = Color.white;
        
        private int _gridWidth;
        private int _gridHeight;
        private GridSlot[,] _slots;
        private List<GridItem> _items = new List<GridItem>();
        private Dictionary<int, GridItemVisual> _itemVisuals = new Dictionary<int, GridItemVisual>(); // itemID -> visual
        private GridItem _selectedItem;
        private GridItemVisual _selectedVisual;
        private bool _isInitialized = false;
        
        // Properties
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public bool IsInitialized => _isInitialized;
        public GridItem SelectedItem => _selectedItem;
        
        // Events
        public System.Action<GridItem> OnItemPlaced { get; set; }
        public System.Action<GridItem> OnItemMoved { get; set; }
        public System.Action<GridItem> OnItemRotated { get; set; }
        public System.Action<GridItem> OnItemRemoved { get; set; }
        public System.Action<GridItem> OnItemSelected { get; set; }
        public System.Action<GridItem> OnItemDeselected { get; set; }
        
        /// <summary>
        /// Initialize grid system with specified size
        /// </summary>
        /// <param name="width">Grid width (from PlayerBaseStats)</param>
        /// <param name="height">Grid height (from PlayerBaseStats)</param>
        public void InitializeGrid(int width, int height)
        {
            Debug.Log($"GridSystem: InitializeGrid called with width={width}, height={height}");
            
            if (_isInitialized)
            {
                Debug.LogWarning("GridSystem: Already initialized");
                return;
            }
            
            // Override with provided dimensions
            _gridWidth = width;
            _gridHeight = height;
            _slots = new GridSlot[_gridWidth, _gridHeight];
            
            // Render Grid Container and Slots
            CreateSlotVisualContainer();
            CreateSlots();

            // Render Item Visual Containers Above Slots
            CreateItemVisualContainer();
            
            SetupSlotEvents();
            
            _isInitialized = true;

            Debug.Log($"GridSystem: Initialized slots array {_gridWidth}x{_gridHeight}");
        }

        private void CreateSlotVisualContainer()
        {
            RectTransform containerRect = null;
            if (_slotVisualContainer == null)
            {
                _slotVisualContainer = new GameObject("SlotVisualContainer").transform;
                _slotVisualContainer.SetParent(transform, false);
                _slotVisualContainer.SetAsLastSibling();
                containerRect = _slotVisualContainer.gameObject.AddComponent<RectTransform>();
                Debug.Log("GridSystem: Created new ItemVisualsContainer");
            }
            else
            {
                containerRect = _slotVisualContainer.gameObject.GetComponent<RectTransform>();
            }

            SetContainerRectTransform(containerRect);
        }

        /// <summary>
        /// 创建槽位对象
        /// </summary>
        private void CreateSlots()
        {            
            if (_slotPrefab == null)
            {
                Debug.LogError("GridSystem: Slot prefab is not assigned!");
                return;
            }
            
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    GameObject slotObj = Instantiate(_slotPrefab, _slotVisualContainer);
                    slotObj.name = $"Slot_{x}_{y}";
                    
                    GridSlot slot = slotObj.GetComponent<GridSlot>();
                    if (slot == null)
                    {
                        slot = slotObj.AddComponent<GridSlot>();
                    }
                    
                    slot.Initialize(new Vector2Int(x, y));
                    _slots[x, y] = slot;
                    
                    // 设置位置
                    RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        float xPos = x * (_slotSize + _slotSpacing);
                        float yPos = -y * (_slotSize + _slotSpacing);
                        rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                        rectTransform.sizeDelta = new Vector2(_slotSize, _slotSize);
                    }
                }
            }
        }
        
        /// <summary>
        /// 创建物品视觉容器
        /// </summary>
        private void CreateItemVisualContainer()
        {
            Debug.Log($"GridSystem: CreateItemVisualContainer called");

            RectTransform containerRect = null;
            
            if (_itemVisualContainer == null)
            {
                _itemVisualContainer = new GameObject("ItemVisualsContainer").transform;
                _itemVisualContainer.SetParent(transform, false);
                _itemVisualContainer.SetAsLastSibling();
                containerRect = _itemVisualContainer.gameObject.AddComponent<RectTransform>();
                Debug.Log("GridSystem: Created new ItemVisualsContainer");
            }
            else
            {
                containerRect = _itemVisualContainer.gameObject.GetComponent<RectTransform>();
            }
            
            SetContainerRectTransform(containerRect);
        }

        private void SetContainerRectTransform(RectTransform containerRect)
        {
            // Set up RectTransform to cover entire grid area
            // Use top-left anchor
            containerRect.anchorMin = new Vector2(0, 1); // Top-left
            containerRect.anchorMax = new Vector2(0, 1); // Top-left
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.pivot = new Vector2(0, 1);     // Top-left pivot
        
            // Calculate size to cover entire grid
            float totalWidth = _gridWidth * _slotSize + (_gridWidth - 1) * _slotSpacing;
            float totalHeight = _gridHeight * _slotSize + (_gridHeight - 1) * _slotSpacing;
            containerRect.sizeDelta = new Vector2(totalWidth, totalHeight);

            Debug.Log($"GridSystem: Created visuals container with size {totalWidth}x{totalHeight}");
        }
        
        /// <summary>
        /// 设置槽位事件
        /// </summary>
        private void SetupSlotEvents()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    GridSlot slot = _slots[x, y];
                    if (slot != null)
                    {
                        slot.OnSlotClicked += OnSlotClicked;
                        slot.OnSlotHovered += OnSlotHovered;
                        slot.OnSlotUnhovered += OnSlotUnhovered;
                    }
                }
            }
        }
        
        #region IGridSystem Implementation
        
        public bool CanPlaceItem(GridItem item, Vector2Int position)
        {
            if (item == null || !IsPositionValid(position)) 
            {
                Debug.Log($"CanPlaceItem: Item is null or position {position} is invalid");
                return false;
            }
            
            // 检查物品是否超出边界
            bool xOutOfBounds = position.x + item.CurrentWidth > _gridWidth;
            bool yOutOfBounds = position.y + item.CurrentHeight > _gridHeight;
            
            Debug.Log($"CanPlaceItem: Item {item.itemName} at {position}, size {item.CurrentWidth}x{item.CurrentHeight}");
            Debug.Log($"CanPlaceItem: Grid size {_gridWidth}x{_gridHeight}");
            Debug.Log($"CanPlaceItem: X bounds check: {position.x} + {item.CurrentWidth} = {position.x + item.CurrentWidth} > {_gridWidth} = {xOutOfBounds}");
            Debug.Log($"CanPlaceItem: Y bounds check: {position.y} + {item.CurrentHeight} = {position.y + item.CurrentHeight} > {_gridHeight} = {yOutOfBounds}");
            
            if (xOutOfBounds || yOutOfBounds) 
            {
                Debug.Log($"CanPlaceItem: Item {item.itemName} would be out of bounds");
                return false;
            }
            
            // 检查区域是否被占用（排除物品自身）
            bool areaEmpty = IsAreaEmpty(position, item);
            Debug.Log($"CanPlaceItem: Area empty check result: {areaEmpty}");
            return areaEmpty;
        }
        
        public bool PlaceItem(GridItem item, Vector2Int position)
        {
            Debug.Log($"GridSystem: PlaceItem called for {item?.itemName ?? "NULL"} at {position}");
            
            // Ensure container exists before placing item
            EnsureItemVisualsContainer();
            
            if (!CanPlaceItem(item, position)) 
            {
                Debug.LogWarning($"GridSystem: Cannot place item {item?.itemName ?? "NULL"} at {position}");
                return false;
            }
            
            // 移除物品的旧位置
            if (item.gridPosition.x >= 0 && item.gridPosition.y >= 0)
            {
                Debug.Log($"GridSystem: Removing item from old position {item.gridPosition}");
                RemoveItemFromSlots(item);
                // Don't destroy visual here, we'll update it
            }
            
            // 设置新位置
            item.SetGridPosition(position);
            
            // 添加到物品列表
            if (!_items.Contains(item))
            {
                _items.Add(item);
                Debug.Log($"GridSystem: Added item to _items list");
            }
            
            // 更新槽位
            UpdateSlotsForItem(item);
            
            // Create or update visual representation
            CreateOrUpdateItemVisual(item);
            
            OnItemPlaced?.Invoke(item);
            Debug.Log($"GridSystem: Placed item {item.itemName} at {position}");
            
            return true;
        }
        
        public bool MoveItem(GridItem item, Vector2Int newPosition)
        {
            if (item == null || !_items.Contains(item)) return false;
            
            Vector2Int oldPosition = item.gridPosition;
            
            // PlaceItem already handles UpdateSlotsForItem and CreateOrUpdateItemVisual
            if (PlaceItem(item, newPosition))
            {
                OnItemMoved?.Invoke(item);
                Debug.Log($"GridSystem: Moved item {item.itemName} from {oldPosition} to {newPosition}");
                return true;
            }
            
            return false;
        }
        
        public bool RotateItem(GridItem item, bool clockwise = true)
        {
            if (item == null || !_items.Contains(item)) return false;
            
            Vector2Int oldPosition = item.gridPosition;
            int oldWidth = item.CurrentWidth;
            int oldHeight = item.CurrentHeight;
            
            Debug.Log($"GridSystem: RotateItem called - Item: {item.itemName}, Old position: {oldPosition}, Old size: {oldWidth}x{oldHeight}");
            
            // Clear current slot occupancy
            RemoveItemFromSlots(item);
            
            // Rotate the item (this toggles isRotated flag)
            item.Rotate(clockwise);
            
            int newWidth = item.CurrentWidth;
            int newHeight = item.CurrentHeight;
            
            Debug.Log($"GridSystem: After rotation - New size: {newWidth}x{newHeight}");
            
            // Calculate geometric center of the item before rotation
            float centerX = oldPosition.x + oldWidth / 2.0f;
            float centerY = oldPosition.y + oldHeight / 2.0f;
            
            Debug.Log($"GridSystem: Geometric center: ({centerX}, {centerY})");
            
            // Calculate new top-left position to keep the center as close as possible
            float newPosX = centerX - newWidth / 2.0f;
            float newPosY = centerY - newHeight / 2.0f;
            
            // Round to nearest grid position
            Vector2Int newPosition = new Vector2Int(
                Mathf.RoundToInt(newPosX),
                Mathf.RoundToInt(newPosY)
            );
            
            Debug.Log($"GridSystem: Calculated new position (before clamping): {newPosition}");
            
            // Try to place at the calculated position
            if (CanPlaceItem(item, newPosition))
            {
                item.SetGridPosition(newPosition);
                UpdateSlotsForItem(item);
                CreateOrUpdateItemVisual(item);
                
                OnItemRotated?.Invoke(item);
                Debug.Log($"GridSystem: Successfully rotated {item.itemName} from {oldPosition} to {newPosition}");
                return true;
            }
            
            // If calculated position doesn't work, try nearby positions
            Debug.Log($"GridSystem: Calculated position {newPosition} doesn't work, searching nearby...");
            Vector2Int adjustedPosition = FindBestPositionForRotatedItem(item, newPosition);
            if (adjustedPosition.x >= 0 && CanPlaceItem(item, adjustedPosition))
            {
                item.SetGridPosition(adjustedPosition);
                UpdateSlotsForItem(item);
                CreateOrUpdateItemVisual(item);
                
                OnItemRotated?.Invoke(item);
                Debug.Log($"GridSystem: Rotated {item.itemName} and adjusted position from {newPosition} to {adjustedPosition}");
                return true;
            }
            
            // Rotation failed, restore original state
            item.Rotate(!clockwise);
            item.SetGridPosition(oldPosition);
            UpdateSlotsForItem(item);
            
            Debug.LogWarning($"GridSystem: Cannot rotate item {item.itemName} - no space available");
            return false;
        }
        
        /// <summary>
        /// 为旋转后的物品找到最佳位置
        /// </summary>
        private Vector2Int FindBestPositionForRotatedItem(GridItem item, Vector2Int originalPosition)
        {
            // 尝试在原始位置附近找到合适的位置
            int searchRadius = 2; // 搜索半径
            
            for (int radius = 0; radius <= searchRadius; radius++)
            {
                for (int x = originalPosition.x - radius; x <= originalPosition.x + radius; x++)
                {
                    for (int y = originalPosition.y - radius; y <= originalPosition.y + radius; y++)
                    {
                        Vector2Int testPosition = new Vector2Int(x, y);
                        
                        // 检查是否在网格范围内
                        if (testPosition.x >= 0 && testPosition.y >= 0 &&
                            testPosition.x + item.CurrentWidth <= _gridWidth &&
                            testPosition.y + item.CurrentHeight <= _gridHeight)
                        {
                            if (IsAreaEmpty(testPosition, item))
                            {
                                return testPosition;
                            }
                        }
                    }
                }
            }
            
            return new Vector2Int(-1, -1); // 没有找到合适位置
        }
        
        public bool RemoveItem(GridItem item)
        {
            if (item == null || !_items.Contains(item)) return false;
            
            RemoveItemFromSlots(item);
            _items.Remove(item);
            
            // Destroy visual representation
            DestroyItemVisual(item);
            
            OnItemRemoved?.Invoke(item);
            Debug.Log($"GridSystem: Removed item {item.itemName}");
            
            return true;
        }
        
        public GridItem GetItemAt(Vector2Int position)
        {
            if (!IsPositionValid(position)) return null;
            
            return _items.FirstOrDefault(item => item.OccupiesPosition(position));
        }
        
        public List<GridItem> GetAllItems()
        {
            return new List<GridItem>(_items);
        }
        
        public Vector2Int FindEmptySpace(int width, int height)
        {
            for (int y = 0; y <= _gridHeight - height; y++)
            {
                for (int x = 0; x <= _gridWidth - width; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (IsAreaEmpty(position, width, height))
                    {
                        return position;
                    }
                }
            }
            
            return new Vector2Int(-1, -1); // 没有找到空位
        }
        
        public List<Vector2Int> FindEmptySpaces(int width, int height)
        {
            var emptySpaces = new List<Vector2Int>();
            
            for (int y = 0; y <= _gridHeight - height; y++)
            {
                for (int x = 0; x <= _gridWidth - width; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (IsAreaEmpty(position, width, height))
                    {
                        emptySpaces.Add(position);
                    }
                }
            }
            
            return emptySpaces;
        }
        
        public bool IsPositionValid(Vector2Int position)
        {
            return position.x >= 0 && position.x < _gridWidth && 
                   position.y >= 0 && position.y < _gridHeight;
        }
        
        /// <summary>
        /// Core logic: Check if the specified area is empty
        /// </summary>
        /// <param name="position">Top-left position of the area</param>
        /// <param name="width">Width of the area</param>
        /// <param name="height">Height of the area</param>
        /// <param name="excludeItem">Item to exclude from the check (optional)</param>
        /// <returns>True if the area is empty</returns>
        private bool IsAreaEmptyCore(Vector2Int position, int width, int height, GridItem excludeItem = null)
        {
            if (!IsPositionValid(position)) 
            {
                Debug.Log($"IsAreaEmpty: Position {position} is invalid");
                return false;
            }
            if (position.x + width > _gridWidth || position.y + height > _gridHeight) 
            {
                Debug.Log($"IsAreaEmpty: Area {position} to ({position.x + width - 1}, {position.y + height - 1}) is out of bounds");
                return false;
            }
            
            Debug.Log($"IsAreaEmpty: Checking area from {position} to ({position.x + width - 1}, {position.y + height - 1})" + 
                     (excludeItem != null ? $" (excluding {excludeItem.itemName})" : ""));
            
            for (int x = position.x; x < position.x + width; x++)
            {
                for (int y = position.y; y < position.y + height; y++)
                {
                    Vector2Int checkPos = new Vector2Int(x, y);
                    var itemAtPos = GetItemAt(checkPos);
                    if (itemAtPos != null && itemAtPos != excludeItem)
                    {
                        Debug.Log($"IsAreaEmpty: Position {checkPos} is occupied by {itemAtPos.itemName}");
                        return false;
                    }
                }
            }
            
            Debug.Log($"IsAreaEmpty: Area is empty");
            return true;
        }
        
        /// <summary>
        /// Check if the specified item can be placed at the given position (Polymorphic overload 1)
        /// </summary>
        /// <param name="position">Position to place the item</param>
        /// <param name="item">Item to be placed</param>
        /// <returns>True if the item can be placed</returns>
        public bool IsAreaEmpty(Vector2Int position, GridItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("IsAreaEmpty: Item is null");
                return false;
            }

            int width = item.CurrentWidth;
            int height = item.CurrentHeight;
            
            return IsAreaEmptyCore(position, width, height, item);
        }
        
        /// <summary>
        /// Check if the specified area with given dimensions is empty (Polymorphic overload 2)
        /// </summary>
        /// <param name="position">Top-left position of the area</param>
        /// <param name="width">Width of the area</param>
        /// <param name="height">Height of the area</param>
        /// <returns>True if the area is empty</returns>
        public bool IsAreaEmpty(Vector2Int position, int width, int height)
        {
            return IsAreaEmptyCore(position, width, height, null);
        }
        
        /// <summary>
        /// Select item visual (internal method)
        /// </summary>
        private void SelectItemVisual(GridItemVisual visual)
        {
            if (visual == null) return;
            
            // Deselect previous
            if (_selectedVisual != null)
            {
                _selectedVisual.SetSelected(false);
            }
            
            // Select new
            _selectedVisual = visual;
            _selectedItem = visual.GridItem;
            _selectedVisual.SetSelected(true);
            
            OnItemSelected?.Invoke(_selectedItem);
            Debug.Log($"GridSystem: Selected item {_selectedItem.itemName}");
        }
        
        /// <summary>
        /// Select item by GridItem (public API for external use)
        /// </summary>
        public void SelectItem(GridItem item)
        {
            if (item == null)
            {
                DeselectItem();
                return;
            }
            
            if (_itemVisuals.TryGetValue(item.itemID, out GridItemVisual visual))
            {
                SelectItemVisual(visual);
            }
        }
        
        /// <summary>
        /// Deselect current item
        /// </summary>
        public void DeselectItem()
        {
            if (_selectedVisual != null)
            {
                _selectedVisual.SetSelected(false);
                OnItemDeselected?.Invoke(_selectedItem);
                _selectedVisual = null;
                _selectedItem = null;
                Debug.Log("GridSystem: Deselected item");
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 从槽位中移除物品
        /// </summary>
        /// <param name="item">要移除的物品</param>
        private void RemoveItemFromSlots(GridItem item)
        {
            if (item.gridPosition.x < 0 || item.gridPosition.y < 0) return;
            
            for (int x = item.gridPosition.x; x < item.gridPosition.x + item.CurrentWidth; x++)
            {
                for (int y = item.gridPosition.y; y < item.gridPosition.y + item.CurrentHeight; y++)
                {
                    if (IsPositionValid(new Vector2Int(x, y)))
                    {
                        _slots[x, y].SetItem(null);
                    }
                }
            }
        }
        
        /// <summary>
        /// 更新物品占用的槽位
        /// </summary>
        /// <param name="item">物品</param>
        private void UpdateSlotsForItem(GridItem item)
        {
            if (item.gridPosition.x < 0 || item.gridPosition.y < 0) return;
            
            for (int x = item.gridPosition.x; x < item.gridPosition.x + item.CurrentWidth; x++)
            {
                for (int y = item.gridPosition.y; y < item.gridPosition.y + item.CurrentHeight; y++)
                {
                    if (IsPositionValid(new Vector2Int(x, y)))
                    {
                        _slots[x, y].SetItem(item);
                    }
                }
            }
        }
        
        /// <summary>
        /// 确保 ItemVisualsContainer 存在，如果不存在则创建它
        /// </summary>
        private void EnsureItemVisualsContainer()
        {
            if (_itemVisualContainer == null && _isInitialized)
            {
                Debug.LogWarning($"GridSystem: ItemVisualsContainer is null but GridSystem is initialized. Recreating container.");
                CreateItemVisualContainer();
            }
        }
        
        /// <summary>
        /// 创建或更新物品的视觉表现
        /// </summary>
        private void CreateOrUpdateItemVisual(GridItem item)
        {
            Debug.Log($"GridSystem: CreateOrUpdateItemVisual called for item: {item?.itemName ?? "NULL"}");
            
            if (item == null)
            {
                Debug.LogError("GridSystem: Cannot create visual for null item");
                return;
            }
            
            // Ensure container exists (lazy initialization)
            if (_itemVisualContainer == null)
            {
                if (_isInitialized)
                {
                    Debug.LogWarning($"GridSystem: ItemVisualsContainer is null but GridSystem is initialized. Attempting to recreate container.");
                    EnsureItemVisualsContainer();
                    
                    if (_itemVisualContainer == null)
                    {
                        Debug.LogError($"GridSystem: Failed to create ItemVisualsContainer. Cannot create visual for {item.itemName}");
                        return;
                    }
                }
                else
                {
                    // GridSystem not initialized yet - this is fine, visuals will be created when UI opens
                    Debug.LogWarning($"GridSystem: ItemVisualsContainer not initialized yet. Item {item.itemName} visual will be created when inventory panel opens.");
                    Debug.LogWarning($"GridSystem: _isInitialized = {_isInitialized}, _itemVisualContainer = {(_itemVisualContainer != null ? "EXISTS" : "NULL")}");
                    return;
                }
            }
            
            // Check if visual already exists
            if (_itemVisuals.TryGetValue(item.itemID, out GridItemVisual existingVisual))
            {
                // Update existing visual
                existingVisual.UpdateSizeAndPosition();
                Debug.Log($"GridSystem: Updated visual for item {item.itemName}");
                return;
            }

            // Instantiate the item prefab or create fallback
            GameObject visualObj = null;
            GridItemVisual visual = null;
            
            if (item.itemPrefab == null)
            {
                // Fallback: Create simple visual with icon
                Debug.LogWarning($"GridSystem: No prefab found for {item.itemName}, creating fallback visual");
                visualObj = new GameObject($"ItemVisual_{item.itemID}_{item.itemName}");
                
                // Add Image for icon
                Image iconImage = visualObj.AddComponent<Image>();
                iconImage.sprite = item.itemIcon;
                iconImage.color = item.itemColor;
                
                // Add Button for interaction
                Button button = visualObj.AddComponent<Button>();
                
                // Add GridItemVisual
                visual = visualObj.AddComponent<GridItemVisual>();

                RectTransform containerRect = visualObj.GetComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0, 1); // Top-left
                containerRect.anchorMax = new Vector2(0, 1); // Top-left
                containerRect.anchoredPosition = Vector2.zero;
                containerRect.pivot = new Vector2(0, 1);     // Top-left pivot
            }
            else
            {
                // Instantiate the prefab
                // which should already have GridItemVisual and Button components
                visualObj = Instantiate(item.itemPrefab, _itemVisualContainer);
                visualObj.name = $"ItemVisual_{item.itemID}_{item.itemName}";
                
                // Get or add GridItemVisual component
                visual = visualObj.GetComponent<GridItemVisual>();
                if (visual == null)
                {
                    visual = visualObj.AddComponent<GridItemVisual>();
                    Debug.LogWarning($"GridSystem: ItemPrefab for {item.itemName} doesn't have GridItemVisual component. Adding it.");
                }
            }

            // Subscribe to click event
            visual.OnItemClicked += OnItemVisualClicked;
            
            // Initialize the visual
            visual.Initialize(item, _slotSize, _itemVisualContainer);
            
            // Store reference
            _itemVisuals[item.itemID] = visual;
            
            Debug.Log($"GridSystem: Created visual for item {item.itemName} (HasPrefab: {item.itemPrefab != null}, Position: {item.gridPosition})");
        }
        
        /// <summary>
        /// 销毁物品的视觉表现
        /// </summary>
        private void DestroyItemVisual(GridItem item)
        {
            if (item == null) return;
            
            if (_itemVisuals.TryGetValue(item.itemID, out GridItemVisual visual))
            {
                // Unsubscribe from events
                if (visual != null)
                {
                    visual.OnItemClicked -= OnItemVisualClicked;
                }
                
                // Clear selection if this was selected
                if (_selectedVisual == visual)
                {
                    _selectedVisual = null;
                    _selectedItem = null;
                }
                
                _itemVisuals.Remove(item.itemID);
                if (visual != null)
                {
                    Destroy(visual.gameObject);
                }
                Debug.Log($"GridSystem: Destroyed visual for item {item.itemName}");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle grid slot clicked (empty slot or background)
        /// </summary>
        private void OnSlotClicked(GridSlot slot)
        {
            // Clicking on empty slot deselects current item
            DeselectItem();
        }
        
        /// <summary>
        /// Handle item visual clicked
        /// </summary>
        private void OnItemVisualClicked(GridItemVisual visual)
        {
            if (visual == null || visual.GridItem == null) return;
            
            // Toggle selection
            if (_selectedVisual == visual)
            {
                // Clicked on already selected item - deselect
                DeselectItem();
            }
            else
            {
                // Select this item
                SelectItemVisual(visual);
            }
        }
        
        private void OnSlotHovered(GridSlot slot)
        {
            // 可以在这里添加悬停效果
        }
        
        private void OnSlotUnhovered(GridSlot slot)
        {
            // 可以在这里移除悬停效果
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 清空所有物品
        /// </summary>
        public void ClearAllItems()
        {
            Debug.Log($"GridSystem: ClearAllItems called");
            
            var itemsToRemove = new List<GridItem>(_items);
            foreach (var item in itemsToRemove)
            {
                RemoveItem(item);
            }
            
            // Ensure all visuals are cleaned up
            foreach (var visual in _itemVisuals.Values)
            {
                if (visual != null)
                {
                    Destroy(visual.gameObject);
                }
            }
            _itemVisuals.Clear();
            
            Debug.Log($"GridSystem: ClearAllItems completed. Items count: {_items.Count}, Visuals count: {_itemVisuals.Count}");
        }
        
        /// <summary>
        /// 获取指定类型的物品
        /// </summary>
        /// <param name="itemType">物品类型</param>
        /// <returns>物品列表</returns>
        public List<GridItem> GetItemsByType(ItemType itemType)
        {
            return _items.Where(item => item.itemType == itemType).ToList();
        }
        
        /// <summary>
        /// 检查是否有指定ID的物品
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <returns>是否存在</returns>
        public bool HasItem(int itemID)
        {
            return _items.Any(item => item.itemID == itemID);
        }
        
        /// <summary>
        /// 获取指定ID的物品
        /// </summary>
        /// <param name="itemID">物品ID</param>
        /// <returns>物品</returns>
        public GridItem GetItemByID(int itemID)
        {
            return _items.FirstOrDefault(item => item.itemID == itemID);
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnDestroy()
        {
            // 清理事件
            OnItemPlaced = null;
            OnItemMoved = null;
            OnItemRotated = null;
            OnItemRemoved = null;
            OnItemSelected = null;
            OnItemDeselected = null;
        }
        
        #endregion
    }
}
