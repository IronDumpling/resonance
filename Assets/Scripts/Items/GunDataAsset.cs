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
        
        // Runtime state - not serialized, reset each time
        [System.NonSerialized] private int _currentAmmo = -1; // -1 means not initialized
        
        [Header("Combat Stats")]
        public float damage = 25f;
        public float range = 100f;
        public float fireRate = 1f; // shots per second
        
        [Header("Visual")]
        public Sprite weaponIcon;
        
        [Header("Inventory")]
        public int gridWidth = 2;
        public int gridHeight = 3;

        // Runtime Properties
        public int CurrentAmmo 
        { 
            get 
            { 
                if (_currentAmmo == -1) _currentAmmo = maxAmmo; // Initialize on first access
                return _currentAmmo; 
            } 
            set { _currentAmmo = value; } 
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

        #region Runtime Methods

        /// <summary>
        /// 检查是否有弹药
        /// </summary>
        /// <returns>是否有弹药</returns>
        public bool HasAmmo()
        {
            return CurrentAmmo > 0;
        }

        /// <summary>
        /// 检查是否是满弹药
        /// </summary>
        /// <returns>是否满弹药</returns>
        public bool IsFullAmmo()
        {
            return CurrentAmmo >= maxAmmo;
        }

        /// <summary>
        /// 获取弹药百分比
        /// </summary>
        /// <returns>弹药百分比 (0-1)</returns>
        public float GetAmmoPercentage()
        {
            if (maxAmmo <= 0) return 0f;
            return (float)CurrentAmmo / maxAmmo;
        }

        /// <summary>
        /// 重置弹药到满弹药状态
        /// </summary>
        public void ResetAmmo()
        {
            CurrentAmmo = maxAmmo;
        }

        /// <summary>
        /// 消耗一发弹药
        /// </summary>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeAmmo()
        {
            if (CurrentAmmo > 0)
            {
                CurrentAmmo--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 创建这个武器的独立副本（用于拾取时给玩家）
        /// 注意：这会创建一个新的ScriptableObject实例，用于运行时独立的武器状态
        /// </summary>
        /// <returns>武器副本</returns>
        public GunDataAsset CreateRuntimeCopy()
        {
            var copy = ScriptableObject.CreateInstance<GunDataAsset>();
            
            // Copy all properties
            copy.weaponName = this.weaponName;
            copy.weaponDescription = this.weaponDescription;
            copy.maxAmmo = this.maxAmmo;
            copy.ammoType = this.ammoType;
            copy.damage = this.damage;
            copy.range = this.range;
            copy.fireRate = this.fireRate;
            copy.weaponIcon = this.weaponIcon;
            copy.gridWidth = this.gridWidth;
            copy.gridHeight = this.gridHeight;
            
            // Initialize runtime state
            copy._currentAmmo = this.maxAmmo; // Start with full ammo
            
            return copy;
        }

        #endregion

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
