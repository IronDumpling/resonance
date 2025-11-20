using UnityEngine;
using Resonance.Systems.Waves;

namespace Resonance.Systems.Waves.WavePhenomenons
{
    /// <summary>
    /// Wave interaction context - contains all information needed for wave phenomenon calculations
    /// </summary>
    public class WaveInteractionContext
    {
        #region Wave Properties
        
        /// <summary>
        /// The wave being processed
        /// </summary>
        public Wave Wave { get; set; }
        
        /// <summary>
        /// Phase offset for superposition calculations
        /// </summary>
        public float PhaseOffset { get; set; } = 0f;
        
        #endregion
        
        #region Spatial Properties
        
        /// <summary>
        /// Current position of the wave
        /// </summary>
        public Vector3 Position { get; set; }
        
        /// <summary>
        /// Propagation direction (normalized)
        /// </summary>
        public Vector3 Direction { get; set; }
        
        /// <summary>
        /// Distance traveled from origin
        /// </summary>
        public float DistanceTraveled { get; set; } = 0f;
        
        #endregion
        
        #region Environment Properties
        
        /// <summary>
        /// Current environment material
        /// </summary>
        public WaveMaterial Material { get; set; }
        
        /// <summary>
        /// Obstacle GameObject (if interacting with obstacle)
        /// </summary>
        public GameObject Obstacle { get; set; }
        
        /// <summary>
        /// Obstacle surface normal (for reflection/refraction)
        /// </summary>
        public Vector3 SurfaceNormal { get; set; }
        
        /// <summary>
        /// Obstacle thickness (for penetration)
        /// </summary>
        public float ObstacleThickness { get; set; } = 0f;
        
        /// <summary>
        /// Obstacle size (for diffraction)
        /// </summary>
        public float ObstacleSize { get; set; } = 0f;
        
        #endregion
        
        #region Additional Parameters
        
        /// <summary>
        /// Additional parameters for custom calculations
        /// </summary>
        public System.Collections.Generic.Dictionary<string, float> Parameters { get; set; }
        
        #endregion
        
        /// <summary>
        /// Constructor
        /// </summary>
        public WaveInteractionContext(Wave wave, Vector3 position, Vector3 direction)
        {
            Wave = wave;
            Position = position;
            Direction = direction.normalized;
            Material = WaveMaterial.Air;
            Parameters = new System.Collections.Generic.Dictionary<string, float>();
        }
        
        /// <summary>
        /// Create a copy of this context
        /// </summary>
        public WaveInteractionContext Clone()
        {
            var clone = new WaveInteractionContext(Wave, Position, Direction)
            {
                PhaseOffset = PhaseOffset,
                DistanceTraveled = DistanceTraveled,
                Material = Material,
                Obstacle = Obstacle,
                SurfaceNormal = SurfaceNormal,
                ObstacleThickness = ObstacleThickness,
                ObstacleSize = ObstacleSize
            };
            
            // Deep copy parameters
            if (Parameters != null)
            {
                clone.Parameters = new System.Collections.Generic.Dictionary<string, float>(Parameters);
            }
            
            return clone;
        }
    }
}

