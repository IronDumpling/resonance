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
        [Header("Grid Configuration")]
        [SerializeField] private float _slotSize = 64f;
        [SerializeField] private float _slotSpacing = 2f;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _slotPrefab;
        
        [Header("Visual Settings")]
        [SerializeField] private Transform _itemVisualsContainer;
        [SerializeField] private Color _gridColor = Color.white;
        [SerializeField] private Color _occupiedColor = Color.gray;
        [SerializeField] private Color _highlightColor = Color.yellow;
        
        private int _gridWidth;
        private int _gridHeight;
        private GridSlot[,] _slots;
        private List<GridItem> _items = new List<GridItem>();
        private Dictionary<int, GridItemVisual> _itemVisuals = new Dictionary<int, GridItemVisual>(); // itemID -> visual
        private GridItem _selectedItem;
        private GridItemVisual _selectedVisual;
        private bool _isInitialized = false;
        
        // 属性
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public bool IsInitialized => _isInitialized;
        public GridItem SelectedItem => _selectedItem;
        
        // 事件
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
            
            // 创建槽位数组
            _slots = new GridSlot[_gridWidth, _gridHeight];
            
            // 创建 item visuals 容器（在槽位之上渲染）
            CreateItemVisualsContainer();
            
            // 创建槽位对象
            CreateSlots();
            
            // 设置槽位事件
            SetupSlotEvents();
            
            _isInitialized = true;

            Debug.Log($"GridSystem: Initialized slots array {_gridWidth}x{_gridHeight}");
        }
        
        /// <summary>
        /// 初始化网格系统（使用默认尺寸）
        /// </summary>
        public void InitializeGrid()
        {
            InitializeGrid(_gridWidth, _gridHeight);
        }
        
        /// <summary>
        /// 创建物品视觉容器
        /// </summary>
        private void CreateItemVisualsContainer()
        {
            Debug.Log($"GridSystem: CreateItemVisualsContainer called");

            RectTransform containerRect = null;
            
            if (_itemVisualsContainer == null)
            {
                _itemVisualsContainer = new GameObject("ItemVisualsContainer").transform;
                _itemVisualsContainer.SetParent(transform, false);
                _itemVisualsContainer.SetAsLastSibling();
                containerRect = _itemVisualsContainer.gameObject.AddComponent<RectTransform>();
                Debug.Log("GridSystem: Created new ItemVisualsContainer");
            }
            else
            {
                containerRect = _itemVisualsContainer.gameObject.GetComponent<RectTransform>();
            }
            
            Debug.Log($"GridSystem: Set _itemVisualsContainer = {(_itemVisualsContainer != null ? "EXISTS" : "NULL")}");
            
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
            
            Debug.Log($"GridSystem: Created item visuals container with size {totalWidth}x{totalHeight}");
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
                    GameObject slotObj = Instantiate(_slotPrefab, transform);
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
            if (item == null || !IsPositionValid(position)) return false;
            
            // 检查物品是否超出边界
            if (position.x + item.CurrentWidth > _gridWidth || 
                position.y + item.CurrentHeight > _gridHeight)
                return false;
            
            // 检查区域是否被占用
            return IsAreaEmpty(position, item.CurrentWidth, item.CurrentHeight);
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
            
            Vector2Int currentPosition = item.gridPosition;
            
            // 临时旋转物品
            item.Rotate(clockwise);
            
            // 检查旋转后是否可以放置
            if (CanPlaceItem(item, currentPosition))
            {
                // 更新槽位
                UpdateSlotsForItem(item);
                
                // Update visual representation
                CreateOrUpdateItemVisual(item);
                
                OnItemRotated?.Invoke(item);
                Debug.Log($"GridSystem: Rotated item {item.itemName}");
                return true;
            }
            else
            {
                // 旋转失败，恢复原状态
                item.Rotate(!clockwise);
                Debug.LogWarning($"GridSystem: Cannot rotate item {item.itemName} - no space");
                return false;
            }
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
        
        public bool IsAreaEmpty(Vector2Int position, int width, int height)
        {
            if (!IsPositionValid(position)) return false;
            if (position.x + width > _gridWidth || position.y + height > _gridHeight) return false;
            
            for (int x = position.x; x < position.x + width; x++)
            {
                for (int y = position.y; y < position.y + height; y++)
                {
                    if (GetItemAt(new Vector2Int(x, y)) != null)
                    {
                        return false;
                    }
                }
            }
            
            return true;
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
            if (_itemVisualsContainer == null && _isInitialized)
            {
                Debug.LogWarning($"GridSystem: ItemVisualsContainer is null but GridSystem is initialized. Recreating container.");
                CreateItemVisualsContainer();
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
            if (_itemVisualsContainer == null)
            {
                if (_isInitialized)
                {
                    Debug.LogWarning($"GridSystem: ItemVisualsContainer is null but GridSystem is initialized. Attempting to recreate container.");
                    EnsureItemVisualsContainer();
                    
                    if (_itemVisualsContainer == null)
                    {
                        Debug.LogError($"GridSystem: Failed to create ItemVisualsContainer. Cannot create visual for {item.itemName}");
                        return;
                    }
                }
                else
                {
                    // GridSystem not initialized yet - this is fine, visuals will be created when UI opens
                    Debug.LogWarning($"GridSystem: ItemVisualsContainer not initialized yet. Item {item.itemName} visual will be created when inventory panel opens.");
                    Debug.LogWarning($"GridSystem: _isInitialized = {_isInitialized}, _itemVisualsContainer = {(_itemVisualsContainer != null ? "EXISTS" : "NULL")}");
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
            }
            else
            {
                // Instantiate the prefab
                // which should already have GridItemVisual and Button components
                visualObj = Instantiate(item.itemPrefab, _itemVisualsContainer);
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
            visual.Initialize(item, _slotSize, _itemVisualsContainer);
            
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
