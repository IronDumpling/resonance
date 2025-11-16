using UnityEngine;
using Resonance.Shared.Interfaces;
using Resonance.Shared.Interfaces.Objects;
using Resonance.Gameplay.Items.Core;

namespace Resonance.Gameplay.Items
{
    /// <summary>
    /// Energy Bottle Data Asset
    /// Consumable item that restores Crystal Core Energy
    /// </summary>
    [CreateAssetMenu(fileName = "New Energy Bottle Data", menuName = "Resonance/Items/Energy Bottle Data", order = 3)]
    public class EnergyBottleDataAsset : ScriptableObject, IInfoable
    {
        [Header("Basic Info")]
        public string itemName = "Energy Bottle";
        [TextArea(2, 4)]
        public string itemDescription = "A bottle filled with crystalline energy";
        
        [Header("Visual")]
        public Sprite itemIcon;
        public GameObject itemPrefab;
        
        [Header("Energy Restoration")]
        [Tooltip("Amount of Crystal Core Energy restored when consumed")]
        [Range(10f, 100f)]
        public float energyRestoreAmount = 30f;
        
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
                content: $"{itemDescription}\n\nRestores: {energyRestoreAmount} Energy",
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
            energyRestoreAmount = Mathf.Max(1f, energyRestoreAmount);
            gridWidth = Mathf.Clamp(gridWidth, 1, 5);
            gridHeight = Mathf.Clamp(gridHeight, 1, 5);
            maxStackQuantity = Mathf.Max(1, maxStackQuantity);
        }
    }
}

