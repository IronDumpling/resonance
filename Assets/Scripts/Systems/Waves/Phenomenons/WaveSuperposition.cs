using UnityEngine;
using Resonance.Systems.Waves;
using Resonance.Shared.Types;

namespace Resonance.Systems.Waves.WavePhenomenons
{
    /// <summary>
    /// Wave Superposition - calculates wave interference (constructive/destructive)
    /// Implements the principle of superposition: when two waves meet, they add together
    /// </summary>
    public class WaveSuperposition : IWavePhenomenon
    {
        #region IWavePhenomenon Implementation
        
        public WavePhenomenonType PhenomenonType => WavePhenomenonType.Superposition;
        
        public bool CanApply(WaveInteractionContext context)
        {
            // Superposition requires two waves
            // Check if second wave is provided via parameters
            return context != null && 
                   context.Wave != null && 
                   context.Parameters != null &&
                   context.Parameters.ContainsKey("wave2Reference");
        }
        
        public WavePhenomenonResult Calculate(WaveInteractionContext context)
        {
            if (!CanApply(context))
            {
                Debug.LogError("WaveSuperposition: Cannot apply - invalid context or missing second wave");
                return WavePhenomenonResult.Terminated(
                    context?.Position ?? Vector3.zero, 
                    0f, 
                    WavePhenomenonType.None);
            }
            
            // Get second wave from parameters (stored as object reference hash)
            // Note: In actual implementation, you might want to pass Wave directly
            // For now, we'll use a workaround via Parameters
            Wave wave2 = GetSecondWaveFromContext(context);
            
            if (wave2 == null)
            {
                Debug.LogError("WaveSuperposition: Second wave is null");
                return WavePhenomenonResult.Terminated(
                    context.Position, 
                    0f, 
                    WavePhenomenonType.None);
            }
            
            return CalculateSuperposition(context.Wave, wave2, context.PhaseOffset);
        }
        
        #endregion
        
        #region Public Static Methods
        
        /// <summary>
        /// Calculate superposition of two waves
        /// </summary>
        /// <param name="wave1">First wave</param>
        /// <param name="wave2">Second wave</param>
        /// <param name="phaseOffset">Phase offset between waves [0-1]</param>
        /// <returns>Superposition result with new combined wave</returns>
        public static WavePhenomenonResult CalculateSuperposition(Wave wave1, Wave wave2, float phaseOffset = 0f)
        {
            if (wave1 == null || wave2 == null)
            {
                Debug.LogError("WaveSuperposition: Cannot calculate superposition with null waves");
                return WavePhenomenonResult.Terminated(Vector3.zero, 0f, WavePhenomenonType.None);
            }
            
            // Calculate match percentage (for QTE system)
            float matchPercentage = wave1.CalculateMatchPercentage(wave2, phaseOffset);
            
            // Create superimposed wave
            Wave superimposedWave = CreateSuperimposedWave(wave1, wave2, phaseOffset);
            
            // Calculate resulting energy
            // When waves interfere constructively, energy can increase
            // When waves interfere destructively, energy decreases
            float interferenceFactor = matchPercentage / 100f; // 0-1
            float energyMultiplier = 1f + (interferenceFactor - 0.5f) * 0.5f; // Range: 0.75 - 1.25
            float resultingEnergy = (wave1.EnergyStrength + wave2.EnergyStrength) * 0.5f * energyMultiplier;
            
            // Update superimposed wave's energy (by adjusting unit)
            if (superimposedWave.EnergyStrength > 0)
            {
                float energyRatio = resultingEnergy / superimposedWave.EnergyStrength;
                float newUnit = superimposedWave.Unit * energyRatio;
                superimposedWave.SetUnit(newUnit);
            }
            
            // Determine interference type
            string interferenceType = interferenceFactor > 0.7f ? "Constructive" : 
                                     interferenceFactor < 0.3f ? "Destructive" : "Partial";
            
            var result = WavePhenomenonResult.Partial(
                superimposedWave,
                Vector3.zero, // Position doesn't change for superposition
                Vector3.forward, // Direction doesn't change
                resultingEnergy,
                (wave1.EnergyStrength + wave2.EnergyStrength) - resultingEnergy,
                interferenceFactor,
                WavePhenomenonType.Superposition
            );
            
            // Add additional parameters
            result.Parameters["matchPercentage"] = matchPercentage;
            result.Parameters["interferenceFactor"] = interferenceFactor;
            result.Parameters["interferenceType"] = interferenceType == "Constructive" ? 1f : 
                                                    interferenceType == "Destructive" ? -1f : 0f;
            result.Parameters["wave1Energy"] = wave1.EnergyStrength;
            result.Parameters["wave2Energy"] = wave2.EnergyStrength;
            
            return result;
        }
        
        /// <summary>
        /// Calculate match percentage between two waves (for QTE system)
        /// This is a convenience method that wraps Wave.CalculateMatchPercentage
        /// </summary>
        public static float CalculateMatchPercentage(Wave wave1, Wave wave2, float phaseOffset = 0f)
        {
            if (wave1 == null || wave2 == null) return 0f;
            return wave1.CalculateMatchPercentage(wave2, phaseOffset);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Create a new wave that is the superposition of two waves
        /// Principle: y_combined(t) = y1(t) + y2(t + phaseOffset)
        /// </summary>
        private static Wave CreateSuperimposedWave(Wave wave1, Wave wave2, float phaseOffset)
        {
            // Determine resolution (use higher resolution)
            int resolution = Mathf.Max(wave1.Resolution, wave2.Resolution);
            
            // Create new waveform table
            float[] superimposedTable = new float[resolution];
            
            // Sample and add waves at each point
            for (int i = 0; i < resolution; i++)
            {
                float t = (float)i / (resolution - 1);
                
                // Get values from both waves
                float value1 = wave1.GetWaveValue(t);
                float value2 = wave2.GetWaveValue((t + phaseOffset) % 1.0f);
                
                // Superposition: add the values
                float combinedValue = value1 + value2;
                
                // Normalize to prevent overflow (clamp to reasonable range)
                // Since individual waves are in [-amplitude, amplitude], 
                // combined can be in [-2*maxAmplitude, 2*maxAmplitude]
                float maxAmplitude = Mathf.Max(wave1.Amplitude, wave2.Amplitude);
                combinedValue = Mathf.Clamp(combinedValue, -2f * maxAmplitude, 2f * maxAmplitude);
                
                // Normalize to [-1, 1] range for waveform table
                if (maxAmplitude > 0)
                {
                    combinedValue = combinedValue / (2f * maxAmplitude);
                }
                
                superimposedTable[i] = combinedValue;
            }
            
            // Determine properties of the new wave
            // Use average frequency, combined amplitude, etc.
            float combinedFrequency = (wave1.Frequency + wave2.Frequency) * 0.5f;
            float combinedAmplitude = Mathf.Max(wave1.Amplitude, wave2.Amplitude) * 1.2f; // Slightly higher for constructive interference
            float combinedUnit = (wave1.Unit + wave2.Unit) * 0.5f;
            
            // Create a temporary config with the correct resolution
            // ScriptableObject.CreateInstance works in both editor and runtime
            WaveConfig tempConfig = ScriptableObject.CreateInstance<WaveConfig>();
            tempConfig.waveformType = WaveformType.Custom;
            tempConfig.frequency = combinedFrequency;
            tempConfig.amplitude = combinedAmplitude;
            tempConfig.unit = combinedUnit;
            tempConfig.waveformResolution = resolution;
            
            // Create new wave with the config
            Wave superimposedWave = new Wave(tempConfig);
            
            // Set the custom waveform table (must match resolution)
            superimposedWave.UpdateWaveProperties(
                WaveformType.Custom,
                combinedFrequency,
                combinedAmplitude,
                combinedUnit,
                superimposedTable
            );
            
            // Clean up temporary config
            // In runtime, use Destroy instead of DestroyImmediate
            if (Application.isPlaying)
            {
                Object.Destroy(tempConfig);
            }
            else
            {
                Object.DestroyImmediate(tempConfig);
            }
            
            return superimposedWave;
        }
        
        /// <summary>
        /// Get second wave from context parameters
        /// This is a workaround since we can't store object references in Dictionary
        /// In practice, you might want to extend WaveInteractionContext for superposition
        /// </summary>
        private Wave GetSecondWaveFromContext(WaveInteractionContext context)
        {
            // This is a limitation - we can't easily pass Wave objects through Parameters
            // For now, return null and expect the caller to use CalculateSuperposition directly
            // In a real implementation, you might want to:
            // 1. Extend WaveInteractionContext with a SecondWave property
            // 2. Or use a different mechanism to pass the second wave
            
            if (context.Parameters != null && context.Parameters.ContainsKey("wave2Reference"))
            {
                // This won't work directly - we need a different approach
                // For now, this method is a placeholder
                Debug.LogWarning("WaveSuperposition: Cannot retrieve Wave from Parameters. Use CalculateSuperposition directly with two Wave parameters.");
            }
            
            return null;
        }
        
        #endregion
    }
}

