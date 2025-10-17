using UnityEngine;
using Resonance.Utilities;
using Resonance.Utilities.Waves;

namespace Resonance.Utilities.CrystalCore
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
    /// 晶核系统 - 玩家和敌人共用的晶核管理系统
    /// 包含三个部分：生命、能量、波纹
    /// </summary>
    [System.Serializable]
    public class CrystalCore
    {
        #region Serialized Fields
        
        [Header("Core Health")]
        [SerializeField] private float _currentCoreHealth;
        [SerializeField] private float _maxCoreHealth;
        
        [Header("Core Energy")]
        [SerializeField] private float _energyPerSlot = 30f;
        [SerializeField] private float _currentEnergy;
        [SerializeField] private float _maxEnergy;
        
        [Header("Core Wave")]
        [SerializeField] private Wave _wave;
        
        #endregion
        
        #region Runtime Calculated Values
        
        private CrystalEnergyTier _energyTier;
        private int _maxSlots;
        private int _currentSlots;
        
        #endregion
        
        #region Properties - Core Health
        
        public float CurrentCoreHealth => _currentCoreHealth;
        public float MaxCoreHealth => _maxCoreHealth;
        public CoreHealthState CoreHealthState => _currentCoreHealth > 0 ? CoreHealthState.Intact : CoreHealthState.Destroyed;
        public float CoreHealthPercentage => _maxCoreHealth > 0 ? _currentCoreHealth / _maxCoreHealth : 0f;
        
        #endregion
        
        #region Properties - Core Energy
        
        public float CurrentEnergy => _currentEnergy;
        public float MaxEnergy => _maxEnergy;
        public float EnergyPerSlot => _energyPerSlot;
        public CrystalEnergyTier EnergyTier => _energyTier;
        public int MaxSlots => _maxSlots;
        public int CurrentSlots => _currentSlots;
        public float EnergyPercentage => _maxEnergy > 0 ? _currentEnergy / _maxEnergy : 0f;
        public bool HasEnergy => _currentEnergy > 0f;
        
        #endregion
        
        #region Properties - Core Wave
        
        public Wave Wave => _wave;
        public float CurrentChaos => _wave?.CurrentChaos ?? 0f;
        public float MaxChaos => _wave?.MaxChaos ?? 0f;
        public float ChaosThreshold => _wave?.ChaosThreshold ?? 0f;
        public WaveChaosState ChaosState => _wave?.ChaosState ?? WaveChaosState.Order;
        public float ChaosPercentage => _wave?.ChaosPercentage ?? 0f;
        
        #endregion
        
        #region Events
        
        public System.Action<float, float> OnCoreHealthChanged; // current, max
        public System.Action<float, float> OnEnergyChanged; // current, max
        public System.Action<CrystalEnergyTier> OnEnergyTierChanged;
        public System.Action OnCoreDestroyed;
        
        // Wave events are delegated to Wave object
        public System.Action<float, float> OnChaosChanged
        {
            get => _wave?.OnChaosChanged;
            set { if (_wave != null) _wave.OnChaosChanged = value; }
        }
        
        public System.Action<WaveChaosState> OnChaosStateChanged
        {
            get => _wave?.OnChaosStateChanged;
            set { if (_wave != null) _wave.OnChaosStateChanged = value; }
        }
        
        #endregion
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public CrystalCore(CrystalCoreConfig config, QTEConfig qteConfig = null)
        {
            if (config != null)
            {
                _energyPerSlot = config.energyPerSlot;
                _maxCoreHealth = config.initialMaxCoreHealth;
                _currentCoreHealth = _maxCoreHealth;
                
                // 最大能量值 = 当前晶核生命值
                _maxEnergy = _currentCoreHealth;
                
                // 玩家从0能量开始, 敌人拥有满能量
                _currentEnergy = config.startWithFullEnergy ? _maxEnergy : 0f;
                
                // 波纹系统初始化 - 使用WaveConfig或legacy参数
                if (config.waveConfig != null)
                {
                    _wave = new Wave(config.waveConfig);
                }
                else
                {
                    // Legacy fallback - 使用默认值
                    _wave = new Wave(100f, 18f, qteConfig);
                }
            }
            else
            {
                // 默认配置：3格生命值 = 90点
                _energyPerSlot = 30f;
                _maxCoreHealth = 90f; // 3 slots * 30 per slot
                _currentCoreHealth = _maxCoreHealth;
                _maxEnergy = _maxCoreHealth;
                _currentEnergy = 0f;
                
                // 默认波纹配置
                _wave = new Wave(100f, 18f, qteConfig);
            }

            UpdateCalculatedValues();
        }
        
        /// <summary>
        /// 更新计算值(槽位、等级等)
        /// </summary>
        public void UpdateCalculatedValues()
        {
            // max slots is the max core health (ideal max energy) / energy per slot
            _maxSlots = Mathf.FloorToInt(_maxCoreHealth / _energyPerSlot);
            // current slots is the current energy / energy per slot
            _currentSlots = Mathf.FloorToInt(_currentEnergy / _energyPerSlot);
            
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
        
        #region Core Health Methods
        
        /// <summary>
        /// 晶核受到生命伤害
        /// </summary>
        public float TakeCoreHealthDamage(float damage)
        {
            if (damage <= 0f || _currentCoreHealth <= 0f) return 0f;

            float previousHealth = _currentCoreHealth;
            _currentCoreHealth = Mathf.Max(0f, _currentCoreHealth - damage);
            float actualDamage = previousHealth - _currentCoreHealth;

            if (actualDamage > 0f)
            {
                // 最大能量值同步到当前晶核生命值
                _maxEnergy = _currentCoreHealth;
                
                // 如果当前能量超过新的最大能量, 调整当前能量
                if (_currentEnergy > _maxEnergy)
                {
                    _currentEnergy = _maxEnergy;
                    OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
                }
                
                UpdateCalculatedValues();
                OnCoreHealthChanged?.Invoke(_currentCoreHealth, _maxCoreHealth);
                
                // 晶核死亡
                if (_currentCoreHealth <= 0f)
                {
                    OnCoreDestroyed?.Invoke();
                }
                
                Debug.Log($"CrystalCore: Took {actualDamage} core health damage. Current: {_currentCoreHealth}/{_maxCoreHealth}");
            }

            return actualDamage;
        }
        
        /// <summary>
        /// 修复晶核生命(在医疗舱调用)
        /// </summary>
        public float RepairCoreHealth(float repairAmount)
        {
            if (repairAmount <= 0f) return 0f;

            float previousHealth = _currentCoreHealth;
            _currentCoreHealth = Mathf.Min(_currentCoreHealth + repairAmount, _maxCoreHealth);
            float actualRepair = _currentCoreHealth - previousHealth;

            if (actualRepair > 0f)
            {
                // 最大能量值同步到当前晶核生命值
                _maxEnergy = _currentCoreHealth;
                
                UpdateCalculatedValues();
                OnCoreHealthChanged?.Invoke(_currentCoreHealth, _maxCoreHealth);
                
                Debug.Log($"CrystalCore: Repaired {actualRepair} core health. Current: {_currentCoreHealth}/{_maxCoreHealth}");
            }

            return actualRepair;
        }
        
        /// <summary>
        /// 完全修复晶核生命
        /// </summary>
        public void FullRepairCoreHealth()
        {
            RepairCoreHealth(_maxCoreHealth - _currentCoreHealth);
        }
        
        /// <summary>
        /// 提升最大晶核生命值(成长系统)
        /// </summary>
        public void UpgradeMaxCoreHealth(float amount)
        {
            if (amount <= 0f) return;
            
            _maxCoreHealth += amount;
            
            // 如果当前生命值等于之前的最大值, 也提升当前生命值
            if (_currentCoreHealth >= _maxCoreHealth - amount)
            {
                _currentCoreHealth = _maxCoreHealth;
                _maxEnergy = _currentCoreHealth;
                OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
            }
            
            UpdateCalculatedValues();
            OnCoreHealthChanged?.Invoke(_currentCoreHealth, _maxCoreHealth);
            
            Debug.Log($"CrystalCore: Upgraded max core health by {amount}. New max: {_maxCoreHealth}");
        }
        
        #endregion
        
        #region Core Energy Methods
        
        /// <summary>
        /// 增加能量
        /// </summary>
        public float AddEnergy(float amount)
        {
            if (amount <= 0f) return 0f;

            float previousEnergy = _currentEnergy;
            _currentEnergy = Mathf.Min(_currentEnergy + amount, _maxEnergy);
            float actualAdded = _currentEnergy - previousEnergy;

            if (actualAdded > 0f)
            {
                UpdateCalculatedValues();
                OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
                Debug.Log($"CrystalCore: Added {actualAdded} energy. Current: {_currentEnergy}/{_maxEnergy}");
            }

            return actualAdded;
        }
        
        /// <summary>
        /// 消耗能量
        /// </summary>
        public bool ConsumeEnergy(float amount)
        {
            if (amount <= 0f || _currentEnergy < amount) return false;

            _currentEnergy = Mathf.Max(0f, _currentEnergy - amount);
            UpdateCalculatedValues();
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
            
            Debug.Log($"CrystalCore: Consumed {amount} energy. Current: {_currentEnergy}/{_maxEnergy}");
            return true;
        }
        
        /// <summary>
        /// 消耗一个能量槽
        /// </summary>
        public bool ConsumeEnergySlot()
        {
            return ConsumeEnergy(_energyPerSlot);
        }
        
        /// <summary>
        /// 检查是否有足够能量
        /// </summary>
        public bool HasEnoughEnergy(float amount)
        {
            return _currentEnergy >= amount && amount > 0f;
        }
        
        /// <summary>
        /// 检查是否可以消耗一个能量槽
        /// </summary>
        public bool CanConsumeSlot()
        {
            return HasEnoughEnergy(_energyPerSlot);
        }
        
        /// <summary>
        /// 获取当前能量的槽位数
        /// </summary>
        public float GetEnergyInSlots()
        {
            return _energyPerSlot > 0 ? _currentEnergy / _energyPerSlot : 0f;
        }
        
        /// <summary>
        /// 设置为满能量模式(敌人使用)
        /// </summary>
        public void SetFullEnergy()
        {
            _currentEnergy = _maxEnergy;
            UpdateCalculatedValues();
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
            Debug.Log("CrystalCore: Set to full energy");
        }
        
        #endregion
        
        #region Core Wave Methods
        
        /// <summary>
        /// 增加紊乱值 (委托给 Wave)
        /// </summary>
        public float AddChaos(float amount)
        {
            return _wave?.AddChaos(amount) ?? 0f;
        }
        
        /// <summary>
        /// 更新紊乱值 (自然恢复, 每帧调用, 委托给 Wave)
        /// </summary>
        public void UpdateChaos(float chaosRecoveryRate, float deltaTime)
        {
            _wave?.UpdateChaos(chaosRecoveryRate, deltaTime);
        }
        
        /// <summary>
        /// 重置紊乱值 (委托给 Wave)
        /// </summary>
        public void ResetChaos()
        {
            _wave?.ResetChaos();
        }
        
        #endregion
        
        #region Save/Load
        
        /// <summary>
        /// 获取保存数据
        /// </summary>
        public CrystalCoreSaveData GetSaveData()
        {
            return new CrystalCoreSaveData
            {
                currentCoreHealth = _currentCoreHealth,
                maxCoreHealth = _maxCoreHealth,
                currentEnergy = _currentEnergy,
                energyPerSlot = _energyPerSlot,
                waveSaveData = _wave?.GetSaveData()
            };
        }
        
        /// <summary>
        /// 从保存数据加载
        /// </summary>
        public void LoadFromSaveData(CrystalCoreSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("CrystalCore: Cannot load from null save data");
                return;
            }

            _maxCoreHealth = Mathf.Max(1f, saveData.maxCoreHealth);
            _currentCoreHealth = Mathf.Clamp(saveData.currentCoreHealth, 0f, _maxCoreHealth);
            _maxEnergy = _currentCoreHealth;
            _energyPerSlot = Mathf.Max(1f, saveData.energyPerSlot);
            _currentEnergy = Mathf.Clamp(saveData.currentEnergy, 0f, _maxEnergy);

            // Load wave data
            if (saveData.waveSaveData != null)
            {
                _wave?.LoadFromSaveData(saveData.waveSaveData);
            }

            UpdateCalculatedValues();
            
            OnCoreHealthChanged?.Invoke(_currentCoreHealth, _maxCoreHealth);
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);

            Debug.Log($"CrystalCore: Loaded from save data. Health: {_currentCoreHealth}/{_maxCoreHealth}, Energy: {_currentEnergy}/{_maxEnergy}, Chaos: {CurrentChaos}/{MaxChaos}");
        }
        
        #endregion
        
        /// <summary>
        /// 清理事件订阅
        /// </summary>
        public void Cleanup()
        {
            OnCoreHealthChanged = null;
            OnEnergyChanged = null;
            OnEnergyTierChanged = null;
            OnCoreDestroyed = null;
            _wave?.Cleanup();
        }
    }

    /// <summary>
    /// 晶核保存数据结构
    /// </summary>
    [System.Serializable]
    public class CrystalCoreSaveData
    {
        public float currentCoreHealth;
        public float maxCoreHealth;
        public float currentEnergy;
        public float energyPerSlot;
        public WaveSaveData waveSaveData;

        public CrystalCoreSaveData()
        {
            currentCoreHealth = 90f;
            maxCoreHealth = 90f;
            currentEnergy = 0f;
            energyPerSlot = 30f;
            waveSaveData = new WaveSaveData();
        }
    }
}
