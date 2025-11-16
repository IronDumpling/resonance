using UnityEngine;
using Resonance.Shared.Interfaces;
using Resonance.Shared.Interfaces.Objects;
using Resonance.Gameplay.Items.Core;

namespace Resonance.Gameplay.Items
{
    /// <summary>
    /// Healant Data Asset
    /// Consumable item that restores Crystal Core Health
    /// </summary>
    [CreateAssetMenu(fileName = "New Healant Data", menuName = "Resonance/Items/Healant Data", order = 4)]
    public class HealantDataAsset : ScriptableObject, IInfoable
    {
        [Header("Basic Info")]
        public string itemName = "Healant";
        [TextArea(2, 4)]
        public string itemDescription = "A medical item that repairs crystal core damage";
        
        [Header("Visual")]
        public Sprite itemIcon;
        public GameObject itemPrefab;
        
        [Header("Core Health Restoration")]
        [Tooltip("Amount of Crystal Core Health restored when consumed")]
        [Range(10f, 100f)]
        public float coreHealthRestoreAmount = 25f;
        
        [Header("Inventory")]
        public int gridWidth = 1;
        public int gridHeight = 1;
        public int maxStackQuantity = 5;

        /// <summary>
        /// Get information data to display in InfoPanel
        /// </summary>
        public InfoData GetInfoData()
        {
            return new InfoData(
                name: itemName,
                content: $"{itemDescription}\n\nRestores: {coreHealthRestoreAmount} Core Health",
                image: itemIcon
            );
        }

        /// <summary>
        /// Check if there is valid information to display
        /// </summary>
        public bool HasValidInfo()
        {
            return GetInfoData().IsValid();
        }
        
        /// <summary>
        /// Validate data
        /// </summary>
        void OnValidate()
        {
            coreHealthRestoreAmount = Mathf.Max(1f, coreHealthRestoreAmount);
            gridWidth = Mathf.Clamp(gridWidth, 1, 5);
            gridHeight = Mathf.Clamp(gridHeight, 1, 5);
            maxStackQuantity = Mathf.Max(1, maxStackQuantity);
        }
    }
}