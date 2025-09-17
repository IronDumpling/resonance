using UnityEngine;
using Resonance.Items;
using Resonance.Interfaces.Services;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Player.Data;

namespace Resonance.Player.Core
{
    /// <summary>
    /// 管理玩家武器状态的系统
    /// 负责检测玩家是否携带武器，以及当前武器的状态
    /// 与PlayerInventory双向同步
    /// </summary>
    public class WeaponManager
    {
        // 当前装备的武器
        private GunDataAsset _currentGun;
        
        // 与PlayerInventory的同步引用
        private PlayerInventory _inventory;
        
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
        /// 设置PlayerInventory引用以实现双向同步
        /// </summary>
        /// <param name="inventory">PlayerInventory实例</param>
        public void SetInventoryReference(PlayerInventory inventory)
        {
            _inventory = inventory;
            Debug.Log("WeaponManager: Inventory reference set for synchronization");
        }

        /// <summary>
        /// 装备武器（与PlayerInventory同步）
        /// </summary>
        /// <param name="gunData">武器数据</param>
        /// <param name="syncWithInventory">是否与背包同步（默认true）</param>
        public void EquipWeapon(GunDataAsset gunData, bool syncWithInventory = true)
        {
            if (gunData == null)
            {
                Debug.LogWarning("WeaponManager: Trying to equip null weapon");
                return;
            }

            // 如果已有武器，先卸下
            if (_currentGun != null)
            {
                UnequipWeapon(syncWithInventory);
            }

            _currentGun = gunData;
            
            // 与PlayerInventory同步
            if (syncWithInventory && _inventory != null)
            {
                int weaponID = gunData.GetInstanceID();
                
                // 确保武器在背包中
                if (!_inventory.HasWeapon(weaponID))
                {
                    _inventory.AddWeapon(gunData);
                }
                
                // 装备武器
                _inventory.EquipWeapon(weaponID);
            }
            
            // Play weapon equip audio based on weapon type
            PlayWeaponEquipAudio(_currentGun);
            
            OnWeaponEquipped?.Invoke(_currentGun);
            OnAmmoChanged?.Invoke(_currentGun.CurrentAmmo);

            Debug.Log($"WeaponManager: Equipped weapon {_currentGun.weaponName} with {_currentGun.CurrentAmmo}/{_currentGun.maxAmmo} ammo");
        }

        /// <summary>
        /// 卸下武器（与PlayerInventory同步）
        /// </summary>
        /// <param name="syncWithInventory">是否与背包同步（默认true）</param>
        public void UnequipWeapon(bool syncWithInventory = true)
        {
            if (_currentGun == null) return;

            Debug.Log($"WeaponManager: Unequipped weapon {_currentGun.weaponName}");
            
            // 与PlayerInventory同步
            if (syncWithInventory && _inventory != null)
            {
                _inventory.UnequipCurrentWeapon();
            }
            
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
        /// 消耗一发子弹（与PlayerInventory同步）
        /// </summary>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeAmmo()
        {
            if (!HasEquippedWeapon) return false;

            bool consumed = _currentGun.ConsumeAmmo();
            if (consumed)
            {
                // 与PlayerInventory同步弹药数量
                if (_inventory != null)
                {
                    int weaponID = _currentGun.GetInstanceID();
                    _inventory.UpdateWeaponAmmo(weaponID, _currentGun.CurrentAmmo);
                }
                
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
        /// 播放武器装备音效
        /// </summary>
        /// <param name="gunData">武器数据</param>
        private void PlayWeaponEquipAudio(GunDataAsset gunData)
        {
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService == null) return;

            AudioClipType audioClipType = AudioClipType.PistoArming;
            
            if (gunData.ammoType == "Pisto")
            {
                audioClipType = AudioClipType.PistoArming;
            }
            else if (gunData.ammoType == "Rifle")
            {
                audioClipType = AudioClipType.RifleArming;
            }
            
            audioService.PlaySFX2D(audioClipType, 0.8f, 1f);
            Debug.Log($"WeaponManager: Played equip audio for {gunData.weaponName}");
        }

        #region Save/Load System
        
        /// <summary>
        /// 获取武器管理器的保存数据
        /// </summary>
        public WeaponManagerSaveData GetSaveData()
        {
            return new WeaponManagerSaveData
            {
                equippedWeaponID = _currentGun?.GetInstanceID() ?? -1,
                weaponName = _currentGun?.weaponName ?? "",
                currentAmmo = _currentGun?.CurrentAmmo ?? 0,
                maxAmmo = _currentGun?.maxAmmo ?? 0,
                ammoType = _currentGun?.ammoType ?? "",
                assetPath = _currentGun != null ? GetAssetPath(_currentGun) : ""
            };
        }
        
        /// <summary>
        /// 从保存数据加载武器状态
        /// </summary>
        public void LoadFromSaveData(WeaponManagerSaveData saveData)
        {
            if (saveData == null || saveData.equippedWeaponID == -1)
            {
                _currentGun = null;
                return;
            }
            
            // 从PlayerInventory获取武器
            if (_inventory != null)
            {
                var weapon = _inventory.GetEquippedWeapon();
                if (weapon != null && weapon.GetInstanceID() == saveData.equippedWeaponID)
                {
                    _currentGun = weapon;
                    // 恢复弹药状态
                    _currentGun.SetCurrentAmmo(saveData.currentAmmo);
                    
                    OnWeaponEquipped?.Invoke(_currentGun);
                    OnAmmoChanged?.Invoke(_currentGun.CurrentAmmo);
                    
                    Debug.Log($"WeaponManager: Loaded weapon {_currentGun.weaponName} from save data");
                }
            }
        }
        
        /// <summary>
        /// 获取ScriptableObject的资源路径
        /// </summary>
        private string GetAssetPath(ScriptableObject asset)
        {
            if (asset == null) return "";
            
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(asset);
            #else
            return asset.name;
            #endif
        }
        
        #endregion

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            OnWeaponEquipped = null;
            OnWeaponUnequipped = null;
            OnAmmoChanged = null;
            _currentGun = null;
            _inventory = null;
        }
    }

    /// <summary>
    /// WeaponManager的保存数据结构
    /// </summary>
    [System.Serializable]
    public class WeaponManagerSaveData
    {
        public int equippedWeaponID;
        public string weaponName;
        public int currentAmmo;
        public int maxAmmo;
        public string ammoType;
        public string assetPath;
        
        public WeaponManagerSaveData()
        {
            equippedWeaponID = -1;
            weaponName = "";
            currentAmmo = 0;
            maxAmmo = 0;
            ammoType = "";
            assetPath = "";
        }
    }
}
