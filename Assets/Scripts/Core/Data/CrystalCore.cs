using UnityEngine;

namespace Resonance.Core.Data
{
    /// <summary>
    /// 晶核能量等级
    /// 基于能量/最大能量上限的百分比
    /// </summary>
    public enum CrystalEnergyTier
    {
        Abundant,   // > 80% - 充盈
        Normal,     // > 30%, ≤ 80% - 正常  
        Low         // > 0%, ≤ 30% - 低下
    }

    /// <summary>
    /// 晶核系统 - 玩家和敌人共用的晶核能量管理系统
    /// 管理晶核波纹、能量、容量和完整度
    /// </summary>
    [System.Serializable]
    public class CrystalCore
    {
        [Header("Crystal Core Pattern")]
        [SerializeField] private string _corePattern = "";
        
        [Header("Energy System")]
        [SerializeField] private float _currentEnergy = 0f;
        [SerializeField] private float _maxEnergyCapacity = 60f;
        [SerializeField] private float _currentEnergyCapacity = 60f;
        
        [Header("Energy Configuration")]
        [SerializeField] private float _energyPerSlot = 20f;
        
        // Runtime calculated values
        private CrystalEnergyTier _energyTier;
        private float _integrity;
        private int _maxSlots;
        private int _currentSlots;

        // Properties
        public string CorePattern => _corePattern;
        public float CurrentEnergy => _currentEnergy;
        public float MaxEnergyCapacity => _maxEnergyCapacity;
        public float CurrentEnergyCapacity => _currentEnergyCapacity;
        public float EnergyPerSlot => _energyPerSlot;
        public CrystalEnergyTier EnergyTier => _energyTier;
        public float Integrity => _integrity;
        public int MaxSlots => _maxSlots;
        public int CurrentSlots => _currentSlots;

        // Percentage properties
        public float EnergyPercentage => _maxEnergyCapacity > 0 ? _currentEnergy / _maxEnergyCapacity : 0f;
        public float CapacityPercentage => _maxEnergyCapacity > 0 ? _currentEnergyCapacity / _maxEnergyCapacity : 0f;

        // State properties
        public bool HasEnergy => _currentEnergy > 0f;
        public bool IsIntact => _integrity > 0f;
        public bool IsDestroyed => _integrity <= 0f;

        // Events
        public System.Action<float, float> OnEnergyChanged; // current, max
        public System.Action<float, float> OnCapacityChanged; // current, max
        public System.Action<CrystalEnergyTier> OnEnergyTierChanged;
        public System.Action<float> OnIntegrityChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">晶核配置</param>
        public CrystalCore(CrystalCoreConfig config)
        {
            if (config != null)
            {
                _corePattern = config.defaultPattern;
                _maxEnergyCapacity = config.maxEnergyCapacity;
                _energyPerSlot = config.energyPerSlot;
                _currentEnergyCapacity = _maxEnergyCapacity;
                
                // 玩家从0能量开始，敌人拥有无限能量
                _currentEnergy = config.startWithFullEnergy ? _maxEnergyCapacity : 0f;
            }
            else
            {
                // 默认配置
                _corePattern = "";
                _maxEnergyCapacity = 60f;
                _currentEnergyCapacity = 60f;
                _energyPerSlot = 20f;
                _currentEnergy = 0f;
            }

            UpdateCalculatedValues();
        }

        /// <summary>
        /// 更新计算值（槽位、等级、完整度等）
        /// </summary>
        public void UpdateCalculatedValues()
        {
            // 计算槽位数量
            _maxSlots = Mathf.FloorToInt(_maxEnergyCapacity / _energyPerSlot);
            _currentSlots = Mathf.FloorToInt(_currentEnergyCapacity / _energyPerSlot);
            
            // 计算完整度
            _integrity = _maxEnergyCapacity > 0 ? _currentEnergyCapacity / _maxEnergyCapacity : 0f;
            
            // 计算能量等级
            var previousTier = _energyTier;
            float energyPercent = EnergyPercentage;
            
            if (energyPercent > 0.8f)
                _energyTier = CrystalEnergyTier.Abundant;
            else if (energyPercent > 0.3f)
                _energyTier = CrystalEnergyTier.Normal;
            else
                _energyTier = CrystalEnergyTier.Low;

            // 触发事件
            if (previousTier != _energyTier)
            {
                OnEnergyTierChanged?.Invoke(_energyTier);
            }
        }

        /// <summary>
        /// 设置晶核波纹
        /// </summary>
        /// <param name="pattern">波纹名称</param>
        public void SetCorePattern(string pattern)
        {
            _corePattern = pattern ?? "";
            Debug.Log($"CrystalCore: Pattern set to {_corePattern}");
        }

        /// <summary>
        /// 增加能量
        /// </summary>
        /// <param name="amount">增加的能量值</param>
        /// <returns>实际增加的能量值</returns>
        public float AddEnergy(float amount)
        {
            if (amount <= 0f) return 0f;

            float previousEnergy = _currentEnergy;
            _currentEnergy = Mathf.Min(_currentEnergy + amount, _currentEnergyCapacity);
            float actualAdded = _currentEnergy - previousEnergy;

            if (actualAdded > 0f)
            {
                UpdateCalculatedValues();
                OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergyCapacity);
                Debug.Log($"CrystalCore: Added {actualAdded} energy. Current: {_currentEnergy}/{_currentEnergyCapacity}");
            }

            return actualAdded;
        }

        /// <summary>
        /// 消耗能量
        /// </summary>
        /// <param name="amount">消耗的能量值</param>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeEnergy(float amount)
        {
            if (amount <= 0f || _currentEnergy < amount) return false;

            _currentEnergy = Mathf.Max(0f, _currentEnergy - amount);
            UpdateCalculatedValues();
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergyCapacity);
            
            Debug.Log($"CrystalCore: Consumed {amount} energy. Current: {_currentEnergy}/{_currentEnergyCapacity}");
            return true;
        }

        /// <summary>
        /// 消耗一个能量槽
        /// </summary>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeEnergySlot()
        {
            return ConsumeEnergy(_energyPerSlot);
        }

        /// <summary>
        /// 检查是否可以消耗指定数量的能量
        /// </summary>
        /// <param name="amount">需要的能量值</param>
        /// <returns>是否有足够能量</returns>
        public bool HasEnoughEnergy(float amount)
        {
            return _currentEnergy >= amount && amount > 0f;
        }

        /// <summary>
        /// 检查是否可以消耗一个能量槽
        /// </summary>
        /// <returns>是否有足够能量</returns>
        public bool CanConsumeSlot()
        {
            return HasEnoughEnergy(_energyPerSlot);
        }

        /// <summary>
        /// 获取当前能量的槽位数
        /// </summary>
        /// <returns>槽位数（可以是小数）</returns>
        public float GetEnergyInSlots()
        {
            return _energyPerSlot > 0 ? _currentEnergy / _energyPerSlot : 0f;
        }

        /// <summary>
        /// 损坏晶核容量（被攻击时调用）
        /// </summary>
        /// <param name="capacityDamage">容量损失值</param>
        /// <returns>实际损失的容量</returns>
        public float DamageCapacity(float capacityDamage)
        {
            if (capacityDamage <= 0f) return 0f;

            float previousCapacity = _currentEnergyCapacity;
            _currentEnergyCapacity = Mathf.Max(0f, _currentEnergyCapacity - capacityDamage);
            float actualDamage = previousCapacity - _currentEnergyCapacity;

            // 如果当前能量超过了新的容量上限，调整当前能量
            if (_currentEnergy > _currentEnergyCapacity)
            {
                _currentEnergy = _currentEnergyCapacity;
                OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergyCapacity);
            }

            if (actualDamage > 0f)
            {
                UpdateCalculatedValues();
                OnCapacityChanged?.Invoke(_currentEnergyCapacity, _maxEnergyCapacity);
                OnIntegrityChanged?.Invoke(_integrity);
                
                Debug.Log($"CrystalCore: Capacity damaged by {actualDamage}. Current capacity: {_currentEnergyCapacity}/{_maxEnergyCapacity} (Integrity: {_integrity:P1})");
            }

            return actualDamage;
        }

        /// <summary>
        /// 修复晶核容量（在医疗舱等地方调用）
        /// </summary>
        /// <param name="repairAmount">修复的容量值</param>
        /// <returns>实际修复的容量</returns>
        public float RepairCapacity(float repairAmount)
        {
            if (repairAmount <= 0f) return 0f;

            float previousCapacity = _currentEnergyCapacity;
            _currentEnergyCapacity = Mathf.Min(_currentEnergyCapacity + repairAmount, _maxEnergyCapacity);
            float actualRepair = _currentEnergyCapacity - previousCapacity;

            if (actualRepair > 0f)
            {
                UpdateCalculatedValues();
                OnCapacityChanged?.Invoke(_currentEnergyCapacity, _maxEnergyCapacity);
                OnIntegrityChanged?.Invoke(_integrity);
                
                Debug.Log($"CrystalCore: Capacity repaired by {actualRepair}. Current capacity: {_currentEnergyCapacity}/{_maxEnergyCapacity} (Integrity: {_integrity:P1})");
            }

            return actualRepair;
        }

        /// <summary>
        /// 完全修复晶核容量
        /// </summary>
        public void FullRepair()
        {
            RepairCapacity(_maxEnergyCapacity - _currentEnergyCapacity);
        }

        /// <summary>
        /// 设置为无限能量模式（敌人使用）
        /// </summary>
        public void SetInfiniteEnergy()
        {
            _currentEnergy = _maxEnergyCapacity;
            UpdateCalculatedValues();
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergyCapacity);
            Debug.Log("CrystalCore: Set to infinite energy mode");
        }

        /// <summary>
        /// 获取保存数据
        /// </summary>
        /// <returns>晶核保存数据</returns>
        public CrystalCoreSaveData GetSaveData()
        {
            return new CrystalCoreSaveData
            {
                corePattern = _corePattern,
                currentEnergy = _currentEnergy,
                maxEnergyCapacity = _maxEnergyCapacity,
                currentEnergyCapacity = _currentEnergyCapacity,
                energyPerSlot = _energyPerSlot
            };
        }

        /// <summary>
        /// 从保存数据加载
        /// </summary>
        /// <param name="saveData">保存数据</param>
        public void LoadFromSaveData(CrystalCoreSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("CrystalCore: Cannot load from null save data");
                return;
            }

            _corePattern = saveData.corePattern ?? "";
            _currentEnergy = Mathf.Max(0f, saveData.currentEnergy);
            _maxEnergyCapacity = Mathf.Max(1f, saveData.maxEnergyCapacity);
            _currentEnergyCapacity = Mathf.Clamp(saveData.currentEnergyCapacity, 0f, _maxEnergyCapacity);
            _energyPerSlot = Mathf.Max(1f, saveData.energyPerSlot);

            // 确保当前能量不超过当前容量
            _currentEnergy = Mathf.Min(_currentEnergy, _currentEnergyCapacity);

            UpdateCalculatedValues();
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergyCapacity);
            OnCapacityChanged?.Invoke(_currentEnergyCapacity, _maxEnergyCapacity);
            OnIntegrityChanged?.Invoke(_integrity);

            Debug.Log($"CrystalCore: Loaded from save data. Energy: {_currentEnergy}/{_currentEnergyCapacity}, Pattern: {_corePattern}");
        }

        /// <summary>
        /// 清理事件订阅
        /// </summary>
        public void Cleanup()
        {
            OnEnergyChanged = null;
            OnCapacityChanged = null;
            OnEnergyTierChanged = null;
            OnIntegrityChanged = null;
        }
    }

    /// <summary>
    /// 晶核保存数据结构
    /// </summary>
    [System.Serializable]
    public class CrystalCoreSaveData
    {
        public string corePattern;
        public float currentEnergy;
        public float maxEnergyCapacity;
        public float currentEnergyCapacity;
        public float energyPerSlot;

        public CrystalCoreSaveData()
        {
            corePattern = "";
            currentEnergy = 0f;
            maxEnergyCapacity = 60f;
            currentEnergyCapacity = 60f;
            energyPerSlot = 20f;
        }
    }
}
