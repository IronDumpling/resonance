using UnityEngine;
using System.Collections.Generic;
using Resonance.Player.Core;
using Resonance.Player.Inventory;

namespace Resonance.Utilities
{
    /// <summary>
    /// Grid中的物品表示
    /// 支持任意尺寸的物品，可以旋转
    /// </summary>
    [System.Serializable]
    public class GridItem
    {
        [Header("Basic Properties")]
        public int itemID;
        public string itemName;
        public ItemType itemType;
        public Sprite itemIcon;
        
        [Header("Grid Properties")]
        public int baseWidth = 1;   // 基础宽度（未旋转时）
        public int baseHeight = 1;  // 基础高度（未旋转时）
        public Vector2Int gridPosition = new Vector2Int(-1, -1); // 在网格中的位置
        public bool isRotated = false; // 是否旋转了90度
        
        [Header("Visual Properties")]
        public GameObject itemPrefab; // Prefab instance to display in grid
        public Color itemColor = Color.white;
        public bool isSelected = false;
        public bool isDragging = false;
        
        [Header("Custom Data")]
        public Dictionary<string, object> customData = new Dictionary<string, object>();
        
        // 计算属性
        public int CurrentWidth => isRotated ? baseHeight : baseWidth;
        public int CurrentHeight => isRotated ? baseWidth : baseHeight;
        public Vector2Int CurrentSize => new Vector2Int(CurrentWidth, CurrentHeight);
        
        // 占用区域
        public List<Vector2Int> OccupiedPositions
        {
            get
            {
                var positions = new List<Vector2Int>();
                if (gridPosition.x < 0 || gridPosition.y < 0) return positions;
                
                for (int x = 0; x < CurrentWidth; x++)
                {
                    for (int y = 0; y < CurrentHeight; y++)
                    {
                        positions.Add(new Vector2Int(gridPosition.x + x, gridPosition.y + y));
                    }
                }
                return positions;
            }
        }
        
        public GridItem(int id, string name, ItemType type, int width = 1, int height = 1)
        {
            itemID = id;
            itemName = name;
            itemType = type;
            baseWidth = width;
            baseHeight = height;
            gridPosition = new Vector2Int(-1, -1);
            customData = new Dictionary<string, object>();
        }
        
        /// <summary>
        /// 旋转物品
        /// </summary>
        /// <param name="clockwise">是否顺时针旋转</param>
        public void Rotate(bool clockwise = true)
        {
            isRotated = !isRotated;
        }
        
        /// <summary>
        /// 设置网格位置
        /// </summary>
        /// <param name="position">新位置</param>
        public void SetGridPosition(Vector2Int position)
        {
            gridPosition = position;
        }
        
        /// <summary>
        /// 检查是否在指定位置
        /// </summary>
        /// <param name="position">要检查的位置</param>
        /// <returns>是否占用该位置</returns>
        public bool OccupiesPosition(Vector2Int position)
        {
            if (gridPosition.x < 0 || gridPosition.y < 0) return false;
            
            return position.x >= gridPosition.x && position.x < gridPosition.x + CurrentWidth &&
                   position.y >= gridPosition.y && position.y < gridPosition.y + CurrentHeight;
        }
        
        /// <summary>
        /// 检查是否与另一个物品重叠
        /// </summary>
        /// <param name="other">另一个物品</param>
        /// <returns>是否重叠</returns>
        public bool OverlapsWith(GridItem other)
        {
            if (other == null) return false;
            
            var myPositions = OccupiedPositions;
            var otherPositions = other.OccupiedPositions;
            
            foreach (var pos in myPositions)
            {
                if (otherPositions.Contains(pos))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取物品的边界框
        /// </summary>
        /// <returns>边界框 (minX, minY, maxX, maxY)</returns>
        public RectInt GetBounds()
        {
            if (gridPosition.x < 0 || gridPosition.y < 0)
                return new RectInt(0, 0, 0, 0);
                
            return new RectInt(gridPosition.x, gridPosition.y, CurrentWidth, CurrentHeight);
        }
        
        /// <summary>
        /// 克隆物品
        /// </summary>
        /// <returns>克隆的物品</returns>
        public GridItem Clone()
        {
            var clone = new GridItem(itemID, itemName, itemType, baseWidth, baseHeight)
            {
                itemIcon = itemIcon,
                gridPosition = gridPosition,
                isRotated = isRotated,
                itemColor = itemColor,
                isSelected = false, // 克隆的物品默认不选中
                isDragging = false,
                customData = new Dictionary<string, object>(customData)
            };
            
            return clone;
        }
    }
}
