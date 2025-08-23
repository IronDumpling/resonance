using UnityEngine;

namespace Resonance.Items
{
    /// <summary>
    /// Gun的数据结构，包含武器的所有属性
    /// 
    /// Visual System Notes:
    /// - weaponIcon: Used for UI/inventory display
    /// - No equipped weapon prefab field - visual representation when equipped is handled separately
    /// - This data structure focuses on gameplay stats and UI elements
    /// </summary>
    [System.Serializable]
    public class GunData
    {
        [Header("Basic Info")]
        public string weaponName = "Basic Gun";
        public string weaponDescription = "A basic firearm";
        
        [Header("Ammo")]
        public int maxAmmo = 8;
        public int currentAmmo = 8;
        public string ammoType = "TypeA";
        
        [Header("Combat Stats")]
        public float damage = 25f;
        public float range = 100f;
        public float fireRate = 1f; // shots per second
        
        [Header("Visual")]
        public Sprite weaponIcon;
        
        [Header("Inventory")]
        public int gridWidth = 2;
        public int gridHeight = 3;

        /// <summary>
        /// 创建Gun数据的副本（用于拾取时）
        /// </summary>
        /// <returns>Gun数据副本</returns>
        public GunData Clone()
        {
            return new GunData
            {
                weaponName = this.weaponName,
                weaponDescription = this.weaponDescription,
                maxAmmo = this.maxAmmo,
                currentAmmo = this.currentAmmo,
                ammoType = this.ammoType,
                damage = this.damage,
                range = this.range,
                fireRate = this.fireRate,
                weaponIcon = this.weaponIcon,
                gridWidth = this.gridWidth,
                gridHeight = this.gridHeight
            };
        }

        /// <summary>
        /// 检查是否有弹药
        /// </summary>
        /// <returns>是否有弹药</returns>
        public bool HasAmmo()
        {
            return currentAmmo > 0;
        }

        /// <summary>
        /// 检查是否是满弹药
        /// </summary>
        /// <returns>是否满弹药</returns>
        public bool IsFullAmmo()
        {
            return currentAmmo >= maxAmmo;
        }

        /// <summary>
        /// 获取弹药百分比
        /// </summary>
        /// <returns>弹药百分比 (0-1)</returns>
        public float GetAmmoPercentage()
        {
            if (maxAmmo <= 0) return 0f;
            return (float)currentAmmo / maxAmmo;
        }
    }
}
