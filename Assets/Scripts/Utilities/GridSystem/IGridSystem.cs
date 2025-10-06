using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Utilities
{
    /// <summary>
    /// Grid系统接口，定义可复用的网格系统功能
    /// 支持物品放置、移动、旋转等操作
    /// </summary>
    public interface IGridSystem
    {
        // 基础属性
        int GridWidth { get; }
        int GridHeight { get; }
        bool IsInitialized { get; }
        
        // 物品管理
        bool CanPlaceItem(GridItem item, Vector2Int position);
        bool PlaceItem(GridItem item, Vector2Int position);
        bool MoveItem(GridItem item, Vector2Int newPosition);
        bool RotateItem(GridItem item, bool clockwise = true);
        bool RemoveItem(GridItem item);
        GridItem GetItemAt(Vector2Int position);
        List<GridItem> GetAllItems();
        
        // 空间查找
        Vector2Int FindEmptySpace(int width, int height);
        List<Vector2Int> FindEmptySpaces(int width, int height);
        bool IsPositionValid(Vector2Int position);
        bool IsAreaEmpty(Vector2Int position, int width, int height);
        
        // 选择管理
        GridItem SelectedItem { get; }
        void SelectItem(GridItem item);
        void DeselectItem();
        
        // 事件
        System.Action<GridItem> OnItemPlaced { get; set; }
        System.Action<GridItem> OnItemMoved { get; set; }
        System.Action<GridItem> OnItemRotated { get; set; }
        System.Action<GridItem> OnItemRemoved { get; set; }
        System.Action<GridItem> OnItemSelected { get; set; }
        System.Action<GridItem> OnItemDeselected { get; set; }
    }
}
