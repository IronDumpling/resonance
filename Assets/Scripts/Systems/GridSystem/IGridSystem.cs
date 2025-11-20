using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Systems.GridSystem
{
    /// <summary>
    /// Grid system interface - defines reusable grid system functionality
    /// Supports item placement, movement, rotation, etc.
    /// </summary>
    public interface IGridSystem
    {
        // Basic properties
        int GridWidth { get; }
        int GridHeight { get; }
        bool IsInitialized { get; }
        
        // Item management
        bool CanPlaceItem(GridItem item, Vector2Int position);
        bool PlaceItem(GridItem item, Vector2Int position);
        bool MoveItem(GridItem item, Vector2Int newPosition);
        bool RotateItem(GridItem item, bool clockwise = true);
        bool RemoveItem(GridItem item);
        GridItem GetItemAt(Vector2Int position);
        List<GridItem> GetAllItems();
        
        // Space searching
        Vector2Int FindEmptySpace(int width, int height);
        List<Vector2Int> FindEmptySpaces(int width, int height);
        bool IsPositionValid(Vector2Int position);
        bool IsAreaEmpty(Vector2Int position, int width, int height);
        
        // Selection management
        GridItem SelectedItem { get; }
        void SelectItem(GridItem item);
        void DeselectItem();
        
        // Events
        System.Action<GridItem> OnItemPlaced { get; set; }
        System.Action<GridItem> OnItemMoved { get; set; }
        System.Action<GridItem> OnItemRotated { get; set; }
        System.Action<GridItem> OnItemRemoved { get; set; }
        System.Action<GridItem> OnItemSelected { get; set; }
        System.Action<GridItem> OnItemDeselected { get; set; }
    }
}
