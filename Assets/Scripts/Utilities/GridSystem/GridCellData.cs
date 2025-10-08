using UnityEngine;
using Resonance.Utilities;
using System.Collections.Generic;

namespace Resonance.Utilities.GridSystem
{
    /// <summary>
    /// Grid cell data - stores complete information about an item in a single cell
    /// This is a pure data structure, without any business logic
    /// </summary>
    [System.Serializable]
    public class GridCellData
    {
        // Basic information
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public ItemType ItemType { get; set; }
        
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
        
        // Weapon-specific data
        public int CurrentAmmo { get; set; }
        public string AmmoType { get; set; }
        public int MaxAmmo { get; set; }
        
        // Extra data
        public string AssetPath { get; set; }
        public float Durability { get; set; }
        public Dictionary<string, object> CustomData { get; set; }
        
        public GridCellData()
        {
            CustomData = new Dictionary<string, object>();
            GridPosition = new Vector2Int(-1, -1);
            Quantity = 1;
            MaxStackQuantity = 1;
            Durability = 1f;
        }

        /// <summary>
        /// Calculate current width (considering rotation)
        /// </summary>
        public int GetCurrentWidth()
        {
            return (Rotation == 90 || Rotation == 270) ? GridHeight : GridWidth;
        }
        
        /// <summary>
        /// Calculate current height
        /// </summary>
        public int GetCurrentHeight()
        {
            return (Rotation == 90 || Rotation == 270) ? GridWidth : GridHeight;
        }

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
    }
}