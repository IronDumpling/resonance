using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Resonance.Player.Data;
using Resonance.Items;

namespace Resonance.Player.Core
{
    /// <summary>
    /// 物品类型枚举
    /// </summary>
    public enum ItemType
    {
        Consumable,    // 消耗品（弹药等）
        Tool,          // 道具（钥匙等）
        Module,        // 晶体模块
        Weapon         // 武器
    }

    /// <summary>
    /// 扩展的物品数据结构
    /// 支持不同类型物品的统一管理
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        // 基础属性
        public int ItemID { get; set; }
        public ItemType ItemType { get; set; }
        public int Quantity { get; set; }
        public float Durability { get; set; }
        
        // 格子系统属性（预留）
        public int GridWidth { get; set; }   // 物品占用格子宽度
        public int GridHeight { get; set; }  // 物品占用格子高度
        public Vector2Int GridPosition { get; set; } // 在格子中的位置（-1,-1表示未放置）
        
        // 武器特有属性
        public string AssetPath { get; set; }     // ScriptableObject资源路径
        public int CurrentAmmo { get; set; }      // 当前弹药数
        public Dictionary<string, object> CustomData { get; set; } // 自定义数据
        
        public InventoryItem(int itemID, ItemType itemType, int quantity = 1, float durability = 1f)
        {
            ItemID = itemID;
            ItemType = itemType;
            Quantity = quantity;
            Durability = durability;
            GridWidth = 1;
            GridHeight = 1;
            GridPosition = new Vector2Int(-1, -1); // 未放置状态
            CustomData = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 统一背包系统的保存数据结构
    /// </summary>
    [System.Serializable]
    public class InventorySaveData
    {
        public List<ItemSaveData> items;
        public List<int> equippedItemIDs;
        public int equippedWeaponID;
        public Dictionary<string, int> ammoInventory; // 弹药库存
        
        public InventorySaveData()
        {
            items = new List<ItemSaveData>();
            equippedItemIDs = new List<int>();
            equippedWeaponID = -1;
            ammoInventory = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// 统一物品的保存数据
    /// </summary>
    [System.Serializable]
    public class ItemSaveData
    {
        public int itemID;
        public ItemType itemType;
        public int quantity;
        public float durability;
        public Vector2Int gridPosition;
        public int currentAmmo;
        public string assetPath;
        public Dictionary<string, object> customData;
    }
    /// <summary>
    /// 统一的玩家背包系统
    /// 管理所有类型的物品：消耗品、道具、晶体模块、武器
    /// 分为逻辑存储层和格子系统层
    /// </summary>
    public class PlayerInventory
    {
        #region 逻辑存储层 - Logical Storage Layer
        
        // 统一物品存储
        private List<InventoryItem> _items;
        private List<int> _equippedItemIDs;
        
        // 当前装备的武器ID（与WeaponManager同步）
        private int _equippedWeaponID = -1;
        
        // 容量限制
        private int _maxSlots;

        // 扩展事件系统
        public System.Action OnInventoryChanged;
        public System.Action<int> OnItemEquipped; // itemID (向后兼容)
        public System.Action<int> OnItemUnequipped; // itemID (向后兼容)
        public System.Action<int, ItemType> OnItemAdded; // itemID, itemType
        public System.Action<int, ItemType> OnItemRemoved; // itemID, itemType
        public System.Action<int> OnWeaponEquipped; // weaponID
        public System.Action OnWeaponUnequipped;
        
        #endregion
        
        #region 格子系统层 - Grid System Layer (预留)
        
        // 格子系统相关字段（预留）
        private int _gridWidth = 10;   // 背包格子宽度
        private int _gridHeight = 6;   // 背包格子高度
        private bool[,] _gridOccupied; // 格子占用状态
        
        // 格子系统方法（预留接口）
        public bool CanPlaceItemAt(InventoryItem item, Vector2Int position) 
        { 
            // TODO: 实现格子放置逻辑
            return true; 
        }
        
        public bool PlaceItemAt(InventoryItem item, Vector2Int position) 
        { 
            // TODO: 实现格子放置逻辑
            return true; 
        }
        
        public void RemoveItemFromGrid(InventoryItem item) 
        { 
            // TODO: 实现格子移除逻辑
        }
        
        public Vector2Int FindEmptySpace(int width, int height) 
        { 
            // TODO: 实现自动寻找空位逻辑
            return Vector2Int.zero; 
        }
        
        #endregion

        // Properties
        public int MaxSlots => _gridWidth * _gridHeight;
        public int UsedSlots => _items?.Count ?? 0;
        public bool IsFull => UsedSlots >= MaxSlots;

        public PlayerInventory(int gridWidth, int gridHeight)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            
            // 初始化存储系统   
            _items = new List<InventoryItem>();
            _equippedItemIDs = new List<int>();
            
            // 初始化格子系统（预留）
            _gridOccupied = new bool[_gridWidth, _gridHeight];
        }

        #region 武器管理 - Weapon Management
        
        /// <summary>
        /// 添加武器到背包
        /// </summary>
        public bool AddWeapon(GunDataAsset weaponAsset)
        {
            if (weaponAsset == null) 
            {
                Debug.LogError("PlayerInventory: Cannot add null weapon");
                return false;
            }
            
            int weaponID = weaponAsset.GetInstanceID();
            Debug.Log($"🎒 [INVENTORY] AddWeapon called for {weaponAsset.weaponName} (ID: {weaponID})");
            
            // 检查是否已存在
            if (HasWeapon(weaponID))
            {
                Debug.LogWarning($"PlayerInventory: Weapon {weaponAsset.weaponName} already in inventory");
                return false;
            }
            
            // 检查背包空间
            if (IsFull)
            {
                Debug.LogWarning("PlayerInventory: Cannot add weapon - inventory full");
                return false;
            }
            
            // 创建武器物品
            var weaponItem = new InventoryItem(weaponID, ItemType.Weapon, 1, 1f)
            {
                AssetPath = GetAssetPath(weaponAsset),
                CurrentAmmo = weaponAsset.CurrentAmmo,
                GridWidth = weaponAsset.gridWidth,
                GridHeight = weaponAsset.gridHeight
            };
            
            // 武器特有数据
            weaponItem.CustomData["weaponName"] = weaponAsset.weaponName;
            weaponItem.CustomData["ammoType"] = weaponAsset.ammoType;
            weaponItem.CustomData["maxAmmo"] = weaponAsset.maxAmmo;
            weaponItem.CustomData["originalAsset"] = weaponAsset; // 保存原始引用
            
            Debug.Log($"🎒 [INVENTORY] Created weapon item: {weaponItem.ItemID}, AssetPath: {weaponItem.AssetPath}");
            
            _items.Add(weaponItem);
            OnItemAdded?.Invoke(weaponID, ItemType.Weapon);
            OnInventoryChanged?.Invoke();
            
            Debug.Log($"PlayerInventory: Added weapon {weaponAsset.weaponName} to inventory");
            return true;
        }
        
        /// <summary>
        /// 装备武器（与WeaponManager同步）
        /// </summary>
        public bool EquipWeapon(int weaponID)
        {
            Debug.Log($"🎒 [INVENTORY] EquipWeapon called for weaponID: {weaponID}");
            
            if (!HasWeapon(weaponID))
            {
                Debug.LogWarning($"PlayerInventory: Cannot equip weapon {weaponID} - not in inventory");
                return false;
            }
            
            // 卸下当前武器
            if (_equippedWeaponID != -1)
            {
                Debug.Log($"🎒 [INVENTORY] Unequipping current weapon: {_equippedWeaponID}");
                UnequipCurrentWeapon();
            }
            
            _equippedWeaponID = weaponID;
            if (!_equippedItemIDs.Contains(weaponID))
            {
                _equippedItemIDs.Add(weaponID);
            }
            
            OnWeaponEquipped?.Invoke(weaponID);
            OnItemEquipped?.Invoke(weaponID); // 向后兼容
            Debug.Log($"PlayerInventory: Equipped weapon {weaponID}");
            return true;
        }
        
        /// <summary>
        /// 卸下当前武器
        /// </summary>
        public void UnequipCurrentWeapon()
        {
            if (_equippedWeaponID == -1) return;
            
            _equippedItemIDs.Remove(_equippedWeaponID);
            int oldWeaponID = _equippedWeaponID;
            _equippedWeaponID = -1;
            
            OnWeaponUnequipped?.Invoke();
            OnItemUnequipped?.Invoke(oldWeaponID); // 向后兼容
            Debug.Log("PlayerInventory: Unequipped current weapon");
        }
        
        /// <summary>
        /// 获取当前装备的武器
        /// </summary>
        public GunDataAsset GetEquippedWeapon()
        {
            if (_equippedWeaponID == -1)  return null;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
            }
            
            var weaponItem = _items.FirstOrDefault(item => 
                item.ItemID == _equippedWeaponID && item.ItemType == ItemType.Weapon);
            
            if (weaponItem == null) 
            {
                Debug.LogWarning($"PlayerInventory: Weapon not found! Looking for ID {_equippedWeaponID} with type Weapon");
                return null;
            }
            
            // 尝试从CustomData中获取原始GunDataAsset引用
            if (weaponItem.CustomData.ContainsKey("originalAsset") && weaponItem.CustomData["originalAsset"] is GunDataAsset originalAsset)
            {
                return originalAsset;
            }
            
            // 如果CustomData中没有，尝试从路径加载
            var gunAsset = LoadAssetFromPath<GunDataAsset>(weaponItem.AssetPath);
            
            return gunAsset;
        }
        
        /// <summary>
        /// 更新武器弹药数量（由WeaponManager调用）
        /// </summary>
        public void UpdateWeaponAmmo(int weaponID, int currentAmmo)
        {
            var weaponItem = _items.FirstOrDefault(item => 
                item.ItemID == weaponID && item.ItemType == ItemType.Weapon);
            
            if (weaponItem != null)
            {
                weaponItem.CurrentAmmo = currentAmmo;
                Debug.Log($"PlayerInventory: Updated weapon {weaponID} ammo to {currentAmmo}");
            }
        }
        
        public bool HasWeapon(int weaponID) => _items.Any(item => 
            item.ItemID == weaponID && item.ItemType == ItemType.Weapon);
        
        public int GetEquippedWeaponID() => _equippedWeaponID;
        
        /// <summary>
        /// 获取所有拥有的武器
        /// </summary>
        public List<InventoryItem> GetAllWeapons()
        {
            return _items.Where(item => item.ItemType == ItemType.Weapon).ToList();
        }
        
        #endregion

        #region 消耗品管理 - Consumable Management
        
        /// <summary>
        /// 添加消耗品到背包
        /// </summary>
        public bool AddConsumable(int itemID, int quantity = 1)
        {
            // 检查是否已存在相同类型的消耗品
            var existingItem = _items.FirstOrDefault(item => 
                item.ItemID == itemID && item.ItemType == ItemType.Consumable);
            
            if (existingItem != null)
            {
                // 堆叠现有物品
                existingItem.Quantity += quantity;
                Debug.Log($"PlayerInventory: Added {quantity} consumable {itemID} (stacked)");
            }
            else
            {
                // 检查背包空间
                if (IsFull)
                {
                    Debug.LogWarning("PlayerInventory: Cannot add consumable - inventory full");
                    return false;
                }

                // 添加新物品
                var newItem = new InventoryItem(itemID, ItemType.Consumable, quantity);
                _items.Add(newItem);
                Debug.Log($"PlayerInventory: Added new consumable {itemID} x{quantity}");
            }

            OnItemAdded?.Invoke(itemID, ItemType.Consumable);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// 移除消耗品
        /// </summary>
        public bool RemoveConsumable(int itemID, int quantity = 1)
        {
            var item = _items.FirstOrDefault(i => i.ItemID == itemID && i.ItemType == ItemType.Consumable);
            if (item == null)
            {
                Debug.LogWarning($"PlayerInventory: Cannot remove consumable {itemID} - not found");
                return false;
            }

            if (item.Quantity < quantity)
            {
                Debug.LogWarning($"PlayerInventory: Cannot remove {quantity} of consumable {itemID} - only have {item.Quantity}");
                return false;
            }

            item.Quantity -= quantity;
            if (item.Quantity <= 0)
            {
                _items.Remove(item);
                
                // 卸下如果已装备
                if (_equippedItemIDs.Contains(itemID))
                {
                    UnequipItem(itemID);
                }
            }

            OnItemRemoved?.Invoke(itemID, ItemType.Consumable);
            OnInventoryChanged?.Invoke();
            Debug.Log($"PlayerInventory: Removed {quantity} of consumable {itemID}");
            return true;
        }
        
        /// <summary>
        /// 检查是否有指定数量的消耗品
        /// </summary>
        public bool HasConsumable(int itemID, int quantity = 1)
        {
            var item = _items.FirstOrDefault(i => i.ItemID == itemID && i.ItemType == ItemType.Consumable);
            return item != null && item.Quantity >= quantity;
        }
        
        /// <summary>
        /// 获取消耗品数量
        /// </summary>
        public int GetConsumableQuantity(int itemID)
        {
            var item = _items.FirstOrDefault(i => i.ItemID == itemID && i.ItemType == ItemType.Consumable);
            return item?.Quantity ?? 0;
        }
        
        /// <summary>
        /// 获取所有消耗品
        /// </summary>
        public List<InventoryItem> GetAllConsumables()
        {
            return _items.Where(item => item.ItemType == ItemType.Consumable).ToList();
        }

        #endregion

        #region 弹药管理 - Ammo Management
        
        // 弹药库存 - 使用Dictionary存储弹药类型和数量
        private Dictionary<string, int> _ammoInventory = new Dictionary<string, int>();
        
        // 弹药事件
        public System.Action<string, int> OnAmmoAdded; // ammoType, amount added
        public System.Action<string, int, int> OnAmmoChanged; // ammoType, oldAmount, newAmount
        
        /// <summary>
        /// 添加弹药到库存
        /// </summary>
        /// <param name="ammoType">弹药类型</param>
        /// <param name="amount">数量</param>
        /// <returns>是否成功添加</returns>
        public bool AddAmmo(string ammoType, int amount)
        {
            if (string.IsNullOrEmpty(ammoType) || amount <= 0)
            {
                Debug.LogWarning($"PlayerInventory: Invalid ammo parameters - type: {ammoType}, amount: {amount}");
                return false;
            }
            
            int oldAmount = _ammoInventory.GetValueOrDefault(ammoType, 0);
            int newAmount = oldAmount + amount;
            _ammoInventory[ammoType] = newAmount;
            
            Debug.Log($"PlayerInventory: Added {amount} {ammoType} ammo. Total: {newAmount}");
            
            // 触发事件
            OnAmmoAdded?.Invoke(ammoType, amount);
            OnAmmoChanged?.Invoke(ammoType, oldAmount, newAmount);
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        
        /// <summary>
        /// 消耗弹药
        /// </summary>
        /// <param name="ammoType">弹药类型</param>
        /// <param name="amount">消耗数量</param>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeAmmo(string ammoType, int amount)
        {
            if (string.IsNullOrEmpty(ammoType) || amount <= 0)
                return false;
            
            int oldAmount = _ammoInventory.GetValueOrDefault(ammoType, 0);
            if (oldAmount < amount)
            {
                Debug.LogWarning($"PlayerInventory: Not enough {ammoType} ammo - need {amount}, have {oldAmount}");
                return false;
            }
                
            int newAmount = oldAmount - amount;
            _ammoInventory[ammoType] = newAmount;
            
            Debug.Log($"PlayerInventory: Consumed {amount} {ammoType} ammo. Remaining: {newAmount}");
            
            // 触发事件
            OnAmmoChanged?.Invoke(ammoType, oldAmount, newAmount);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// 检查是否有足够的弹药
        /// </summary>
        /// <param name="ammoType">弹药类型</param>
        /// <param name="amount">需要的数量</param>
        /// <returns>是否有足够弹药</returns>
        public bool HasAmmo(string ammoType, int amount = 1)
        {
            if (string.IsNullOrEmpty(ammoType) || amount <= 0)
                return false;
                
            return _ammoInventory.GetValueOrDefault(ammoType, 0) >= amount;
        }
        
        /// <summary>
        /// 获取指定类型弹药的数量
        /// </summary>
        /// <param name="ammoType">弹药类型</param>
        /// <returns>弹药数量</returns>
        public int GetAmmoCount(string ammoType)
        {
            if (string.IsNullOrEmpty(ammoType))
                return 0;
                
            return _ammoInventory.GetValueOrDefault(ammoType, 0);
        }
        
        /// <summary>
        /// 设置弹药数量（用于测试或特殊情况）
        /// </summary>
        /// <param name="ammoType">弹药类型</param>
        /// <param name="count">新的数量</param>
        public void SetAmmoCount(string ammoType, int count)
        {
            if (string.IsNullOrEmpty(ammoType))
                return;
            
            int oldAmount = _ammoInventory.GetValueOrDefault(ammoType, 0);
            int newAmount = Mathf.Max(0, count);
            _ammoInventory[ammoType] = newAmount;
            
            Debug.Log($"PlayerInventory: Set {ammoType} ammo to {newAmount}");
            
            // 触发事件（如果数量有变化）
            if (oldAmount != newAmount)
            {
                OnAmmoChanged?.Invoke(ammoType, oldAmount, newAmount);
                OnInventoryChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 获取所有弹药类型和数量
        /// </summary>
        /// <returns>弹药字典的副本</returns>
        public Dictionary<string, int> GetAllAmmo()
        {
            return new Dictionary<string, int>(_ammoInventory);
        }
        
        /// <summary>
        /// 获取所有有库存的弹药类型
        /// </summary>
        /// <returns>弹药类型列表</returns>
        public List<string> GetAvailableAmmoTypes()
        {
            var types = new List<string>();
            
            foreach (var kvp in _ammoInventory)
            {
                if (kvp.Value > 0)
                {
                    types.Add(kvp.Key);
                }
            }
            
            return types;
        }
        
        /// <summary>
        /// 检查是否有任何弹药
        /// </summary>
        /// <returns>是否有弹药</returns>
        public bool HasAnyAmmo()
        {
            foreach (var kvp in _ammoInventory)
            {
                if (kvp.Value > 0)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取总弹药数量
        /// </summary>
        /// <returns>所有类型弹药的总数</returns>
        public int GetTotalAmmoCount()
        {
            int total = 0;
            foreach (var kvp in _ammoInventory)
            {
                total += kvp.Value;
            }
            
            return total;
        }
        
        /// <summary>
        /// 清空所有弹药（用于测试或特殊事件）
        /// </summary>
        public void ClearAllAmmo()
        {
            var oldAmmo = new Dictionary<string, int>(_ammoInventory);
            _ammoInventory.Clear();
            
            // 触发每种弹药的变化事件
            foreach (var kvp in oldAmmo)
            {
                if (kvp.Value > 0)
                {
                    OnAmmoChanged?.Invoke(kvp.Key, kvp.Value, 0);
                }
            }
            
            OnInventoryChanged?.Invoke();
            Debug.Log("PlayerInventory: All ammo cleared");
        }

        #endregion

        #region Equipment System

        public bool EquipItem(int itemID)
        {
            // if (!HasItem(itemID))
            // {
            //     Debug.LogWarning($"PlayerInventory: Cannot equip item {itemID} - not in inventory");
            //     return false;
            // }

            if (_equippedItemIDs.Contains(itemID))
            {
                Debug.LogWarning($"PlayerInventory: Item {itemID} already equipped");
                return false;
            }

            _equippedItemIDs.Add(itemID);
            OnItemEquipped?.Invoke(itemID);
            Debug.Log($"PlayerInventory: Equipped item {itemID}");
            return true;
        }

        public bool UnequipItem(int itemID)
        {
            if (!_equippedItemIDs.Contains(itemID))
            {
                Debug.LogWarning($"PlayerInventory: Cannot unequip item {itemID} - not equipped");
                return false;
            }

            _equippedItemIDs.Remove(itemID);
            OnItemUnequipped?.Invoke(itemID);
            Debug.Log($"PlayerInventory: Unequipped item {itemID}");
            return true;
        }

        public bool IsItemEquipped(int itemID)
        {
            return _equippedItemIDs.Contains(itemID);
        }

        public List<int> GetEquippedItemIDs()
        {
            return new List<int>(_equippedItemIDs);
        }

        #endregion

        #region Save/Load System

        /// <summary>
        /// Get save data
        /// </summary>
        public InventorySaveData GetSaveData()
        {
            return new InventorySaveData
            {
                items = _items.Select(item => new ItemSaveData
                {
                    itemID = item.ItemID,
                    itemType = item.ItemType,
                    quantity = item.Quantity,
                    durability = item.Durability,
                    gridPosition = item.GridPosition,
                    currentAmmo = item.CurrentAmmo,
                    assetPath = item.AssetPath,
                    customData = item.CustomData
                }).ToList(),
                equippedItemIDs = new List<int>(_equippedItemIDs),
                equippedWeaponID = _equippedWeaponID,
                ammoInventory = new Dictionary<string, int>(_ammoInventory) // 保存弹药库存
            };
        }
        
        /// <summary>
        /// Load from save data
        /// </summary>
        public void LoadFromSaveData(InventorySaveData saveData)
        {
            _items.Clear();
            _equippedItemIDs.Clear();
            _ammoInventory.Clear();
            
            if (saveData?.items != null)
            {
                foreach (var itemData in saveData.items)
                {
                    var item = new InventoryItem(itemData.itemID, itemData.itemType, 
                        itemData.quantity, itemData.durability)
                    {
                        GridPosition = itemData.gridPosition,
                        CurrentAmmo = itemData.currentAmmo,
                        AssetPath = itemData.assetPath,
                        CustomData = itemData.customData ?? new Dictionary<string, object>()
                    };
                    
                    _items.Add(item);
                }
            }
            
            if (saveData?.equippedItemIDs != null)
            {
                _equippedItemIDs.AddRange(saveData.equippedItemIDs);
            }
            
            _equippedWeaponID = saveData?.equippedWeaponID ?? -1;
            
            // 加载弹药库存
            if (saveData?.ammoInventory != null)
            {
                foreach (var kvp in saveData.ammoInventory)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value >= 0)
                    {
                        _ammoInventory[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            OnInventoryChanged?.Invoke();
            Debug.Log($"PlayerInventory: Loaded {_items.Count} items and {_ammoInventory.Count} ammo types from save data");
        }


        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 获取ScriptableObject的资源路径
        /// </summary>
        private string GetAssetPath(ScriptableObject asset)
        {
            if (asset == null) return "";
            
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(asset);
            #else
            return $"Assets/Resources/Data/Items/{asset.name}Data.asset";
            #endif
        }
        
        /// <summary>
        /// 从路径加载ScriptableObject资源
        /// </summary>
        private T LoadAssetFromPath<T>(string path) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(path)) 
            {
                Debug.LogWarning($"PlayerInventory: LoadAssetFromPath: path is null or empty");
                return null;
            }
            
            Debug.Log($"PlayerInventory: LoadAssetFromPath: Loading from path: {path}");
            
            #if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Debug.Log($"PlayerInventory: LoadAssetFromPath: Editor result: {asset?.name ?? "NULL"}");
            return asset;
            #else
            // 运行时从Resources加载，需要去掉扩展名和Resources路径
            string resourcePath = path;
            if (path.StartsWith("Assets/Resources/"))
            {
                resourcePath = path.Substring("Assets/Resources/".Length);
            }
            if (resourcePath.EndsWith(".asset"))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - ".asset".Length);
            }
            
            Debug.Log($"PlayerInventory: LoadAssetFromPath: Resource path: {resourcePath}");
            var resource = Resources.Load<T>(resourcePath);
            Debug.Log($"PlayerInventory: LoadAssetFromPath: Resource result: {resource?.name ?? "NULL"}");
            return resource;
            #endif
        }
        
        #endregion
    }
}
