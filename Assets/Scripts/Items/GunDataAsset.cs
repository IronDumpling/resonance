using UnityEngine;

namespace Resonance.Items
{
    /// <summary>
    /// Gun数据的ScriptableObject资产
    /// 用于在Unity Editor中创建和编辑Gun配置
    /// </summary>
    [CreateAssetMenu(fileName = "New Gun Data", menuName = "Resonance/Items/Gun Data", order = 1)]
    public class GunDataAsset : ScriptableObject
    {
        [Header("Basic Info")]
        public string weaponName = "Basic Gun";
        [TextArea(2, 4)]
        public string weaponDescription = "A basic firearm";
        
        [Header("Ammo")]
        public int maxAmmo = 8;
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
        /// 创建GunData实例
        /// </summary>
        /// <returns>GunData实例</returns>
        public GunData CreateGunData()
        {
            return new GunData
            {
                weaponName = this.weaponName,
                weaponDescription = this.weaponDescription,
                maxAmmo = this.maxAmmo,
                currentAmmo = this.maxAmmo, // 默认满弹药
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
        /// 验证Gun数据是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public bool ValidateData()
        {
            if (string.IsNullOrEmpty(weaponName))
            {
                Debug.LogError($"GunDataAsset: {name} has empty weapon name");
                return false;
            }

            if (maxAmmo <= 0)
            {
                Debug.LogError($"GunDataAsset: {weaponName} has invalid max ammo: {maxAmmo}");
                return false;
            }

            if (damage <= 0)
            {
                Debug.LogError($"GunDataAsset: {weaponName} has invalid damage: {damage}");
                return false;
            }

            if (range <= 0)
            {
                Debug.LogError($"GunDataAsset: {weaponName} has invalid range: {range}");
                return false;
            }

            if (fireRate <= 0)
            {
                Debug.LogError($"GunDataAsset: {weaponName} has invalid fire rate: {fireRate}");
                return false;
            }

            return true;
        }

        #region Unity Editor

        void OnValidate()
        {
            // 确保数值在合理范围内
            maxAmmo = Mathf.Max(1, maxAmmo);
            damage = Mathf.Max(0.1f, damage);
            range = Mathf.Max(1f, range);
            fireRate = Mathf.Max(0.1f, fireRate);
            gridWidth = Mathf.Clamp(gridWidth, 1, 10);
            gridHeight = Mathf.Clamp(gridHeight, 1, 10);
        }

        #endregion
    }
}
