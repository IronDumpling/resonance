using UnityEngine;
using UnityEngine.UI;
using Resonance.Gameplay.Player.Core;
using Resonance.Gameplay.Player.Inventory;
using Resonance.Utilities.Types;
using System.Collections.Generic;
using System.Linq;

namespace Resonance.Utilities.GridSystem
{
    /// <summary>
    /// Reusable grid system implementation
    /// Supports item placement, movement, rotation, etc.
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
        /// Create slot object
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
                    
                    // Set position
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
            
            // Check if the item is out of bounds
            int currentWidth = item.GetCurrentWidth();
            int currentHeight = item.GetCurrentHeight();
            bool xOutOfBounds = position.x + currentWidth > _gridWidth;
            bool yOutOfBounds = position.y + currentHeight > _gridHeight;
            
            Debug.Log($"CanPlaceItem: Item {item.ItemName} at {position}, size {currentWidth}x{currentHeight}");
            Debug.Log($"CanPlaceItem: Grid size {_gridWidth}x{_gridHeight}");
            Debug.Log($"CanPlaceItem: X bounds check: {position.x} + {currentWidth} = {position.x + currentWidth} > {_gridWidth} = {xOutOfBounds}");
            Debug.Log($"CanPlaceItem: Y bounds check: {position.y} + {currentHeight} = {position.y + currentHeight} > {_gridHeight} = {yOutOfBounds}");
            
            if (xOutOfBounds || yOutOfBounds) 
            {
                Debug.Log($"CanPlaceItem: Item {item.ItemName} would be out of bounds");
                return false;
            }
            
            // Check if the area is occupied (excluding the item itself)
            bool areaEmpty = IsAreaEmpty(position, item);
            Debug.Log($"CanPlaceItem: Area empty check result: {areaEmpty}");
            return areaEmpty;
        }
        
        public bool PlaceItem(GridItem item, Vector2Int position)
        {
            Debug.Log($"GridSystem: PlaceItem called for {item?.ItemName ?? "NULL"} at {position}");
            
            // Ensure container exists before placing item
            EnsureItemVisualsContainer();
            
            if (!CanPlaceItem(item, position)) 
            {
                Debug.LogWarning($"GridSystem: Cannot place item {item?.ItemName ?? "NULL"} at {position}");
                return false;
            }
            
            // Remove item from old position
            if (item.GridPosition.x >= 0 && item.GridPosition.y >= 0)
            {
                Debug.Log($"GridSystem: Removing item from old position {item.GridPosition}");
                RemoveItemFromSlots(item);
                // Don't destroy visual here, we'll update it
            }
            
            // Set new position
            item.SetGridPosition(position);
            
            // Add to items list
            if (!_items.Contains(item))
            {
                _items.Add(item);
                Debug.Log($"GridSystem: Added item to _items list");
            }
            
            // Update slots
            UpdateSlotsForItem(item);
            
            // Create or update visual representation
            CreateOrUpdateItemVisual(item);
            
            OnItemPlaced?.Invoke(item);
            Debug.Log($"GridSystem: Placed item {item.ItemName} at {position}");
            
            return true;
        }
        
        public bool MoveItem(GridItem item, Vector2Int newPosition)
        {
            if (item == null || !_items.Contains(item)) return false;
            
            Vector2Int oldPosition = item.GridPosition;
            bool wasSelected = (item == _selectedItem);
            
            // PlaceItem already handles UpdateSlotsForItem and CreateOrUpdateItemVisual
            if (PlaceItem(item, newPosition))
            {
                OnItemMoved?.Invoke(item);
                Debug.Log($"GridSystem: Moved item {item.ItemName} from {oldPosition} to {newPosition}");
                
                // Maintain selection after move
                if (wasSelected)
                {
                    SelectItem(item);
                }
                
                return true;
            }
            
            return false;
        }
        
        public bool RotateItem(GridItem item, bool clockwise = true)
        {
            if (item == null || !_items.Contains(item)) return false;
            
            Vector2Int oldPosition = item.GridPosition;
            int oldWidth = item.GetCurrentWidth();
            int oldHeight = item.GetCurrentHeight();
            bool wasSelected = (item == _selectedItem);
            
            Debug.Log($"GridSystem: RotateItem called - Item: {item.ItemName}, Old position: {oldPosition}, Old size: {oldWidth}x{oldHeight}");
            
            // Clear current slot occupancy
            RemoveItemFromSlots(item);
            
            // Rotate the item (this toggles IsRotated flag)
            item.Rotate(clockwise);
            
            int newWidth = item.GetCurrentWidth();
            int newHeight = item.GetCurrentHeight();
            
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
                Debug.Log($"GridSystem: Successfully rotated {item.ItemName} from {oldPosition} to {newPosition}");
                
                // Maintain selection after rotation
                if (wasSelected)
                {
                    SelectItem(item);
                }
                
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
                Debug.Log($"GridSystem: Rotated {item.ItemName} and adjusted position from {newPosition} to {adjustedPosition}");
                
                // Maintain selection after rotation
                if (wasSelected)
                {
                    SelectItem(item);
                }
                
                return true;
            }
            
            // Rotation failed, restore original state
            item.Rotate(!clockwise);
            item.SetGridPosition(oldPosition);
            UpdateSlotsForItem(item);
            
            Debug.LogWarning($"GridSystem: Cannot rotate item {item.ItemName} - no space available");
            return false;
        }
        
        /// <summary>
        /// Find the best position for a rotated item
        /// </summary>
        private Vector2Int FindBestPositionForRotatedItem(GridItem item, Vector2Int originalPosition)
        {
            // Try to find a suitable position near the original position
            int searchRadius = 2;
            
            for (int radius = 0; radius <= searchRadius; radius++)
            {
                for (int x = originalPosition.x - radius; x <= originalPosition.x + radius; x++)
                {
                    for (int y = originalPosition.y - radius; y <= originalPosition.y + radius; y++)
                    {
                        Vector2Int testPosition = new Vector2Int(x, y);
                        
                        // Check if the position is within the grid
                        if (testPosition.x >= 0 && testPosition.y >= 0 &&
                            testPosition.x + item.GetCurrentWidth() <= _gridWidth &&
                            testPosition.y + item.GetCurrentHeight() <= _gridHeight)
                        {
                            if (IsAreaEmpty(testPosition, item))
                            {
                                return testPosition;
                            }
                        }
                    }
                }
            }
            
            return new Vector2Int(-1, -1); // No suitable position found
        }
        
        public bool RemoveItem(GridItem item)
        {
            if (item == null || !_items.Contains(item)) return false;
            
            RemoveItemFromSlots(item);
            _items.Remove(item);
            
            // Destroy visual representation
            DestroyItemVisual(item);
            
            OnItemRemoved?.Invoke(item);
            Debug.Log($"GridSystem: Removed item {item.ItemName}");
            
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
            
            return new Vector2Int(-1, -1); // No suitable position found
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
                     (excludeItem != null ? $" (excluding {excludeItem.ItemName})" : ""));
            
            for (int x = position.x; x < position.x + width; x++)
            {
                for (int y = position.y; y < position.y + height; y++)
                {
                    Vector2Int checkPos = new Vector2Int(x, y);
                    var itemAtPos = GetItemAt(checkPos);
                    if (itemAtPos != null && itemAtPos != excludeItem)
                    {
                        Debug.Log($"IsAreaEmpty: Position {checkPos} is occupied by {itemAtPos.ItemName}");
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

            int width = item.GetCurrentWidth();
            int height = item.GetCurrentHeight();
            
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
            Debug.Log($"GridSystem: Selected item {_selectedItem.ItemName}");
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
            
            if (_itemVisuals.TryGetValue(item.ItemID, out GridItemVisual visual))
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
        /// Remove item from slots
        /// </summary>
        /// <param name="item">Item to remove</param>
        private void RemoveItemFromSlots(GridItem item)
        {
            if (item.GridPosition.x < 0 || item.GridPosition.y < 0) return;
            
            int currentWidth = item.GetCurrentWidth();
            int currentHeight = item.GetCurrentHeight();
            
            for (int x = item.GridPosition.x; x < item.GridPosition.x + currentWidth; x++)
            {
                for (int y = item.GridPosition.y; y < item.GridPosition.y + currentHeight; y++)
                {
                    if (IsPositionValid(new Vector2Int(x, y)))
                    {
                        _slots[x, y].SetItem(null);
                    }
                }
            }
        }
        
        /// <summary>
        /// Update the slots occupied by the item
        /// </summary>
        /// <param name="item">Item</param>
        private void UpdateSlotsForItem(GridItem item)
        {
            if (item.GridPosition.x < 0 || item.GridPosition.y < 0) return;
            
            int currentWidth = item.GetCurrentWidth();
            int currentHeight = item.GetCurrentHeight();
            
            for (int x = item.GridPosition.x; x < item.GridPosition.x + currentWidth; x++)
            {
                for (int y = item.GridPosition.y; y < item.GridPosition.y + currentHeight; y++)
                {
                    if (IsPositionValid(new Vector2Int(x, y)))
                    {
                        _slots[x, y].SetItem(item);
                    }
                }
            }
        }
        
        /// <summary>
        /// Ensure ItemVisualsContainer exists, if not create it
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
        /// Create or update the visual representation of the item
        /// </summary>
        private void CreateOrUpdateItemVisual(GridItem item)
        {
            Debug.Log($"GridSystem: CreateOrUpdateItemVisual called for item: {item?.ItemName ?? "NULL"}");
            
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
                        Debug.LogError($"GridSystem: Failed to create ItemVisualsContainer. Cannot create visual for {item.ItemName}");
                        return;
                    }
                }
                else
                {
                    // GridSystem not initialized yet - this is fine, visuals will be created when UI opens
                    Debug.LogWarning($"GridSystem: ItemVisualsContainer not initialized yet. Item {item.ItemName} visual will be created when inventory panel opens.");
                    Debug.LogWarning($"GridSystem: _isInitialized = {_isInitialized}, _itemVisualContainer = {(_itemVisualContainer != null ? "EXISTS" : "NULL")}");
                    return;
                }
            }
            
            // Check if visual already exists
            if (_itemVisuals.TryGetValue(item.ItemID, out GridItemVisual existingVisual))
            {
                // Update existing visual
                existingVisual.UpdateSizeAndPosition();
                Debug.Log($"GridSystem: Updated visual for item {item.ItemName}");
                return;
            }

            // Instantiate the item prefab or create fallback
            GameObject visualObj = null;
            GridItemVisual visual = null;
            
            if (item.ItemPrefab == null)
            {
                // Fallback: Create simple visual with icon
                Debug.LogWarning($"GridSystem: No prefab found for {item.ItemName}, creating fallback visual");
                visualObj = new GameObject($"ItemVisual_{item.ItemID}_{item.ItemName}");
                
                // Add Image for icon
                Image iconImage = visualObj.AddComponent<Image>();
                iconImage.sprite = item.ItemIcon;
                iconImage.color = item.ItemColor;
                
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
                visualObj = Instantiate(item.ItemPrefab, _itemVisualContainer);
                visualObj.name = $"ItemVisual_{item.ItemID}_{item.ItemName}";
                
                // Get or add GridItemVisual component
                visual = visualObj.GetComponent<GridItemVisual>();
                if (visual == null)
                {
                    visual = visualObj.AddComponent<GridItemVisual>();
                    Debug.LogWarning($"GridSystem: ItemPrefab for {item.ItemName} doesn't have GridItemVisual component. Adding it.");
                }
            }

            // Subscribe to click event
            visual.OnItemClicked += OnItemVisualClicked;
            
            // Initialize the visual
            visual.Initialize(item, _slotSize, _itemVisualContainer);
            
            // Store reference
            _itemVisuals[item.ItemID] = visual;
            
            Debug.Log($"GridSystem: Created visual for item {item.ItemName} (HasPrefab: {item.ItemPrefab != null}, Position: {item.GridPosition})");
        }
        
        /// <summary>
        /// Destroy the visual representation of the item
        /// </summary>
        private void DestroyItemVisual(GridItem item)
        {
            if (item == null) return;
            
            if (_itemVisuals.TryGetValue(item.ItemID, out GridItemVisual visual))
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
                
                _itemVisuals.Remove(item.ItemID);
                if (visual != null)
                {
                    Destroy(visual.gameObject);
                }
                Debug.Log($"GridSystem: Destroyed visual for item {item.ItemName}");
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
            // Can add hover effect here
        }
        
        private void OnSlotUnhovered(GridSlot slot)
        {
            // Can remove hover effect here
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Clear all items
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
        /// Get items by type
        /// </summary>
        /// <param name="itemType">Item type</param>
        /// <returns>Item list</returns>
        public List<GridItem> GetItemsByType(ItemType itemType)
        {
            return _items.Where(item => item.ItemType == itemType).ToList();
        }
        
        /// <summary>
        /// Check if there is an item with the specified ID
        /// </summary>
        /// <param name="itemID">Item ID</param>
        /// <returns>Whether it exists</returns>
        public bool HasItem(int itemID)
        {
            return _items.Any(item => item.ItemID == itemID);
        }
        
        /// <summary>
        /// Get item by ID
        /// </summary>
        /// <param name="itemID">Item ID</param>
        /// <returns>Item</returns>
        public GridItem GetItemByID(int itemID)
        {
            return _items.FirstOrDefault(item => item.ItemID == itemID);
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnDestroy()
        {
            // Clean up events
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
