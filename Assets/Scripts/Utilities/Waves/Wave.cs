using UnityEngine;
using Resonance.Utilities.Types;

namespace Resonance.Utilities.Waves
{
    /// <summary>
    /// Wave System - manages chaos and QTE configuration
    /// Part of CrystalCore System
    /// </summary>
    [System.Serializable]
    public class Wave
    {
        #region Wave Properties

        [Header("Shape & Dimensions")]
        [SerializeField] private WaveformType _waveformType = WaveformType.Sine; // The base shape generated
        [SerializeField] private float _frequency = 1.0f;     // Cycles per unit length
        [SerializeField] private float _amplitude = 1.0f;     // Peak amplitude (max deviation from 0)
        [SerializeField] private float _length = 10.0f;     // The spatial length of one full pattern repetition before cycling

        [Header("Sampled Representation")]
        [SerializeField] private float[] _waveformTable; // Stores one full cycle of the wave sampled at discrete points, normalized between -1 and 1.
        [SerializeField] private static int WAVEFORM_RESOLUTION = 1024; // Standard resolution for the LUT
        
        #endregion
        
        #region Chaos Fields
        
        [Header("Wave Chaos")]
        [SerializeField] private float _currentChaos;
        [SerializeField] private float _maxChaos;
        [SerializeField] private float _chaosThreshold = 18f;
        [SerializeField] private WaveChaosState _chaosState;
        
        #endregion
        
        #region Properties

        public WaveformType WaveformType => _waveformType;
        public float Frequency => _frequency;
        public float Amplitude => _amplitude;
        public float Length => _length;
        public float[] WaveformTable => _waveformTable;
        public static int WaveformResolution => WAVEFORM_RESOLUTION;
        
        public float CurrentChaos => _currentChaos;
        public float MaxChaos => _maxChaos;
        public float ChaosThreshold => _chaosThreshold;
        public WaveChaosState ChaosState => _chaosState;
        public float ChaosPercentage => _maxChaos > 0 ? _currentChaos / _maxChaos : 0f;
        
        /// <summary>
        /// Get the chaos intensity as a value between 0 and 1
        /// Formula: (currentChaos - chaosThreshold) / (maxChaos - chaosThreshold)
        /// 0 = no chaos effect, 1 = full chaos effect
        /// </summary>
        public float ChaosIntensity
        {
            get
            {
                if (_currentChaos <= _chaosThreshold)
                    return 0f;
                
                float intensity = (_currentChaos - _chaosThreshold) / (_maxChaos - _chaosThreshold);
                return Mathf.Clamp01(intensity);
            }
        }
        
        #endregion
        
        #region Events
        
        public System.Action<float, float> OnChaosChanged; // current, max
        public System.Action<WaveChaosState> OnChaosStateChanged;
        public System.Action OnWavePropertiesChanged;
        
        #endregion
        
        /// <summary>
        /// Constructor with WaveConfig
        /// </summary>
        public Wave(WaveConfig config)
        {
            _waveformTable = new float[WAVEFORM_RESOLUTION];
            
            if (config != null)
            {
                _maxChaos = config.maxChaos;
                _chaosThreshold = config.chaosThreshold;
                _waveformType = config.waveformType;
                _frequency = config.frequency;
                _amplitude = config.amplitude;
                _length = config.length;
            }
            else
            {
                _maxChaos = 100f;
                _chaosThreshold = 18f;
                _waveformType = WaveformType.Sine;
                _frequency = 1.0f;
                _amplitude = 1.0f;
                _length = 10.0f;
            }

            GenerateBaseWaveformTable(_waveformType);
            _currentChaos = 0f;
            _chaosState = WaveChaosState.Order;
        }

        #region Wave Methods

        public float GetWaveValue(float normalizedPosition)
        {
            if (_waveformTable == null || _waveformTable.Length == 0) return 0f;

            // 1. Calculate the phase within the sampled table
            float phase = (normalizedPosition * _frequency) % 1.0f;
            phase = (phase < 0.0f) ? phase + 1.0f : phase;

            // 2. Map phase to table index with linear interpolation
            float index = phase * (_waveformTable.Length - 1);
            int index1 = Mathf.FloorToInt(index);
            int index2 = (index1 + 1 < _waveformTable.Length) ? index1 + 1 : 0;
            float fraction = index - index1;

            // 3. Get the values at the indices
            float value1 = _waveformTable[index1];
            float value2 = _waveformTable[index2];
            float interpolatedValue = Mathf.LerpUnclamped(value1, value2, fraction);

            float resultValue = interpolatedValue * _amplitude;

            // Apply chaos effect based on chaos intensity
            float chaosIntensity = ChaosIntensity;
            if (chaosIntensity > 0f)
            {
                resultValue = ApplyChaosEffect(resultValue, chaosIntensity, normalizedPosition);
            }

            return resultValue;
        }

        public void UpdateWaveProperties(WaveformType waveformType, float frequency, float amplitude, float length, float[] waveformTable)
        {
            bool changed = false;

            if (waveformType != _waveformType)
            {
                _waveformType = waveformType;
                GenerateBaseWaveformTable(waveformType);
                changed = true;
            }
            if (frequency != _frequency)
            {
                _frequency = frequency;
                changed = true;
            }
            if (amplitude != _amplitude)
            {
                _amplitude = amplitude;
                changed = true;
            }
            if (length != _length)
            {
                _length = length;
                changed = true;
            }
            if (waveformTable != null && waveformTable.Length == _waveformTable.Length)
            {
                if (_waveformTable == null || !AreTablesEqual(waveformTable, _waveformTable))
                {
                    _waveformTable = waveformTable;
                    changed = true;
                }
            }
            else if (waveformTable != null)
            {
                Debug.LogWarning("Wave: Waveform table length mismatch. Cannot update waveform table.");
            }

            if (changed)
            {
                OnWavePropertiesChanged?.Invoke();
                Debug.Log($"Wave: Wave properties changed. Waveform type: {_waveformType}, Frequency: {_frequency}," +
                          $"Amplitude: {_amplitude}, Length: {_length}");
            }
        }

        private bool AreTablesEqual(float[] table1, float[] table2)
        {
            if (table1 == null || table2 == null || table1.Length != table2.Length) return false;
            for (int i = 0; i < table1.Length; i++)
            {
                if (Mathf.Approximately(table1[i], table2[i])) return false;
            }
            return true;
        }

        private void GenerateBaseWaveformTable(WaveformType waveformType)
        {
            _waveformType = waveformType;
            _waveformTable = WaveformGenerator.Generate(waveformType, WAVEFORM_RESOLUTION);
            OnWavePropertiesChanged?.Invoke();
            Debug.Log($"Wave: Base waveform table generated. Waveform type: {_waveformType}");
        }

        #endregion

        #region Chaos Methods
        
        /// <summary>
        /// Apply chaos effect to wave value based on intensity
        /// At intensity 1: completely random values in [-amplitude, +amplitude] range
        /// At intensity 0: original wave value
        /// Smooth transition between the two states
        /// </summary>
        /// <param name="originalValue">The original wave value</param>
        /// <param name="chaosIntensity">Chaos intensity (0-1)</param>
        /// <param name="normalizedPosition">Normalized position for consistent random seeding</param>
        /// <returns>Modified wave value with chaos effect applied</returns>
        private float ApplyChaosEffect(float originalValue, float chaosIntensity, float normalizedPosition)
        {
            // Generate a completely random value in the amplitude range
            // Use normalizedPosition as seed for consistent randomness per position
            Random.State oldState = Random.state;
            Random.InitState(Mathf.RoundToInt(normalizedPosition * 10000f) + Mathf.RoundToInt(Time.time * 1000f));
            
            float randomValue = Random.Range(-_amplitude, _amplitude);
            
            // Restore original random state
            Random.state = oldState;
            
            // Interpolate between original value and random value based on chaos intensity
            // At intensity 0: return originalValue
            // At intensity 1: return randomValue
            float chaoticValue = Mathf.Lerp(originalValue, randomValue, chaosIntensity);
            
            // Add additional frequency modulation for extra chaos when intensity is high
            if (chaosIntensity > 0.5f)
            {
                float frequencyModulation = Random.Range(0.8f, 1.2f);
                chaoticValue *= Mathf.Lerp(1f, frequencyModulation, (chaosIntensity - 0.5f) * 2f);
            }
            
            return chaoticValue;
        }
        
        /// <summary>
        /// Add chaos value
        /// </summary>
        public float AddChaos(float amount)
        {
            if (amount <= 0f) return 0f;

            float previousChaos = _currentChaos;
            _currentChaos = Mathf.Min(_currentChaos + amount, _maxChaos);
            float actualAdded = _currentChaos - previousChaos;

            if (actualAdded > 0f)
            {
                UpdateChaosState();
                OnChaosChanged?.Invoke(_currentChaos, _maxChaos);
                Debug.Log($"Wave: Added {actualAdded} chaos. Current: {_currentChaos}/{_maxChaos}");
            }

            return actualAdded;
        }
        
        /// <summary>
        /// Update chaos (natural recovery, called every frame)
        /// </summary>
        public void UpdateChaos(float chaosRecoveryRate, float deltaTime)
        {
            if (chaosRecoveryRate >= 0f || _currentChaos <= 0f) return;

            float previousChaos = _currentChaos;
            _currentChaos = Mathf.Max(0f, _currentChaos + chaosRecoveryRate * deltaTime);
            
            if (_currentChaos != previousChaos)
            {
                UpdateChaosState();
                OnChaosChanged?.Invoke(_currentChaos, _maxChaos);
            }
        }
        
        /// <summary>
        /// Update chaos state
        /// </summary>
        private void UpdateChaosState()
        {
            var previousState = _chaosState;
            
            if (_currentChaos >= _maxChaos)
            {
                _chaosState = WaveChaosState.Chaos;
            }
            else if (_currentChaos < _chaosThreshold)
            {
                _chaosState = WaveChaosState.Order;
            }
            // Maintain current state if between threshold and max

            if (previousState != _chaosState)
            {
                OnChaosStateChanged?.Invoke(_chaosState);
                Debug.Log($"Wave: Chaos state changed to {_chaosState}");
            }
        }
        
        /// <summary>
        /// Reset chaos to 0
        /// </summary>
        public void ResetChaos()
        {
            if (_currentChaos > 0f)
            {
                _currentChaos = 0f;
                UpdateChaosState();
                OnChaosChanged?.Invoke(_currentChaos, _maxChaos);
                Debug.Log("Wave: Chaos reset to 0");
            }
        }
        
        #endregion
        
        #region Save/Load
        
        /// <summary>
        /// Get save data
        /// </summary>
        public WaveSaveData GetSaveData()
        {
            return new WaveSaveData
            {
                currentChaos = _currentChaos
            };
        }
        
        /// <summary>
        /// Load from save data
        /// </summary>
        public void LoadFromSaveData(WaveSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("Wave: Cannot load from null save data");
                return;
            }

            _currentChaos = Mathf.Clamp(saveData.currentChaos, 0f, _maxChaos);
            UpdateChaosState();
            OnChaosChanged?.Invoke(_currentChaos, _maxChaos);

            Debug.Log($"Wave: Loaded from save data. Chaos: {_currentChaos}/{_maxChaos}");
        }
        
        #endregion
        
        /// <summary>
        /// Cleanup event subscriptions
        /// </summary>
        public void Cleanup()
        {
            OnChaosChanged = null;
            OnChaosStateChanged = null;
        }
    }

    /// <summary>
    /// Wave save data structure
    /// </summary>
    [System.Serializable]
    public class WaveSaveData
    {
        public float currentChaos;

        public WaveSaveData()
        {
            currentChaos = 0f;
        }
    }
}
