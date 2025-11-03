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
        
        #region Properties

        public WaveformType WaveformType => _waveformType;
        public float Frequency => _frequency;
        public float Amplitude => _amplitude;
        public float Length => _length;
        public float[] WaveformTable => _waveformTable;
        public static int WaveformResolution => WAVEFORM_RESOLUTION;
        
        #endregion
        
        #region Events
        
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
                _waveformType = config.waveformType;
                _frequency = config.frequency;
                _amplitude = config.amplitude;
                _length = config.length;
            }
            else
            {
                _waveformType = WaveformType.Sine;
                _frequency = 1.0f;
                _amplitude = 1.0f;
                _length = 10.0f;
            }

            GenerateBaseWaveformTable(_waveformType);
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
        
        #region Save/Load
        
        /// <summary>
        /// Get save data
        /// </summary>
        public WaveSaveData GetSaveData()
        {
            return new WaveSaveData
            {

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

            Debug.Log($"Wave: Loaded from save data.");
        }
        
        #endregion
        
        /// <summary>
        /// Cleanup event subscriptions
        /// </summary>
        public void Cleanup()
        {
            OnWavePropertiesChanged = null;
        }
    }
}
