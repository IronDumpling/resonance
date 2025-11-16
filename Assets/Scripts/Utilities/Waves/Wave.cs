using UnityEngine;
using Resonance.Utilities.Types;

namespace Resonance.Utilities.Waves
{
    /// <summary>
    /// Wave System - manages chaos and QTE configuration
    /// Part of CrystalCore System
    /// Supports both microscopic (waveform-level) and macroscopic (energy-level) properties
    /// </summary>
    [System.Serializable]
    public class Wave
    {
        #region Primary Properties
        
        [Header("Shape & Dimensions")]
        [SerializeField] private WaveformType _waveformType = WaveformType.Sine;
        [SerializeField] private float _frequency = 1.0f;      // Cycles per unit length (Hz equivalent)
        [SerializeField] private float _amplitude = 1.0f;      // Peak amplitude
        [SerializeField] private int _resolution = WaveConstants.DEFAULT_WAVEFORM_RESOLUTION;  // Samples per unit wave (constant)
        [SerializeField] private float _unit = 1.0f;           // Current sample count / resolution (how many unit waves)
        
        [Header("Sampled Representation")]
        [SerializeField] private float[] _waveformTable;       // One full cycle sampled at discrete points [-1, 1]
        
        #endregion
        
        #region Secondary Properties (Calculated)
        
        // Energy properties
        private float _energyStrength;              // Total energy: unit energy × units
        private float _energyAttenuationFactor;     // How quickly energy decays over distance
        
        // Interaction properties
        private float _reflectionFactor;            // Probability/strength of reflection [0-1]
        private float _penetrationFactor;           // Ability to penetrate obstacles [0-1]
        private float _diffractionFactor;           // Ability to bend around obstacles [0-1]
        private float _absorptionFactor;            // How easily absorbed (inverse of reflection)
        
        // Motion properties
        private float _speed;                       // Wave propagation speed
        
        // Cached for performance
        private bool _secondaryPropertiesDirty = true;
        
        #endregion
        
        #region Properties - Primary
        
        public WaveformType WaveformType => _waveformType;
        public float Frequency => _frequency;
        public float Amplitude => _amplitude;
        public int Resolution => _resolution;
        public float Unit => _unit;
        public float[] WaveformTable => _waveformTable;
        public static int WaveformResolution => WaveConstants.DEFAULT_WAVEFORM_RESOLUTION;
        
        #endregion
        
        #region Properties - Secondary
        
        public float EnergyStrength 
        { 
            get 
            { 
                if (_secondaryPropertiesDirty) UpdateSecondaryProperties();
                return _energyStrength; 
            } 
        }
        
        public float EnergyAttenuationFactor 
        { 
            get 
            { 
                if (_secondaryPropertiesDirty) UpdateSecondaryProperties();
                return _energyAttenuationFactor; 
            } 
        }
        
        public float ReflectionFactor 
        { 
            get 
            { 
                if (_secondaryPropertiesDirty) UpdateSecondaryProperties();
                return _reflectionFactor; 
            } 
        }
        
        public float PenetrationFactor 
        { 
            get 
            { 
                if (_secondaryPropertiesDirty) UpdateSecondaryProperties();
                return _penetrationFactor; 
            } 
        }
        
        public float DiffractionFactor 
        { 
            get 
            { 
                if (_secondaryPropertiesDirty) UpdateSecondaryProperties();
                return _diffractionFactor; 
            } 
        }
        
        public float AbsorptionFactor 
        { 
            get 
            { 
                if (_secondaryPropertiesDirty) UpdateSecondaryProperties();
                return _absorptionFactor; 
            } 
        }
        
        public float Speed 
        { 
            get 
            { 
                if (_secondaryPropertiesDirty) UpdateSecondaryProperties();
                return _speed; 
            } 
        }
        
        #endregion
        
        #region Events
        
        public System.Action OnWavePropertiesChanged;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Constructor with WaveConfig
        /// </summary>
        public Wave(WaveConfig config)
        {
            if (config != null)
            {
                _resolution = Mathf.Clamp(config.waveformResolution, WaveConstants.MIN_WAVEFORM_RESOLUTION, WaveConstants.MAX_WAVEFORM_RESOLUTION);
                _waveformTable = new float[_resolution];
                
                _waveformType = config.waveformType;
                _frequency = Mathf.Max(WaveConstants.MIN_FREQUENCY, config.frequency);
                _amplitude = Mathf.Max(WaveConstants.MIN_AMPLITUDE, config.amplitude);
                _unit = Mathf.Max(WaveConstants.MIN_UNIT, config.unit);
            }
            else
            {
                _resolution = WaveConstants.DEFAULT_WAVEFORM_RESOLUTION;
                _waveformTable = new float[_resolution];
                _waveformType = WaveformType.Sine;
                _frequency = 1.0f;
                _amplitude = 1.0f;
                _unit = 1.0f;
            }

            GenerateBaseWaveformTable(_waveformType);
            UpdateSecondaryProperties();
        }
        
        #endregion
        
        #region Primary Property Methods

        public float GetWaveValue(float normalizedPosition)
        {
            if (_waveformTable == null || _waveformTable.Length == 0) return 0f;

            // Calculate the phase within the sampled table
            float phase = (normalizedPosition * _frequency) % 1.0f;
            phase = (phase < 0.0f) ? phase + 1.0f : phase;

            // Map phase to table index with linear interpolation
            float index = phase * (_waveformTable.Length - 1);
            int index1 = Mathf.FloorToInt(index);
            int index2 = (index1 + 1 < _waveformTable.Length) ? index1 + 1 : 0;
            float fraction = index - index1;

            // Get the values at the indices
            float value1 = _waveformTable[index1];
            float value2 = _waveformTable[index2];
            float interpolatedValue = Mathf.LerpUnclamped(value1, value2, fraction);

            return interpolatedValue * _amplitude;
        }

        public void UpdateWaveProperties(WaveformType waveformType, float frequency, float amplitude, float unit, float[] waveformTable = null)
        {
            bool changed = false;

            if (waveformType != _waveformType)
            {
                _waveformType = waveformType;
                GenerateBaseWaveformTable(waveformType);
                changed = true;
            }
            
            if (!Mathf.Approximately(frequency, _frequency))
            {
                _frequency = Mathf.Max(WaveConstants.MIN_FREQUENCY, frequency);
                changed = true;
            }
            
            if (!Mathf.Approximately(amplitude, _amplitude))
            {
                _amplitude = Mathf.Max(WaveConstants.MIN_AMPLITUDE, amplitude);
                changed = true;
            }
            
            if (!Mathf.Approximately(unit, _unit))
            {
                _unit = Mathf.Max(WaveConstants.MIN_UNIT, unit);
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
                _secondaryPropertiesDirty = true;
                OnWavePropertiesChanged?.Invoke();
                Debug.Log($"Wave: Properties changed - Type: {_waveformType}, F: {_frequency:F2}, A: {_amplitude:F2}, U: {_unit:F2}");
            }
        }
        
        /// <summary>
        /// Set unit count (how many unit waves are in this wave instance)
        /// </summary>
        public void SetUnit(float unit)
        {
            float newUnit = Mathf.Max(WaveConstants.MIN_UNIT, unit);
            if (!Mathf.Approximately(_unit, newUnit))
            {
                _unit = newUnit;
                _secondaryPropertiesDirty = true;
                OnWavePropertiesChanged?.Invoke();
            }
        }

        private bool AreTablesEqual(float[] table1, float[] table2)
        {
            if (table1 == null || table2 == null || table1.Length != table2.Length) return false;
            for (int i = 0; i < table1.Length; i++)
            {
                if (!Mathf.Approximately(table1[i], table2[i])) return false;
            }
            return true;
        }

        private void GenerateBaseWaveformTable(WaveformType waveformType)
        {
            _waveformType = waveformType;
            _waveformTable = WaveformGenerator.Generate(waveformType, _resolution);
            _secondaryPropertiesDirty = true;
            OnWavePropertiesChanged?.Invoke();
            Debug.Log($"Wave: Base waveform table generated. Type: {_waveformType}");
        }

        #endregion
        
        #region Secondary Property Calculations
        
        /// <summary>
        /// Update all secondary properties based on primary properties
        /// Called when primary properties change
        /// </summary>
        private void UpdateSecondaryProperties()
        {
            CalculateEnergyProperties();
            CalculateInteractionProperties();
            CalculateMotionProperties();
            
            _secondaryPropertiesDirty = false;
            
            Debug.Log($"Wave Secondary Properties Updated:\n" +
                      $"  Energy: {_energyStrength:F2}, Attenuation: {_energyAttenuationFactor:F3}\n" +
                      $"  Reflection: {_reflectionFactor:F2}, Penetration: {_penetrationFactor:F2}\n" +
                      $"  Diffraction: {_diffractionFactor:F2}, Absorption: {_absorptionFactor:F2}\n" +
                      $"  Speed: {_speed:F2}");
        }
        
        /// <summary>
        /// Calculate energy-related properties
        /// Energy formula: E = A² × f × waveformCoefficient × unit × scale
        /// Handles extreme cases: frequency/amplitude/unit → 0 or ∞
        /// </summary>
        private void CalculateEnergyProperties()
        {
            // Get waveform-specific coefficients
            float waveformEnergyCoeff = WaveConstants.GetEnergyCoefficient(_waveformType);
            float waveformAttenuationCoeff = WaveConstants.GetAttenuationCoefficient(_waveformType);
            
            // Detect extreme states
            bool freqExtremeLow = WaveConstants.IsExtremeLow(_frequency);
            bool freqExtremeHigh = WaveConstants.IsExtremeHigh(_frequency);
            bool ampExtremeLow = WaveConstants.IsExtremeLow(_amplitude);
            bool ampExtremeHigh = WaveConstants.IsExtremeHigh(_amplitude);
            bool unitExtremeLow = WaveConstants.IsExtremeLow(_unit);
            bool unitExtremeHigh = WaveConstants.IsExtremeHigh(_unit);
            
            // Handle extreme cases for energy calculation
            if (freqExtremeLow || ampExtremeLow || unitExtremeLow)
            {
                // Any property → 0: Energy → 0
                _energyStrength = 0f;
            }
            else if (freqExtremeHigh || ampExtremeHigh || unitExtremeHigh)
            {
                // Any property → ∞: Energy → ∞ (clamped to prevent overflow)
                // Use logarithmic scaling for extreme values
                float logFreq = freqExtremeHigh ? Mathf.Log(_frequency, 2.0f) : 1.0f;
                float logAmp = ampExtremeHigh ? Mathf.Log(_amplitude, 2.0f) : 1.0f;
                float logUnit = unitExtremeHigh ? Mathf.Log(_unit, 2.0f) : 1.0f;
                
                // Scale up energy for extreme values
                float extremeMultiplier = (logFreq * logAmp * logUnit) / 100.0f; // Scale down to prevent overflow
                _energyStrength = Mathf.Min(
                    WaveConstants.LIMIT_ENERGY_STRENGTH_MAX,
                    _amplitude * _amplitude * _frequency * waveformEnergyCoeff * _unit * WaveConstants.ENERGY_CALCULATION_SCALE * extremeMultiplier
                );
            }
            else
            {
                // Normal case: E_unit = A² × f × waveformCoeff
                float unitEnergy = _amplitude * _amplitude * _frequency * waveformEnergyCoeff;
                
                // Total energy: E_total = E_unit × unit
                _energyStrength = unitEnergy * _unit * WaveConstants.ENERGY_CALCULATION_SCALE;
            }
            
            // Calculate attenuation with extreme handling
            if (freqExtremeLow)
            {
                // Frequency → 0: Minimal attenuation (wave travels far)
                _energyAttenuationFactor = WaveConstants.MIN_ATTENUATION_FACTOR;
            }
            else if (freqExtremeHigh)
            {
                // Frequency → ∞: Extreme attenuation (instant dissipation)
                _energyAttenuationFactor = WaveConstants.LIMIT_ATTENUATION_FACTOR_MAX;
            }
            else
            {
                // Normal case: attenuation = baseAttenuation × (1 + log(1 + f) × factor)
                float frequencyFactor = WaveConstants.BASE_ATTENUATION + Mathf.Log(1.0f + _frequency, 2.0f) * WaveConstants.ATTENUATION_FREQUENCY_FACTOR;
                _energyAttenuationFactor = waveformAttenuationCoeff * frequencyFactor;
                _energyAttenuationFactor = Mathf.Clamp(_energyAttenuationFactor, WaveConstants.MIN_ATTENUATION_FACTOR, WaveConstants.MAX_ATTENUATION_FACTOR);
            }
            
            // Ensure non-negative
            _energyStrength = Mathf.Max(0f, _energyStrength);
        }
        
        /// <summary>
        /// Calculate wave interaction properties
        /// Based on frequency, amplitude, and waveform type
        /// Handles extreme cases: frequency/amplitude → 0 or ∞
        /// </summary>
        private void CalculateInteractionProperties()
        {
            // Detect extreme states
            bool freqExtremeLow = WaveConstants.IsExtremeLow(_frequency);
            bool freqExtremeHigh = WaveConstants.IsExtremeHigh(_frequency);
            bool ampExtremeLow = WaveConstants.IsExtremeLow(_amplitude);
            bool ampExtremeHigh = WaveConstants.IsExtremeHigh(_amplitude);
            
            // Normalize with extreme handling
            float normalizedFreq;
            float normalizedAmp;
            bool freqIsExtreme, ampIsExtreme;
            
            normalizedFreq = WaveConstants.SafeNormalize(_frequency, WaveConstants.MAX_FREQUENCY_NORMALIZATION, out freqIsExtreme);
            normalizedAmp = WaveConstants.SafeNormalize(_amplitude, WaveConstants.MAX_AMPLITUDE_NORMALIZATION, out ampIsExtreme);
            
            // REFLECTION: Low frequency → High reflection, High frequency → Low reflection
            if (freqExtremeLow)
            {
                // Frequency → 0: Maximum reflection (100%)
                _reflectionFactor = 1.0f;
            }
            else if (freqExtremeHigh)
            {
                // Frequency → ∞: Minimum reflection (0%)
                _reflectionFactor = 0.0f;
            }
            else
            {
                // Normal case: R = (1 - f_norm) × range + min
                float clampedFreq = Mathf.Clamp01(normalizedFreq);
                _reflectionFactor = (1.0f - clampedFreq) * WaveConstants.REFLECTION_FACTOR_RANGE + WaveConstants.REFLECTION_FACTOR_MIN;
            }
            
            // PENETRATION: High amplitude → High penetration
            if (ampExtremeLow)
            {
                // Amplitude → 0: Minimum penetration
                _penetrationFactor = WaveConstants.PENETRATION_FACTOR_MIN;
            }
            else if (ampExtremeHigh)
            {
                // Amplitude → ∞: Maximum penetration (100%)
                _penetrationFactor = 1.0f;
            }
            else
            {
                // Normal case: P = A_norm × range + min
                float clampedAmp = Mathf.Clamp01(normalizedAmp);
                _penetrationFactor = clampedAmp * WaveConstants.PENETRATION_FACTOR_RANGE + WaveConstants.PENETRATION_FACTOR_MIN;
            }
            
            // DIFFRACTION: Low frequency → High diffraction
            if (freqExtremeLow)
            {
                // Frequency → 0: Maximum diffraction (100%)
                _diffractionFactor = 1.0f;
            }
            else if (freqExtremeHigh)
            {
                // Frequency → ∞: Minimum diffraction
                _diffractionFactor = WaveConstants.DIFFRACTION_FACTOR_MIN;
            }
            else
            {
                // Normal case: D = (1 - f_norm) × range + min
                float clampedFreq = Mathf.Clamp01(normalizedFreq);
                _diffractionFactor = (1.0f - clampedFreq) * WaveConstants.DIFFRACTION_FACTOR_RANGE + WaveConstants.DIFFRACTION_FACTOR_MIN;
            }
            
            // ABSORPTION: Inverse of reflection (energy conservation)
            // High reflection = Low absorption, High frequency = High absorption
            if (freqExtremeLow)
            {
                // Frequency → 0: Low absorption (high reflection)
                _absorptionFactor = 0.0f;
            }
            else if (freqExtremeHigh)
            {
                // Frequency → ∞: Maximum absorption (100%)
                _absorptionFactor = 1.0f;
            }
            else
            {
                // Normal case: A = 1 - R × scale
                _absorptionFactor = 1.0f - _reflectionFactor * WaveConstants.ABSORPTION_REFLECTION_SCALE;
                
                // Apply waveform modifiers (complex waveforms are absorbed more easily)
                float waveformComplexity = WaveConstants.GetComplexityFactor(_waveformType);
                _absorptionFactor *= waveformComplexity;
            }
            
            // Clamp to valid range
            _absorptionFactor = Mathf.Clamp01(_absorptionFactor);
            
            // Ensure physical constraints (reflection + absorption should balance)
            // For extreme cases, allow full values; for normal cases, normalize
            if (!freqExtremeLow && !freqExtremeHigh)
            {
                float totalInteraction = _reflectionFactor + _absorptionFactor;
                if (totalInteraction > WaveConstants.MAX_TOTAL_INTERACTION)
                {
                    float scale = WaveConstants.MAX_TOTAL_INTERACTION / totalInteraction;
                    _reflectionFactor *= scale;
                    _absorptionFactor *= scale;
                }
            }
        }
        
        /// <summary>
        /// Calculate wave motion properties
        /// Speed is affected by energy density
        /// Handles extreme cases: energy/unit → 0 or ∞
        /// </summary>
        private void CalculateMotionProperties()
        {
            // Detect extreme states
            bool unitExtremeLow = WaveConstants.IsExtremeLow(_unit);
            bool unitExtremeHigh = WaveConstants.IsExtremeHigh(_unit);
            bool energyExtremeHigh = WaveConstants.IsExtremeHigh(_energyStrength);
            
            // Handle extreme cases
            if (unitExtremeLow && energyExtremeHigh)
            {
                // Unit → 0 AND Energy → ∞: Extreme energy density
                // Wave becomes unstable and nearly stationary (explosive behavior)
                _speed = WaveConstants.LIMIT_SPEED_MIN;
            }
            else if (unitExtremeLow)
            {
                // Unit → 0: Very high energy density → Very slow speed
                // Use logarithmic scaling to prevent division issues
                float safeUnit = Mathf.Max(_unit, WaveConstants.EXTREME_LOW_THRESHOLD);
                float energyDensity = (_energyStrength / safeUnit) / WaveConstants.ENERGY_CALCULATION_SCALE;
                float speedReductionFactor = 1.0f + energyDensity * WaveConstants.SPEED_REDUCTION_FACTOR * 10.0f; // Amplify effect
                _speed = WaveConstants.BASE_WAVE_SPEED / speedReductionFactor;
                _speed = Mathf.Max(_speed, WaveConstants.LIMIT_SPEED_MIN);
            }
            else if (unitExtremeHigh && energyExtremeHigh)
            {
                // Unit → ∞ AND Energy → ∞: Massive wave, but spread out
                // Speed approaches base speed (energy is distributed)
                _speed = WaveConstants.BASE_WAVE_SPEED;
            }
            else if (energyExtremeHigh)
            {
                // Energy → ∞: High energy density → Slow speed
                float energyDensity = (_energyStrength / _unit) / WaveConstants.ENERGY_CALCULATION_SCALE;
                // Use logarithmic scaling for extreme energy
                float logEnergyDensity = Mathf.Log(1.0f + energyDensity, 2.0f);
                float speedReductionFactor = 1.0f + logEnergyDensity * WaveConstants.SPEED_REDUCTION_FACTOR;
                _speed = WaveConstants.BASE_WAVE_SPEED / speedReductionFactor;
                _speed = Mathf.Max(_speed, WaveConstants.LIMIT_SPEED_MIN);
            }
            else
            {
                // Normal case: Higher energy density → Slower speed
                // Formula: v = v_base / (1 + E_density × factor)
                float energyDensity = _unit > 0 ? (_energyStrength / _unit) / WaveConstants.ENERGY_CALCULATION_SCALE : 1.0f;
                float speedReductionFactor = 1.0f + energyDensity * WaveConstants.SPEED_REDUCTION_FACTOR;
                
                _speed = WaveConstants.BASE_WAVE_SPEED / speedReductionFactor;
            }
            
            // Clamp to reasonable range (allowing extreme values)
            _speed = Mathf.Clamp(_speed, 
                WaveConstants.LIMIT_SPEED_MIN, 
                WaveConstants.LIMIT_SPEED_MAX);
        }
        
        /// <summary>
        /// Get effective range based on energy and attenuation
        /// Useful for gameplay: how far can this wave travel before dissipating?
        /// Handles extreme cases: attenuation → 0 or ∞
        /// </summary>
        public float GetEffectiveRange()
        {
            // Detect extreme attenuation
            bool attenuationExtremeLow = WaveConstants.IsExtremeLow(_energyAttenuationFactor);
            bool attenuationExtremeHigh = _energyAttenuationFactor >= WaveConstants.LIMIT_ATTENUATION_FACTOR_MAX;
            
            if (_energyAttenuationFactor <= 0 || attenuationExtremeLow)
            {
                // Attenuation → 0: Wave travels infinitely far
                return WaveConstants.LIMIT_EFFECTIVE_RANGE_MAX;
            }
            
            if (attenuationExtremeHigh)
            {
                // Attenuation → ∞: Wave dissipates instantly (nearly zero range)
                return WaveConstants.LIMIT_EFFECTIVE_RANGE_MIN;
            }
            
            // Normal case: Range where energy drops to threshold of original
            // E(d) = E₀ × e^(-attenuation × d)
            // threshold = e^(-attenuation × d)
            // d = -ln(threshold) / attenuation
            float range = WaveConstants.EFFECTIVE_RANGE_LN_FACTOR / _energyAttenuationFactor;
            
            // Clamp to reasonable range
            return Mathf.Clamp(range, WaveConstants.LIMIT_EFFECTIVE_RANGE_MIN, WaveConstants.LIMIT_EFFECTIVE_RANGE_MAX);
        }
        
        /// <summary>
        /// Get energy at distance (accounting for attenuation)
        /// Handles extreme cases: energy/attenuation → 0 or ∞
        /// </summary>
        public float GetEnergyAtDistance(float distance)
        {
            if (distance < 0f) return _energyStrength;
            
            // Detect extreme states
            bool attenuationExtremeHigh = _energyAttenuationFactor >= WaveConstants.LIMIT_ATTENUATION_FACTOR_MAX;
            
            if (attenuationExtremeHigh)
            {
                // Extreme attenuation: Energy drops to near zero instantly
                // Use exponential decay with very high rate
                return _energyStrength * Mathf.Exp(-WaveConstants.LIMIT_ATTENUATION_FACTOR_MAX * distance);
            }
            
            // Normal case: E(d) = E₀ × e^(-attenuation × d)
            float energyAtDistance = _energyStrength * Mathf.Exp(-_energyAttenuationFactor * distance);
            
            // Prevent negative values
            return Mathf.Max(0f, energyAtDistance);
        }
        
        /// <summary>
        /// Calculate wave match percentage with another wave
        /// Used for superposition/interference calculations
        /// </summary>
        public float CalculateMatchPercentage(Wave otherWave, float phaseOffset = 0f)
        {
            if (otherWave == null || _waveformTable == null || otherWave._waveformTable == null)
                return 0f;
            
            float totalDifference = 0f;
            int sampleCount = Mathf.Min(_resolution, otherWave._resolution);
            
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                float thisValue = GetWaveValue(t);
                float otherValue = otherWave.GetWaveValue((t + phaseOffset) % 1.0f);
                
                // Normalize by amplitudes for fair comparison
                float thisNorm = _amplitude > 0 ? thisValue / _amplitude : 0;
                float otherNorm = otherWave._amplitude > 0 ? otherValue / otherWave._amplitude : 0;
                
                totalDifference += Mathf.Abs(thisNorm - otherNorm);
            }
            
            float avgDifference = totalDifference / sampleCount;
            float matchPercentage = Mathf.Clamp01(1f - (avgDifference / 2f)) * 100f;
            
            return matchPercentage;
        }
        
        /// <summary>
        /// Check if wave is in extreme state (unstable/explosive)
        /// Returns true if wave has extreme energy and attenuation that could cause explosive behavior
        /// </summary>
        public bool IsExtremeState()
        {
            if (_secondaryPropertiesDirty) UpdateSecondaryProperties();
            
            // Extreme state: High energy + High attenuation + Low unit = Explosive
            bool energyExtreme = WaveConstants.IsExtremeHigh(_energyStrength) || _energyStrength >= WaveConstants.LIMIT_ENERGY_STRENGTH_MAX * 0.1f;
            bool attenuationExtreme = _energyAttenuationFactor >= WaveConstants.LIMIT_ATTENUATION_FACTOR_MAX * 0.5f;
            bool unitExtreme = WaveConstants.IsExtremeLow(_unit);
            
            // Also check if speed is extremely low (nearly stationary = unstable)
            bool speedExtreme = _speed <= WaveConstants.LIMIT_SPEED_MIN * 2.0f;
            
            return (energyExtreme && attenuationExtreme && unitExtreme) || 
                   (energyExtreme && speedExtreme);
        }
        
        /// <summary>
        /// Get extreme state severity (0-1)
        /// 0 = normal, 1 = maximum extreme (instant explosion)
        /// </summary>
        public float GetExtremeStateSeverity()
        {
            if (_secondaryPropertiesDirty) UpdateSecondaryProperties();
            
            float severity = 0f;
            
            // Energy contribution (0-0.4)
            if (_energyStrength > 0)
            {
                float energyNormalized = Mathf.Clamp01(_energyStrength / (WaveConstants.LIMIT_ENERGY_STRENGTH_MAX * 0.1f));
                severity += energyNormalized * 0.4f;
            }
            
            // Attenuation contribution (0-0.3)
            float attenuationNormalized = Mathf.Clamp01(_energyAttenuationFactor / WaveConstants.LIMIT_ATTENUATION_FACTOR_MAX);
            severity += attenuationNormalized * 0.3f;
            
            // Unit contribution (0-0.2) - lower unit = higher severity
            if (_unit > 0)
            {
                float unitNormalized = 1.0f - Mathf.Clamp01(_unit / WaveConstants.EXTREME_LOW_THRESHOLD);
                severity += unitNormalized * 0.2f;
            }
            
            // Speed contribution (0-0.1) - lower speed = higher severity
            float speedNormalized = 1.0f - Mathf.Clamp01((_speed - WaveConstants.LIMIT_SPEED_MIN) / (WaveConstants.BASE_WAVE_SPEED - WaveConstants.LIMIT_SPEED_MIN));
            severity += speedNormalized * 0.1f;
            
            return Mathf.Clamp01(severity);
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
                waveformType = _waveformType,
                frequency = _frequency,
                amplitude = _amplitude,
                unit = _unit,
                waveformTable = _waveformTable
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

            _waveformType = saveData.waveformType;
            _frequency = Mathf.Max(WaveConstants.MIN_FREQUENCY, saveData.frequency);
            _amplitude = Mathf.Max(WaveConstants.MIN_AMPLITUDE, saveData.amplitude);
            _unit = Mathf.Max(WaveConstants.MIN_UNIT, saveData.unit);
            
            if (saveData.waveformTable != null && saveData.waveformTable.Length == _resolution)
            {
                _waveformTable = saveData.waveformTable;
            }
            else
            {
                GenerateBaseWaveformTable(_waveformType);
            }
            
            UpdateSecondaryProperties();
            OnWavePropertiesChanged?.Invoke();
            
            Debug.Log($"Wave: Loaded from save data - Type: {_waveformType}, F: {_frequency:F2}, A: {_amplitude:F2}, U: {_unit:F2}");
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
