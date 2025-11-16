using UnityEngine;
using Resonance.Utilities.Types;
using Resonance.Utilities.Waves;

namespace Resonance.Utilities.CrystalCore
{
    /// <summary>
    /// Crystal core system - shared crystal core management system for player and enemy
    /// Contains three parts: Health, Energy, Wave
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
        public WaveformType WaveformType => _wave?.WaveformType ?? WaveformType.Sine;
        public float Frequency => _wave?.Frequency ?? 1.0f;
        public float Amplitude => _wave?.Amplitude ?? 1.0f;
        public float Unit => _wave?.Unit ?? 1.0f;
        public float[] WaveformTable => _wave?.WaveformTable ?? new float[0];
        public static int WaveformResolution => Wave.WaveformResolution;
        
        #endregion
        
        #region Events
        
        public System.Action<float, float> OnCoreHealthChanged; // current, max
        public System.Action<float, float> OnEnergyChanged; // current, max
        public System.Action<CrystalEnergyTier> OnEnergyTierChanged;
        public System.Action OnCoreDestroyed;
        
        // Wave events are delegated to Wave object
        public System.Action OnWavePropertiesChanged => _wave?.OnWavePropertiesChanged;
        
        #endregion
        
        /// <summary>
        /// Constructor
        /// </summary>
        public CrystalCore(CrystalCoreConfig config)
        {
            if (config != null)
            {
                _energyPerSlot = Mathf.Round(config.energyPerSlot);
                _maxCoreHealth = Mathf.Round(config.initialMaxCoreHealth);
                _currentCoreHealth = _maxCoreHealth;
                
                // Maximum energy = current core health
                _maxEnergy = _currentCoreHealth;
                
                // Player starts with 0 energy, enemy starts with full energy
                _currentEnergy = config.startWithFullEnergy ? _maxEnergy : 0f;
                
                // Wave system initialization - using WaveConfig
                if (config.waveConfig != null)
                {
                    _wave = new Wave(config.waveConfig);
                }
            }
            else
            {
                // Default configuration: 3 slots * 30 energy per slot = 90 health
                _energyPerSlot = Mathf.Round(30f);
                _maxCoreHealth = Mathf.Round(90f);
                _currentCoreHealth = _maxCoreHealth;
                _maxEnergy = _maxCoreHealth;
                _currentEnergy = 0f;
                
                // Default wave configuration
                _wave = new Wave(new WaveConfig());
            }

            UpdateCalculatedValues();
        }
        
        /// <summary>
        /// Update calculated values (slots, tier, etc.)
        /// </summary>
        public void UpdateCalculatedValues()
        {
            // max slots is the max core health (ideal max energy) / energy per slot
            _maxSlots = Mathf.FloorToInt(_maxCoreHealth / _energyPerSlot);
            // current slots is the current energy / energy per slot
            _currentSlots = Mathf.FloorToInt(_currentEnergy / _energyPerSlot);
            
            // Calculate energy tier
            var previousTier = _energyTier;
            float energyPercent = EnergyPercentage;
            
            if (energyPercent > 0.8f)
                _energyTier = CrystalEnergyTier.Abundant;
            else if (energyPercent > 0.3f)
                _energyTier = CrystalEnergyTier.Normal;
            else
                _energyTier = CrystalEnergyTier.Low;

            // Trigger events
            if (previousTier != _energyTier)
            {
                OnEnergyTierChanged?.Invoke(_energyTier);
            }
        }
        
        #region Core Health Methods
        
        /// <summary>
        /// Take core health damage
        /// </summary>
        public float TakeCoreHealthDamage(float damage)
        {
            if (damage <= 0f || _currentCoreHealth <= 0f) return 0f;

            float previousHealth = _currentCoreHealth;
            _currentCoreHealth = Mathf.Round(Mathf.Max(0f, _currentCoreHealth - damage));
            float actualDamage = previousHealth - _currentCoreHealth;

            if (actualDamage > 0f)
            {
                // Maximum energy value synchronized to current core health
                _maxEnergy = _currentCoreHealth;
                
                // If current energy exceeds new maximum energy, adjust current energy
                if (_currentEnergy > _maxEnergy)
                {
                    _currentEnergy = _maxEnergy;
                    OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
                }
                
                UpdateCalculatedValues();
                OnCoreHealthChanged?.Invoke(_currentCoreHealth, _maxCoreHealth);
                
                // Core death
                if (_currentCoreHealth <= 0f)
                {
                    OnCoreDestroyed?.Invoke();
                }
                
                Debug.Log($"CrystalCore: Took {actualDamage} core health damage. Current: {_currentCoreHealth}/{_maxCoreHealth}");
            }

            return actualDamage;
        }
        
        /// <summary>
        /// Repair core health
        /// </summary>
        public float RestoreCoreHealth(float repairAmount)
        {
            if (repairAmount <= 0f) return 0f;

            float previousHealth = _currentCoreHealth;
            _currentCoreHealth = Mathf.Round(Mathf.Min(_currentCoreHealth + repairAmount, _maxCoreHealth));
            float actualRepair = _currentCoreHealth - previousHealth;

            if (actualRepair > 0f)
            {
                // Maximum energy value synchronized to current core health
                _maxEnergy = _currentCoreHealth;
                
                UpdateCalculatedValues();
                OnCoreHealthChanged?.Invoke(_currentCoreHealth, _maxCoreHealth);
                
                Debug.Log($"CrystalCore: Repaired {actualRepair} core health. Current: {_currentCoreHealth}/{_maxCoreHealth}");
            }

            return actualRepair;
        }
        
        /// <summary>
        /// Fully repair core health
        /// </summary>
        public void FullRestoreCoreHealth()
        {
            RestoreCoreHealth(_maxCoreHealth - _currentCoreHealth);
        }
        
        /// <summary>
        /// Upgrade maximum core health (growth system)
        /// </summary>
        public void UpgradeMaxCoreHealth(float amount)
        {
            if (amount <= 0f) return;
            
            _maxCoreHealth = Mathf.Round(_maxCoreHealth + amount);
            
            // If current health equals previous maximum, also upgrade current health
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
        /// Add energy
        /// </summary>
        public float AddEnergy(float amount)
        {
            if (amount <= 0f) return 0f;

            float previousEnergy = _currentEnergy;
            _currentEnergy = Mathf.Round(Mathf.Min(_currentEnergy + amount, _maxEnergy));
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
        /// Consume energy
        /// </summary>
        public bool ConsumeEnergy(float amount)
        {
            if (amount <= 0f || _currentEnergy < amount) return false;

            _currentEnergy = Mathf.Round(Mathf.Max(0f, _currentEnergy - amount));
            UpdateCalculatedValues();
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);
            
            Debug.Log($"CrystalCore: Consumed {amount} energy. Current: {_currentEnergy}/{_maxEnergy}");
            return true;
        }
        
        /// <summary>
        /// Consume one energy slot
        /// </summary>
        public bool ConsumeEnergySlot()
        {
            return ConsumeEnergy(_energyPerSlot);
        }
        
        /// <summary>
        /// Check if has enough energy
        /// </summary>
        public bool HasEnoughEnergy(float amount)
        {
            return _currentEnergy >= amount && amount > 0f;
        }
        
        /// <summary>
        /// Check if can consume one energy slot
        /// </summary>
        public bool CanConsumeSlot()
        {
            return HasEnoughEnergy(_energyPerSlot);
        }
        
        /// <summary>
        /// Get current energy slots
        /// </summary>
        public float GetEnergyInSlots()
        {
            return _energyPerSlot > 0 ? _currentEnergy / _energyPerSlot : 0f;
        }
        
        /// <summary>
        /// Set to full energy mode (enemy uses)
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
        /// Set wave
        /// </summary>
        public void SetWave(Wave wave)
        {
            if (wave == null) return;
            _wave = wave;
            UpdateCalculatedValues();
            Debug.Log($"CrystalCore: Set wave. Waveform type: {_wave?.WaveformType ?? WaveformType.Sine}, Frequency: {_wave?.Frequency ?? 1.0f},"+
                      $" Amplitude: {_wave?.Amplitude ?? 1.0f}, Unit: {_wave?.Unit ?? 1.0f}");
        }
        
        #endregion
        
        #region Save/Load
        
        /// <summary>
        /// Get save data
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
        /// Load from save data
        /// </summary>
        public void LoadFromSaveData(CrystalCoreSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("CrystalCore: Cannot load from null save data");
                return;
            }

            _maxCoreHealth = Mathf.Round(Mathf.Max(1f, saveData.maxCoreHealth));
            _currentCoreHealth = Mathf.Round(Mathf.Clamp(saveData.currentCoreHealth, 0f, _maxCoreHealth));
            _maxEnergy = _currentCoreHealth;
            _energyPerSlot = Mathf.Round(Mathf.Max(1f, saveData.energyPerSlot));
            _currentEnergy = Mathf.Round(Mathf.Clamp(saveData.currentEnergy, 0f, _maxEnergy));

            // Load wave data
            if (saveData.waveSaveData != null)
            {
                _wave?.LoadFromSaveData(saveData.waveSaveData);
            }

            UpdateCalculatedValues();
            
            OnCoreHealthChanged?.Invoke(_currentCoreHealth, _maxCoreHealth);
            OnEnergyChanged?.Invoke(_currentEnergy, _maxEnergy);

            Debug.Log($"CrystalCore: Loaded from save data. Health: {_currentCoreHealth}/{_maxCoreHealth}, Energy: {_currentEnergy}/{_maxEnergy}");
        }
        
        #endregion
        
        /// <summary>
        /// Cleanup event subscriptions
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
    /// Crystal core save data structure
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
