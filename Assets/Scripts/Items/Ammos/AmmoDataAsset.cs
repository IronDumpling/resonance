using UnityEngine;
using Resonance.Interfaces;
using Resonance.Interfaces.Objects;
using Resonance.Items.Core;

namespace Resonance.Items
{
    /// <summary>
    /// Ammo数据的ScriptableObject资产
    /// 用于在Unity Editor中创建和编辑Ammo配置
    /// </summary>
    [CreateAssetMenu(fileName = "New Ammo Data", menuName = "Resonance/Items/Ammo Data", order = 2)]
    public class AmmoDataAsset : ScriptableObject, IInfoable
    {
        [Header("Basic Info")]
        public string ammoName = "Standard Ammo";
        [TextArea(2, 4)]
        public string ammoDescription = "Standard ammunition for firearms";
        
        [Header("Ammo Properties")]
        public string ammoType = "Pisto";
        public int ammoCount = 4;
        
        [Header("Visual")]
        public Sprite ammoIcon;
        public GameObject itemPrefab;
        
        [Header("Inventory")]
        public int gridWidth = 1;
        public int gridHeight = 1;
        public int maxStackQuantity = 60;

        /// <summary>
        /// Validate Ammo data
        /// </summary>
        /// <returns>Validation result</returns>
        public bool ValidateData()
        {
            if (string.IsNullOrEmpty(ammoName))
            {
                Debug.LogError($"AmmoDataAsset: {name} has empty ammo name");
                return false;
            }

            if (string.IsNullOrEmpty(ammoType))
            {
                Debug.LogError($"AmmoDataAsset: {ammoName} has empty ammo type");
                return false;
            }

            if (ammoCount <= 0)
            {
                Debug.LogError($"AmmoDataAsset: {ammoName} has invalid ammo count: {ammoCount}");
                return false;
            }

            if (gridWidth <= 0 || gridHeight <= 0)
            {
                Debug.LogError($"AmmoDataAsset: {ammoName} has invalid grid size: {gridWidth}x{gridHeight}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get ammo type display name
        /// </summary>
        /// <returns>Ammo type display name</returns>
        public string GetAmmoTypeDisplayName()
        {
            return ammoType switch
            {
                "Pisto" => "Pisto Ammo",
                "Rifle" => "Rifle Ammo",
                "Sniper" => "Sniper Ammo",
                _ => $"Unknown Type ({ammoType})"
            };
        }

        /// <summary>
        /// Check if compatible with specified weapon type
        /// </summary>
        /// <param name="weaponAmmoType">Weapon ammo type</param>
        /// <returns>Is compatible</returns>
        public bool IsCompatibleWith(string weaponAmmoType)
        {
            return string.Equals(ammoType, weaponAmmoType, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get grid size occupied by inventory
        /// </summary>
        /// <returns>Grid size (width, height)</returns>
        public (int width, int height) GetGridSize()
        {
            return (gridWidth, gridHeight);
        }

        /// <summary>
        /// Get total grid size occupied
        /// </summary>
        /// <returns>Total grid size occupied</returns>
        public int GetTotalGridSize()
        {
            return gridWidth * gridHeight;
        }

        #region IInfoable Implementation

        /// <summary>
        /// Get information data to display in InfoPanel
        /// </summary>
        public InfoData GetInfoData()
        {
            return new InfoData(
                name: ammoName,
                content: ammoDescription,
                image: ammoIcon
            );
        }

        /// <summary>
        /// Check if there is valid information to display
        /// </summary>
        public bool HasValidInfo()
        {
            var info = GetInfoData();
            return info.IsValid();
        }

        #endregion

        #region Unity Editor

        void OnValidate()
        {
            // Ensure values are within reasonable range
            ammoCount = Mathf.Max(1, ammoCount);
            gridWidth = Mathf.Clamp(gridWidth, 1, 10);
            gridHeight = Mathf.Clamp(gridHeight, 1, 10);
            
            // Ensure ammoType is not empty
            if (string.IsNullOrEmpty(ammoType))
            {
                ammoType = "Pisto";
            }
        }

        #endregion
    }
}