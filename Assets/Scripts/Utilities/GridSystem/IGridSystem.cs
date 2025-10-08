using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Utilities.GridSystem
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
        bool CanPlaceItem(GridCellData item, Vector2Int position);
        bool PlaceItem(GridCellData item, Vector2Int position);
        bool MoveItem(GridCellData item, Vector2Int newPosition);
        bool RotateItem(GridCellData item, bool clockwise = true);
        bool RemoveItem(GridCellData item);
        GridCellData GetItemAt(Vector2Int position);
        List<GridCellData> GetAllItems();
        
        // Space searching
        Vector2Int FindEmptySpace(int width, int height);
        List<Vector2Int> FindEmptySpaces(int width, int height);
        bool IsPositionValid(Vector2Int position);
        bool IsAreaEmpty(Vector2Int position, int width, int height);
        
        // Selection management
        GridCellData SelectedItem { get; }
        void SelectItem(GridCellData item);
        void DeselectItem();
        
        // Events
        System.Action<GridCellData> OnItemPlaced { get; set; }
        System.Action<GridCellData> OnItemMoved { get; set; }
        System.Action<GridCellData> OnItemRotated { get; set; }
        System.Action<GridCellData> OnItemRemoved { get; set; }
        System.Action<GridCellData> OnItemSelected { get; set; }
        System.Action<GridCellData> OnItemDeselected { get; set; }
    }
}
