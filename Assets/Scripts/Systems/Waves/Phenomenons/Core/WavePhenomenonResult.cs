using UnityEngine;
using Resonance.Systems.Waves;

namespace Resonance.Systems.Waves.WavePhenomenons
{
    /// <summary>
    /// Wave phenomenon result - contains the result of a wave phenomenon calculation
    /// </summary>
    public class WavePhenomenonResult
    {
        #region Wave Result
        
        /// <summary>
        /// Result wave (may be modified or new wave created)
        /// Null if wave is terminated
        /// </summary>
        public Wave ResultWave { get; set; }
        
        /// <summary>
        /// Whether the original wave was modified
        /// </summary>
        public bool IsWaveModified { get; set; } = false;
        
        #endregion
        
        #region Spatial Result
        
        /// <summary>
        /// New position after phenomenon
        /// </summary>
        public Vector3 NewPosition { get; set; }
        
        /// <summary>
        /// New direction after phenomenon
        /// </summary>
        public Vector3 NewDirection { get; set; }
        
        /// <summary>
        /// Total distance traveled
        /// </summary>
        public float DistanceTraveled { get; set; }
        
        #endregion
        
        #region Energy Result
        
        /// <summary>
        /// Remaining energy after phenomenon
        /// </summary>
        public float RemainingEnergy { get; set; }
        
        /// <summary>
        /// Energy lost during phenomenon
        /// </summary>
        public float EnergyLost { get; set; }
        
        /// <summary>
        /// Whether wave is terminated (energy depleted or absorbed)
        /// </summary>
        public bool IsTerminated { get; set; } = false;
        
        #endregion
        
        #region Phenomenon Information
        
        /// <summary>
        /// Type of phenomenon that occurred
        /// </summary>
        public WavePhenomenonType PhenomenonType { get; set; }
        
        /// <summary>
        /// Success rate or match percentage (0-1)
        /// </summary>
        public float SuccessRate { get; set; } = 1.0f;
        
        /// <summary>
        /// Additional parameters for specific phenomena
        /// </summary>
        public System.Collections.Generic.Dictionary<string, float> Parameters { get; set; }
        
        #endregion
        
        /// <summary>
        /// Constructor
        /// </summary>
        public WavePhenomenonResult()
        {
            Parameters = new System.Collections.Generic.Dictionary<string, float>();
        }
        
        /// <summary>
        /// Create a success result (wave continues)
        /// </summary>
        public static WavePhenomenonResult Success(
            Wave wave, 
            Vector3 newPosition, 
            Vector3 newDirection, 
            float remainingEnergy,
            WavePhenomenonType type)
        {
            return new WavePhenomenonResult
            {
                ResultWave = wave,
                NewPosition = newPosition,
                NewDirection = newDirection,
                RemainingEnergy = remainingEnergy,
                IsTerminated = false,
                PhenomenonType = type,
                SuccessRate = 1.0f
            };
        }
        
        /// <summary>
        /// Create a terminated result (wave stopped)
        /// </summary>
        public static WavePhenomenonResult Terminated(
            Vector3 position,
            float energyLost,
            WavePhenomenonType type)
        {
            return new WavePhenomenonResult
            {
                ResultWave = null,
                NewPosition = position,
                RemainingEnergy = 0f,
                EnergyLost = energyLost,
                IsTerminated = true,
                PhenomenonType = type,
                SuccessRate = 0f
            };
        }
        
        /// <summary>
        /// Create a partial result (wave modified but continues)
        /// </summary>
        public static WavePhenomenonResult Partial(
            Wave modifiedWave,
            Vector3 newPosition,
            Vector3 newDirection,
            float remainingEnergy,
            float energyLost,
            float successRate,
            WavePhenomenonType type)
        {
            return new WavePhenomenonResult
            {
                ResultWave = modifiedWave,
                IsWaveModified = true,
                NewPosition = newPosition,
                NewDirection = newDirection,
                RemainingEnergy = remainingEnergy,
                EnergyLost = energyLost,
                IsTerminated = false,
                PhenomenonType = type,
                SuccessRate = successRate
            };
        }
    }
    
    /// <summary>
    /// Wave phenomenon type enumeration
    /// </summary>
    public enum WavePhenomenonType
    {
        None,
        Superposition,    // Wave interference/overlap
        Propagation,      // Wave travel with attenuation
        Reflection,       // Wave bounce off surface
        Penetration,      // Wave pass through material
        Diffraction,      // Wave bend around obstacle
        Refraction,        // Wave change direction at boundary
        Absorption        // Wave absorbed by material
    }
}

