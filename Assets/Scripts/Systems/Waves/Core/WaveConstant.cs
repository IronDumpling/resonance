using Resonance.Shared.Types;
using System.Collections.Generic;

namespace Resonance.Systems.Waves
{
    /// <summary>
    /// Wave system constants and configuration
    /// Contains all hardcoded values and lookup tables for wave calculations
    /// </summary>
    public static class WaveConstants
    {
        #region Physical Constants

        /// <summary>
        /// Base wave speed in units/second
        /// </summary>
        public const float BASE_WAVE_SPEED = 10.0f;

        /// <summary>
        /// Scale factor for energy calculations
        /// </summary>
        public const float ENERGY_CALCULATION_SCALE = 100.0f;

        /// <summary>
        /// Default waveform resolution (samples per unit wave)
        /// </summary>
        public const int DEFAULT_WAVEFORM_RESOLUTION = 1024;

        /// <summary>
        /// Minimum waveform resolution
        /// </summary>
        public const int MIN_WAVEFORM_RESOLUTION = 64;

        /// <summary>
        /// Maximum waveform resolution
        /// </summary>
        public const int MAX_WAVEFORM_RESOLUTION = 2048;

        /// <summary>
        /// Minimum frequency value
        /// </summary>
        public const float MIN_FREQUENCY = 0.1f;

        /// <summary>
        /// Maximum frequency for normalization (typical range)
        /// </summary>
        public const float MAX_FREQUENCY_NORMALIZATION = 10.0f;

        /// <summary>
        /// Maximum amplitude for normalization (typical range)
        /// </summary>
        public const float MAX_AMPLITUDE_NORMALIZATION = 10.0f;

        /// <summary>
        /// Minimum amplitude value
        /// </summary>
        public const float MIN_AMPLITUDE = 0.1f;

        /// <summary>
        /// Minimum unit value
        /// </summary>
        public const float MIN_UNIT = 0.1f;

        #endregion

        #region Waveform Energy Coefficients

        /// <summary>
        /// Waveform energy coefficients based on harmonic content
        /// Higher values indicate more energy per unit wave
        /// </summary>
        public static readonly Dictionary<WaveformType, float> WAVEFORM_ENERGY_COEFFICIENTS = new Dictionary<WaveformType, float>
        {
            { WaveformType.Sine, 1.0f },          // Pure sine - baseline
            { WaveformType.Square, 1.6f },        // Rich in harmonics
            { WaveformType.Triangle, 1.3f },       // Moderate harmonics
            { WaveformType.Sawtooth, 1.5f },      // Rich in harmonics
            { WaveformType.Pulse, 1.2f },         // Moderate
            { WaveformType.Constant, 0.1f },      // Minimal energy
            { WaveformType.Custom, 1.0f }         // Variable
        };

        #endregion

        #region Waveform Attenuation Coefficients

        /// <summary>
        /// Waveform attenuation coefficients
        /// Higher values indicate faster energy decay over distance
        /// </summary>
        public static readonly Dictionary<WaveformType, float> WAVEFORM_ATTENUATION_COEFFICIENTS = new Dictionary<WaveformType, float>
        {
            { WaveformType.Sine, 0.05f },         // Low attenuation
            { WaveformType.Square, 0.15f },       // High attenuation (energy disperses quickly)
            { WaveformType.Triangle, 0.10f },     // Moderate attenuation
            { WaveformType.Sawtooth, 0.12f },     // Moderate-high attenuation
            { WaveformType.Pulse, 0.08f },        // Low-moderate attenuation
            { WaveformType.Constant, 0.01f },     // Minimal attenuation
            { WaveformType.Custom, 0.10f }        // Variable
        };

        #endregion

        #region Waveform Complexity Factors

        /// <summary>
        /// Waveform complexity factors for absorption calculations
        /// Complex waveforms are absorbed more easily
        /// </summary>
        public static readonly Dictionary<WaveformType, float> WAVEFORM_COMPLEXITY_FACTORS = new Dictionary<WaveformType, float>
        {
            { WaveformType.Sine, 1.0f },
            { WaveformType.Square, 1.3f },
            { WaveformType.Triangle, 1.15f },
            { WaveformType.Sawtooth, 1.2f },
            { WaveformType.Pulse, 1.1f },
            { WaveformType.Constant, 0.8f },
            { WaveformType.Custom, 1.0f }
        };

        #endregion

        #region Calculation Parameters

        /// <summary>
        /// Frequency factor multiplier for attenuation calculation
        /// </summary>
        public const float ATTENUATION_FREQUENCY_FACTOR = 0.3f;

        /// <summary>
        /// Base attenuation value
        /// </summary>
        public const float BASE_ATTENUATION = 1.0f;

        /// <summary>
        /// Reflection factor range [min, max]
        /// </summary>
        public const float REFLECTION_FACTOR_MIN = 0.1f;
        public const float REFLECTION_FACTOR_RANGE = 0.9f;

        /// <summary>
        /// Penetration factor range [min, max]
        /// </summary>
        public const float PENETRATION_FACTOR_MIN = 0.1f;
        public const float PENETRATION_FACTOR_RANGE = 0.9f;

        /// <summary>
        /// Diffraction factor range [min, max]
        /// </summary>
        public const float DIFFRACTION_FACTOR_MIN = 0.1f;
        public const float DIFFRACTION_FACTOR_RANGE = 0.9f;

        /// <summary>
        /// Absorption factor scaling (inverse relationship with reflection)
        /// </summary>
        public const float ABSORPTION_REFLECTION_SCALE = 0.9f;

        /// <summary>
        /// Maximum total interaction factor (reflection + absorption)
        /// </summary>
        public const float MAX_TOTAL_INTERACTION = 1.8f;

        /// <summary>
        /// Speed reduction factor for energy density calculation
        /// </summary>
        public const float SPEED_REDUCTION_FACTOR = 0.05f;

        /// <summary>
        /// Speed clamp range [min, max] as multipliers of base speed
        /// </summary>
        public const float SPEED_MIN_MULTIPLIER = 0.3f;
        public const float SPEED_MAX_MULTIPLIER = 1.5f;

        /// <summary>
        /// Energy threshold for effective range calculation (10% of original)
        /// </summary>
        public const float EFFECTIVE_RANGE_ENERGY_THRESHOLD = 0.1f;

        /// <summary>
        /// Natural logarithm of energy threshold for range calculation
        /// </summary>
        public const float EFFECTIVE_RANGE_LN_FACTOR = 2.302585f; // -ln(0.1)

        /// <summary>
        /// Minimum attenuation factor to prevent division by zero
        /// </summary>
        public const float MIN_ATTENUATION_FACTOR = 0.01f;

        /// <summary>
        /// Maximum attenuation factor
        /// </summary>
        public const float MAX_ATTENUATION_FACTOR = 0.5f;

        /// <summary>
        /// Extreme value thresholds for limit state detection
        /// Values beyond these thresholds are considered "extreme"
        /// </summary>
        public const float EXTREME_LOW_THRESHOLD = 0.001f;      // Very close to zero
        public const float EXTREME_HIGH_THRESHOLD = 1000.0f;    // Very large value

        /// <summary>
        /// Limit state values for extreme cases
        /// </summary>
        public const float LIMIT_ENERGY_STRENGTH_MAX = float.MaxValue * 0.1f;  // Prevent overflow
        public const float LIMIT_ATTENUATION_FACTOR_MAX = 10.0f;  // Extreme attenuation (instant dissipation)
        public const float LIMIT_SPEED_MIN = 0.001f;            // Nearly stationary
        public const float LIMIT_SPEED_MAX = 1000.0f;           // Extremely fast
        public const float LIMIT_EFFECTIVE_RANGE_MIN = 0.001f;  // Nearly zero range
        public const float LIMIT_EFFECTIVE_RANGE_MAX = float.MaxValue * 0.1f;  // Infinite range

        /// <summary>
        /// Extreme state detection: check if value is approaching zero
        /// </summary>
        public static bool IsExtremeLow(float value)
        {
            return value > 0f && value < EXTREME_LOW_THRESHOLD;
        }

        /// <summary>
        /// Extreme state detection: check if value is approaching infinity
        /// </summary>
        public static bool IsExtremeHigh(float value)
        {
            return value > EXTREME_HIGH_THRESHOLD && !float.IsInfinity(value) && !float.IsNaN(value);
        }

        /// <summary>
        /// Safe normalization that handles extreme values
        /// Returns values beyond [0,1] for extreme cases
        /// </summary>
        public static float SafeNormalize(float value, float maxValue, out bool isExtreme)
        {
            isExtreme = false;

            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                isExtreme = value <= 0f;
                return 0f;
            }

            if (IsExtremeLow(value))
            {
                isExtreme = true;
                return 0f;
            }

            if (IsExtremeHigh(value))
            {
                isExtreme = true;
                // Return value > 1.0 to indicate extreme high state
                return (value / maxValue) * 10.0f; // Scale up for extreme values
            }

            return value / maxValue;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get energy coefficient for a waveform type
        /// </summary>
        public static float GetEnergyCoefficient(WaveformType waveformType)
        {
            return WAVEFORM_ENERGY_COEFFICIENTS.TryGetValue(waveformType, out float coeff) ? coeff : 1.0f;
        }

        /// <summary>
        /// Get attenuation coefficient for a waveform type
        /// </summary>
        public static float GetAttenuationCoefficient(WaveformType waveformType)
        {
            return WAVEFORM_ATTENUATION_COEFFICIENTS.TryGetValue(waveformType, out float coeff) ? coeff : 0.1f;
        }

        /// <summary>
        /// Get complexity factor for a waveform type
        /// </summary>
        public static float GetComplexityFactor(WaveformType waveformType)
        {
            return WAVEFORM_COMPLEXITY_FACTORS.TryGetValue(waveformType, out float factor) ? factor : 1.0f;
        }

        #endregion
    }
}