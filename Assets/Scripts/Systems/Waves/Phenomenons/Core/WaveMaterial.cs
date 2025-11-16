using UnityEngine;

namespace Resonance.Systems.Waves.WavePhenomenons
{
    /// <summary>
    /// Wave material properties - defines how waves interact with environment
    /// Used for reflection, absorption, penetration, refraction calculations
    /// </summary>
    [System.Serializable]
    public class WaveMaterial
    {
        [Header("Material Properties")]
        [Tooltip("Reflection coefficient [0-1] - how much wave energy is reflected")]
        [Range(0f, 1f)]
        public float reflectionCoefficient = 0.5f;
        
        [Tooltip("Absorption coefficient [0-1] - how much wave energy is absorbed")]
        [Range(0f, 1f)]
        public float absorptionCoefficient = 0.2f;
        
        [Tooltip("Penetration coefficient [0-1] - how easily waves penetrate")]
        [Range(0f, 1f)]
        public float penetrationCoefficient = 0.3f;
        
        [Tooltip("Refraction index - ratio of wave speeds in different media")]
        [Range(0.1f, 10f)]
        public float refractionIndex = 1.0f;
        
        [Tooltip("Material density - affects wave propagation speed")]
        [Range(0.1f, 10f)]
        public float density = 1.0f;
        
        [Header("Material Name")]
        public string materialName = "Default";
        
        #region Preset Materials
        
        /// <summary>
        /// Air - minimal interaction
        /// </summary>
        public static WaveMaterial Air => new WaveMaterial
        {
            materialName = "Air",
            reflectionCoefficient = 0.0f,
            absorptionCoefficient = 0.0f,
            penetrationCoefficient = 1.0f,
            refractionIndex = 1.0f,
            density = 0.001f
        };
        
        /// <summary>
        /// Metal - high reflection, low penetration
        /// </summary>
        public static WaveMaterial Metal => new WaveMaterial
        {
            materialName = "Metal",
            reflectionCoefficient = 0.9f,
            absorptionCoefficient = 0.1f,
            penetrationCoefficient = 0.1f,
            refractionIndex = 0.5f,
            density = 8.0f
        };
        
        /// <summary>
        /// Glass - high refraction, moderate reflection
        /// </summary>
        public static WaveMaterial Glass => new WaveMaterial
        {
            materialName = "Glass",
            reflectionCoefficient = 0.4f,
            absorptionCoefficient = 0.1f,
            penetrationCoefficient = 0.6f,
            refractionIndex = 1.5f,
            density = 2.5f
        };
        
        /// <summary>
        /// Absorber - high absorption, low reflection
        /// </summary>
        public static WaveMaterial Absorber => new WaveMaterial
        {
            materialName = "Absorber",
            reflectionCoefficient = 0.1f,
            absorptionCoefficient = 0.8f,
            penetrationCoefficient = 0.2f,
            refractionIndex = 0.8f,
            density = 5.0f
        };
        
        /// <summary>
        /// Crystal - special material with unique properties
        /// </summary>
        public static WaveMaterial Crystal => new WaveMaterial
        {
            materialName = "Crystal",
            reflectionCoefficient = 0.3f,
            absorptionCoefficient = 0.2f,
            penetrationCoefficient = 0.7f,
            refractionIndex = 2.0f,
            density = 3.0f
        };
        
        #endregion
        
        /// <summary>
        /// Validate material properties (ensure energy conservation)
        /// </summary>
        public bool Validate()
        {
            // Reflection + Absorption + Penetration should not exceed 1.0
            float total = reflectionCoefficient + absorptionCoefficient + penetrationCoefficient;
            if (total > 1.0f)
            {
                Debug.LogWarning($"WaveMaterial {materialName}: Total interaction coefficients exceed 1.0 ({total:F2}). Normalizing...");
                float scale = 1.0f / total;
                reflectionCoefficient *= scale;
                absorptionCoefficient *= scale;
                penetrationCoefficient *= scale;
            }
            
            return true;
        }
        
        /// <summary>
        /// Get effective reflection probability for a wave
        /// Combines material properties with wave properties
        /// </summary>
        public float GetEffectiveReflectionProbability(Wave wave)
        {
            if (wave == null) return 0f;
            // Material reflection × Wave reflection factor
            return reflectionCoefficient * wave.ReflectionFactor;
        }
        
        /// <summary>
        /// Get effective penetration probability for a wave
        /// </summary>
        public float GetEffectivePenetrationProbability(Wave wave)
        {
            if (wave == null) return 0f;
            // Material penetration × Wave penetration factor
            return penetrationCoefficient * wave.PenetrationFactor;
        }
        
        /// <summary>
        /// Get effective absorption probability for a wave
        /// </summary>
        public float GetEffectiveAbsorptionProbability(Wave wave)
        {
            if (wave == null) return 0f;
            // Material absorption × Wave absorption factor
            return absorptionCoefficient * wave.AbsorptionFactor;
        }
    }
}

