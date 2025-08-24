using UnityEngine;
using Resonance.Items;

namespace Resonance.Player.Core
{
    /// <summary>
    /// 管理玩家武器状态的系统
    /// 负责检测玩家是否携带武器，以及当前武器的状态
    /// </summary>
    public class WeaponManager
    {
        // 当前装备的武器
        private GunDataAsset _currentGun;
        
        // 事件
        public System.Action<GunDataAsset> OnWeaponEquipped;
        public System.Action OnWeaponUnequipped;
        public System.Action<int> OnAmmoChanged;

        // 属性
        public bool HasEquippedWeapon => _currentGun != null;
        public GunDataAsset CurrentGun => _currentGun;
        public int CurrentAmmo => _currentGun?.CurrentAmmo ?? 0;
        public int MaxAmmo => _currentGun?.maxAmmo ?? 0;
        public string AmmoType => _currentGun?.ammoType ?? "None";

        /// <summary>
        /// 装备武器
        /// </summary>
        /// <param name="gunData">武器数据</param>
        public void EquipWeapon(GunDataAsset gunData)
        {
            if (gunData == null)
            {
                Debug.LogWarning("WeaponManager: Trying to equip null weapon");
                return;
            }

            // 如果已有武器，先卸下
            if (_currentGun != null)
            {
                UnequipWeapon();
            }

            _currentGun = gunData;
            OnWeaponEquipped?.Invoke(_currentGun);
            OnAmmoChanged?.Invoke(_currentGun.CurrentAmmo);

            Debug.Log($"WeaponManager: Equipped weapon {_currentGun.weaponName} with {_currentGun.CurrentAmmo}/{_currentGun.maxAmmo} ammo");
        }

        /// <summary>
        /// 卸下武器
        /// </summary>
        public void UnequipWeapon()
        {
            if (_currentGun == null) return;

            Debug.Log($"WeaponManager: Unequipped weapon {_currentGun.weaponName}");
            
            _currentGun = null;
            OnWeaponUnequipped?.Invoke();
            OnAmmoChanged?.Invoke(0);
        }

        /// <summary>
        /// 检查是否可以射击
        /// </summary>
        /// <returns>是否可以射击</returns>
        public bool CanShoot()
        {
            return HasEquippedWeapon && _currentGun.HasAmmo();
        }

        /// <summary>
        /// 消耗一发子弹
        /// </summary>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeAmmo()
        {
            if (!HasEquippedWeapon) return false;

            bool consumed = _currentGun.ConsumeAmmo();
            if (consumed)
            {
                OnAmmoChanged?.Invoke(_currentGun.CurrentAmmo);
                Debug.Log($"WeaponManager: Ammo consumed. Remaining: {_currentGun.CurrentAmmo}/{_currentGun.maxAmmo}");
            }
            
            return consumed;
        }

        /// <summary>
        /// 重新装填（恢复满弹药）
        /// </summary>
        public void Reload()
        {
            if (_currentGun == null) return;

            _currentGun.ResetAmmo();
            OnAmmoChanged?.Invoke(_currentGun.CurrentAmmo);

            Debug.Log($"WeaponManager: Reloaded. Ammo: {_currentGun.CurrentAmmo}/{_currentGun.maxAmmo}");
        }

        /// <summary>
        /// 获取武器信息（用于调试和UI显示）
        /// </summary>
        /// <returns>武器信息字符串</returns>
        public string GetWeaponInfo()
        {
            if (!HasEquippedWeapon) return "No Weapon";
            
            return $"{_currentGun.weaponName} ({_currentGun.CurrentAmmo}/{_currentGun.maxAmmo} {_currentGun.ammoType})";
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            OnWeaponEquipped = null;
            OnWeaponUnequipped = null;
            OnAmmoChanged = null;
            _currentGun = null;
        }
    }
}
