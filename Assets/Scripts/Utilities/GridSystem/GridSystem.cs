using UnityEngine;
using Resonance.Player.Core;
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
        [SerializeField] private int _gridWidth = 10;
        [SerializeField] private int _gridHeight = 6;
        [SerializeField] private float _slotSize = 64f;
        [SerializeField] private float _slotSpacing = 2f;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject _slotPrefab;
        [SerializeField] private GameObject _itemPrefab;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _gridColor = Color.white;
        [SerializeField] private Color _occupiedColor = Color.gray;
        [SerializeField] private Color _highlightColor = Color.yellow;
        
        // 核心数据
        private GridSlot[,] _slots;
        private List<GridItem> _items = new List<GridItem>();
        private GridItem _selectedItem;
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
        
        private void Awake()
        {
            InitializeGrid();
        }
        
        /// <summary>
        /// 初始化网格系统
        /// </summary>
        public void InitializeGrid()
        {
            if (_isInitialized) return;
            
            Debug.Log($"GridSystem: Initializing grid {_gridWidth}x{_gridHeight}");
            
            // 创建槽位数组
            _slots = new GridSlot[_gridWidth, _gridHeight];
            
            // 创建槽位对象
            CreateSlots();
            
            // 设置槽位事件
            SetupSlotEvents();
            
            _isInitialized = true;
            Debug.Log("GridSystem: Initialization complete");
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
            if (!CanPlaceItem(item, position)) return false;
            
            // 移除物品的旧位置
            if (item.gridPosition.x >= 0 && item.gridPosition.y >= 0)
            {
                RemoveItemFromSlots(item);
            }
            
            // 设置新位置
            item.SetGridPosition(position);
            
            // 添加到物品列表
            if (!_items.Contains(item))
            {
                _items.Add(item);
            }
            
            // 更新槽位
            UpdateSlotsForItem(item);
            
            OnItemPlaced?.Invoke(item);
            Debug.Log($"GridSystem: Placed item {item.itemName} at {position}");
            
            return true;
        }
        
        public bool MoveItem(GridItem item, Vector2Int newPosition)
        {
            if (item == null || !_items.Contains(item)) return false;
            
            Vector2Int oldPosition = item.gridPosition;
            
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
        
        public void SelectItem(GridItem item)
        {
            if (_selectedItem == item) return;
            
            // 取消之前的选择
            if (_selectedItem != null)
            {
                _selectedItem.isSelected = false;
                OnItemDeselected?.Invoke(_selectedItem);
            }
            
            // 选择新物品
            _selectedItem = item;
            if (_selectedItem != null)
            {
                _selectedItem.isSelected = true;
                OnItemSelected?.Invoke(_selectedItem);
            }
        }
        
        public void DeselectItem()
        {
            if (_selectedItem != null)
            {
                _selectedItem.isSelected = false;
                OnItemDeselected?.Invoke(_selectedItem);
                _selectedItem = null;
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
        
        #endregion
        
        #region Event Handlers
        
        private void OnSlotClicked(GridSlot slot)
        {
            GridItem item = GetItemAt(slot.GridPosition);
            SelectItem(item);
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
            var itemsToRemove = new List<GridItem>(_items);
            foreach (var item in itemsToRemove)
            {
                RemoveItem(item);
            }
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
