using UnityEngine;
using Resonance.Shared.Types;
using System.Collections.Generic;

namespace Resonance.Systems.GridSystem
{
    /// <summary>
    /// Grid cell data - stores complete information about an item in a single cell
    /// Unified data model for both inventory storage and UI display
    /// </summary>
    [System.Serializable]
    public class GridItem
    {
        // Basic information
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public ItemType ItemType { get; set; }
        public ConsumableType ConsumableType { get; set; }
        
        // Stack information
        public int Quantity { get; set; }
        public int MaxStackQuantity { get; set; }
        
        // Grid information
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public int Rotation { get; set; } // 0, 90, 180, 270
        public Vector2Int GridPosition { get; set; } // Starting position

        // Visual data 
        public Sprite ItemIcon { get; set; }
        public GameObject ItemPrefab { get; set; }
        
        // Equipped status
        public bool IsEquipped { get; set; }
        
        // Extra data
        public string AssetPath { get; set; }
        public float Durability { get; set; }
        public Dictionary<string, object> CustomData { get; set; }
        
        // UI State (migrated from GridItem)
        public bool IsSelected { get; set; }
        public bool IsDragging { get; set; }
        public Color ItemColor { get; set; } = Color.white;
        
        public GridItem()
        {
            CustomData = new Dictionary<string, object>();
            GridPosition = new Vector2Int(-1, -1);
            Quantity = 1;
            MaxStackQuantity = 1;
            Durability = 1f;
            ItemColor = Color.white;
            IsSelected = false;
            IsDragging = false;
        }

        #region Computed Properties (migrated from GridItem)
        
        /// <summary>
        /// Calculate current width (considering rotation)
        /// </summary>
        public int GetCurrentWidth()
        {
            return (Rotation == 90 || Rotation == 270) ? GridHeight : GridWidth;
        }
        
        /// <summary>
        /// Calculate current height (considering rotation)
        /// </summary>
        public int GetCurrentHeight()
        {
            return (Rotation == 90 || Rotation == 270) ? GridWidth : GridHeight;
        }
        
        /// <summary>
        /// Compatibility property for GridItem's bool isRotated
        /// </summary>
        public bool IsRotated 
        { 
            get => Rotation == 90 || Rotation == 270;
            set => Rotation = value ? 90 : 0;
        }
        
        /// <summary>
        /// Current size as Vector2Int
        /// </summary>
        public Vector2Int CurrentSize => new Vector2Int(GetCurrentWidth(), GetCurrentHeight());
        
        #endregion
        
        #region Helper Methods (migrated from GridItem)

        /// <summary>
        /// Get all occupied grid positions
        /// </summary>
        public List<Vector2Int> GetOccupiedPositions()
        {
            var positions = new List<Vector2Int>();
            if (GridPosition.x < 0 || GridPosition.y < 0) return positions;
            
            int width = GetCurrentWidth();
            int height = GetCurrentHeight();
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    positions.Add(new Vector2Int(GridPosition.x + x, GridPosition.y + y));
                }
            }
            return positions;
        }
        
        /// <summary>
        /// Rotate item (migrated from GridItem)
        /// </summary>
        /// <param name="clockwise">Whether to rotate clockwise</param>
        public void Rotate(bool clockwise = true)
        {
            Rotation = (Rotation + 90) % 360;
        }
        
        /// <summary>
        /// Set grid position
        /// </summary>
        /// <param name="position">New position</param>
        public void SetGridPosition(Vector2Int position)
        {
            GridPosition = position;
        }
        
        /// <summary>
        /// Check if this item occupies the specified position
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if position is occupied</returns>
        public bool OccupiesPosition(Vector2Int position)
        {
            if (GridPosition.x < 0 || GridPosition.y < 0) return false;
            
            int width = GetCurrentWidth();
            int height = GetCurrentHeight();
            
            return position.x >= GridPosition.x && position.x < GridPosition.x + width &&
                   position.y >= GridPosition.y && position.y < GridPosition.y + height;
        }
        
        /// <summary>
        /// Check if this item overlaps with another item
        /// </summary>
        /// <param name="other">Other item</param>
        /// <returns>True if overlapping</returns>
        public bool OverlapsWith(GridItem other)
        {
            if (other == null) return false;
            
            var myPositions = GetOccupiedPositions();
            var otherPositions = other.GetOccupiedPositions();
            
            foreach (var pos in myPositions)
            {
                if (otherPositions.Contains(pos))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get bounding box of this item
        /// </summary>
        /// <returns>Bounding rectangle (minX, minY, maxX, maxY)</returns>
        public RectInt GetBounds()
        {
            if (GridPosition.x < 0 || GridPosition.y < 0)
                return new RectInt(0, 0, 0, 0);
                
            return new RectInt(GridPosition.x, GridPosition.y, GetCurrentWidth(), GetCurrentHeight());
        }
        
        #endregion
    }
}