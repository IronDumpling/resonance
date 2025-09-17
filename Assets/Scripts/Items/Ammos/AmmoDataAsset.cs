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
        public int ammoCount = 30;
        
        [Header("Visual & Info")]
        public Sprite ammoIcon;
        
        [Header("Info Display")]
        [SerializeField] private InfoData _infoData;
        
        [Header("Inventory")]
        public int gridWidth = 1;
        public int gridHeight = 1;

        /// <summary>
        /// 验证Ammo数据是否有效
        /// </summary>
        /// <returns>验证结果</returns>
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
        /// 获取弹药类型的显示名称
        /// </summary>
        /// <returns>弹药类型显示名称</returns>
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
        /// 检查是否与指定武器类型兼容
        /// </summary>
        /// <param name="weaponAmmoType">武器的弹药类型</param>
        /// <returns>是否兼容</returns>
        public bool IsCompatibleWith(string weaponAmmoType)
        {
            return string.Equals(ammoType, weaponAmmoType, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取库存占用的网格大小
        /// </summary>
        /// <returns>网格大小 (width, height)</returns>
        public (int width, int height) GetGridSize()
        {
            return (gridWidth, gridHeight);
        }

        /// <summary>
        /// 获取总的网格占用数量
        /// </summary>
        /// <returns>占用的网格数量</returns>
        public int GetTotalGridSize()
        {
            return gridWidth * gridHeight;
        }

        #region IInfoable Implementation

        /// <summary>
        /// 获取要在InfoPanel中显示的信息数据
        /// </summary>
        public InfoData GetInfoData()
        {
            // 如果没有设置自定义信息，使用基本信息
            if (_infoData.IsEmpty)
            {
                return new InfoData(
                    name: ammoName,
                    content: ammoDescription,
                    image: ammoIcon
                );
            }
            
            return _infoData;
        }

        /// <summary>
        /// 检查是否有有效的信息可以显示
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
            // 确保数值在合理范围内
            ammoCount = Mathf.Max(1, ammoCount);
            gridWidth = Mathf.Clamp(gridWidth, 1, 10);
            gridHeight = Mathf.Clamp(gridHeight, 1, 10);
            
            // 确保ammoType不为空
            if (string.IsNullOrEmpty(ammoType))
            {
                ammoType = "Pisto";
            }
        }

        #endregion
    }
}