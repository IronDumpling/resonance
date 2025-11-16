using UnityEngine;
using System.Collections.Generic;
using Resonance.Utilities.Waves;

namespace Resonance.Utilities.Waves.WavePhenomenons
{
    /// <summary>
    /// Wave Phenomenon Manager - unified interface for all wave phenomena calculations
    /// Provides convenient methods for common wave interactions
    /// </summary>
    public static class WavePhenomenonManager
    {
        #region Phenomenon Instances
        
        private static readonly WaveSuperposition _superposition = new WaveSuperposition();
        
        #endregion
        
        #region Superposition Methods
        
        /// <summary>
        /// Calculate wave superposition (interference) between two waves
        /// </summary>
        /// <param name="wave1">First wave</param>
        /// <param name="wave2">Second wave</param>
        /// <param name="phaseOffset">Phase offset between waves [0-1]</param>
        /// <returns>Superposition result</returns>
        public static WavePhenomenonResult CalculateSuperposition(Wave wave1, Wave wave2, float phaseOffset = 0f)
        {
            if (wave1 == null || wave2 == null)
            {
                Debug.LogError("WavePhenomenonManager: Cannot calculate superposition with null waves");
                return WavePhenomenonResult.Terminated(Vector3.zero, 0f, WavePhenomenonType.None);
            }
            
            // Use WaveSuperposition to calculate superposition
            return WaveSuperposition.CalculateSuperposition(wave1, wave2, phaseOffset);
        }
        
        /// <summary>
        /// Calculate wave match percentage (for QTE system)
        /// </summary>
        /// <param name="wave1">First wave</param>
        /// <param name="wave2">Second wave</param>
        /// <param name="phaseOffset">Phase offset [0-1]</param>
        /// <returns>Match percentage [0-100]</returns>
        public static float CalculateMatchPercentage(Wave wave1, Wave wave2, float phaseOffset = 0f)
        {
            if (wave1 == null || wave2 == null) return 0f;
            return wave1.CalculateMatchPercentage(wave2, phaseOffset);
        }
        
        #endregion
        
        #region Propagation Methods
        
        /// <summary>
        /// Calculate wave propagation over distance
        /// </summary>
        /// <param name="wave">Wave to propagate</param>
        /// <param name="startPosition">Starting position</param>
        /// <param name="direction">Propagation direction</param>
        /// <param name="distance">Distance to propagate</param>
        /// <param name="material">Environment material (default: Air)</param>
        /// <param name="mode">Propagation mode (default: Directional)</param>
        /// <returns>Propagation result</returns>
        public static WavePhenomenonResult CalculatePropagation(
            Wave wave, 
            Vector3 startPosition, 
            Vector3 direction, 
            float distance,
            WaveMaterial material = null,
            WavePropagationMode mode = WavePropagationMode.Directional)
        {
            if (wave == null)
            {
                Debug.LogError("WavePhenomenonManager: Cannot propagate null wave");
                return WavePhenomenonResult.Terminated(startPosition, 0f, WavePhenomenonType.None);
            }
            
            var context = new WaveInteractionContext(wave, startPosition, direction)
            {
                DistanceTraveled = 0f,
                Material = material ?? WaveMaterial.Air
            };
            context.Parameters["propagationDistance"] = distance;
            context.Parameters["propagationMode"] = (float)mode;
            
            // Use WavePropagation to calculate propagation
            return WavePropagation.CalculatePropagationStep(
                wave,
                startPosition,
                direction,
                0f,
                mode,
                distance);
        }
        
        /// <summary>
        /// Calculate energy at a specific point in space
        /// </summary>
        /// <param name="wave">Wave to propagate</param>
        /// <param name="origin">Wave origin point</param>
        /// <param name="targetPoint">Point to calculate energy at</param>
        /// <param name="mode">Propagation mode</param>
        /// <param name="propagationDirection">Initial propagation direction (for directional mode)</param>
        /// <returns>Energy value at target point</returns>
        public static float CalculateEnergyAtPoint(
            Wave wave,
            Vector3 origin,
            Vector3 targetPoint,
            WavePropagationMode mode = WavePropagationMode.Directional,
            Vector3 propagationDirection = default)
        {
            if (wave == null) return 0f;
            return WavePropagation.CalculateEnergyAtPoint(wave, origin, targetPoint, mode, propagationDirection);
        }
        
        /// <summary>
        /// Get energy at distance (without creating result object)
        /// Uses directional mode by default
        /// </summary>
        public static float GetEnergyAtDistance(Wave wave, float distance)
        {
            if (wave == null) return 0f;
            return wave.GetEnergyAtDistance(distance);
        }
        
        /// <summary>
        /// Get effective range of wave
        /// </summary>
        public static float GetEffectiveRange(Wave wave)
        {
            if (wave == null) return 0f;
            return wave.GetEffectiveRange();
        }
        
        /// <summary>
        /// Get beam spread angle at a given distance
        /// Used for visualizing cone/shotgun spread
        /// </summary>
        public static float GetBeamSpreadAngle(Wave wave, float distance)
        {
            if (wave == null) return 0f;
            return WavePropagation.GetBeamSpreadAngle(wave, distance);
        }
        
        /// <summary>
        /// Check if a point is within the affected area
        /// </summary>
        public static bool IsPointAffected(
            Wave wave,
            Vector3 origin,
            Vector3 point,
            WavePropagationMode mode,
            Vector3 direction,
            float energyThreshold = 1f)
        {
            if (wave == null) return false;
            return WavePropagation.IsPointAffected(wave, origin, point, mode, direction, energyThreshold);
        }
        
        /// <summary>
        /// Get recommended propagation mode based on wave properties
        /// </summary>
        public static WavePropagationMode GetRecommendedPropagationMode(Wave wave)
        {
            if (wave == null) return WavePropagationMode.Directional;
            return WavePropagation.GetRecommendedMode(wave);
        }
        
        #endregion
        
        #region Reflection Methods
        
        /// <summary>
        /// Calculate wave reflection off a surface
        /// </summary>
        /// <param name="wave">Incident wave</param>
        /// <param name="position">Reflection point</param>
        /// <param name="incidentDirection">Incident direction</param>
        /// <param name="surfaceNormal">Surface normal</param>
        /// <param name="material">Surface material</param>
        /// <returns>Reflection result</returns>
        public static WavePhenomenonResult CalculateReflection(
            Wave wave,
            Vector3 position,
            Vector3 incidentDirection,
            Vector3 surfaceNormal,
            WaveMaterial material)
        {
            if (wave == null || material == null)
            {
                Debug.LogError("WavePhenomenonManager: Cannot calculate reflection with null wave or material");
                return WavePhenomenonResult.Terminated(position, 0f, WavePhenomenonType.None);
            }
            
            var context = new WaveInteractionContext(wave, position, incidentDirection)
            {
                SurfaceNormal = surfaceNormal.normalized,
                Material = material
            };
            
            // TODO: Implement when WaveReflection is created
            // Placeholder: simple reflection calculation
            Vector3 reflectedDirection = Vector3.Reflect(incidentDirection.normalized, surfaceNormal.normalized);
            float reflectionEnergy = wave.EnergyStrength * material.GetEffectiveReflectionProbability(wave);
            
            return WavePhenomenonResult.Partial(
                wave,
                position,
                reflectedDirection,
                reflectionEnergy,
                wave.EnergyStrength - reflectionEnergy,
                material.GetEffectiveReflectionProbability(wave),
                WavePhenomenonType.Reflection);
        }
        
        #endregion
        
        #region Penetration Methods
        
        /// <summary>
        /// Calculate wave penetration through material
        /// </summary>
        /// <param name="wave">Wave to penetrate</param>
        /// <param name="position">Penetration point</param>
        /// <param name="direction">Penetration direction</param>
        /// <param name="material">Material to penetrate</param>
        /// <param name="thickness">Material thickness</param>
        /// <returns>Penetration result</returns>
        public static WavePhenomenonResult CalculatePenetration(
            Wave wave,
            Vector3 position,
            Vector3 direction,
            WaveMaterial material,
            float thickness)
        {
            if (wave == null || material == null)
            {
                Debug.LogError("WavePhenomenonManager: Cannot calculate penetration with null wave or material");
                return WavePhenomenonResult.Terminated(position, 0f, WavePhenomenonType.None);
            }
            
            var context = new WaveInteractionContext(wave, position, direction)
            {
                Material = material,
                ObstacleThickness = thickness
            };
            
            // TODO: Implement when WavePenetration is created
            // Placeholder: simple penetration calculation
            float penetrationProb = material.GetEffectivePenetrationProbability(wave);
            float penetrationEnergy = wave.EnergyStrength * penetrationProb;
            
            // Energy loss based on thickness
            float energyLoss = penetrationEnergy * (1f - Mathf.Exp(-thickness * 0.1f));
            float remainingEnergy = penetrationEnergy - energyLoss;
            
            if (remainingEnergy <= 0f)
            {
                return WavePhenomenonResult.Terminated(position, wave.EnergyStrength, WavePhenomenonType.Penetration);
            }
            
            return WavePhenomenonResult.Partial(
                wave,
                position + direction.normalized * thickness,
                direction,
                remainingEnergy,
                energyLoss,
                penetrationProb,
                WavePhenomenonType.Penetration);
        }
        
        #endregion
        
        #region Diffraction Methods
        
        /// <summary>
        /// Calculate wave diffraction around obstacle
        /// </summary>
        /// <param name="wave">Wave to diffract</param>
        /// <param name="position">Diffraction point</param>
        /// <param name="direction">Original direction</param>
        /// <param name="obstacleSize">Obstacle size</param>
        /// <param name="obstaclePosition">Obstacle position</param>
        /// <returns>List of diffracted waves (may be multiple)</returns>
        public static List<WavePhenomenonResult> CalculateDiffraction(
            Wave wave,
            Vector3 position,
            Vector3 direction,
            float obstacleSize,
            Vector3 obstaclePosition)
        {
            if (wave == null)
            {
                Debug.LogError("WavePhenomenonManager: Cannot calculate diffraction with null wave");
                return new List<WavePhenomenonResult>();
            }
            
            var context = new WaveInteractionContext(wave, position, direction)
            {
                ObstacleSize = obstacleSize,
                Obstacle = new GameObject() // Placeholder
            };
            context.Parameters["obstaclePositionX"] = obstaclePosition.x;
            context.Parameters["obstaclePositionY"] = obstaclePosition.y;
            context.Parameters["obstaclePositionZ"] = obstaclePosition.z;
            
            // TODO: Implement when WaveDiffraction is created
            // Placeholder: simple diffraction calculation
            // Diffraction creates waves in multiple directions
            List<WavePhenomenonResult> results = new List<WavePhenomenonResult>();
            
            // Calculate diffraction angle based on frequency and obstacle size
            // λ = 1/frequency (simplified), diffraction angle ≈ λ / obstacleSize
            float wavelength = 1f / Mathf.Max(wave.Frequency, 0.1f);
            float diffractionAngle = Mathf.Atan(wavelength / (obstacleSize + 0.1f)) * Mathf.Rad2Deg;
            float energyPerDirection = wave.EnergyStrength * wave.DiffractionFactor / 3f; // Split into 3 directions
            
            // Main direction (slightly bent around obstacle)
            Vector3 perpendicular = Vector3.Cross(direction, (obstaclePosition - position).normalized);
            if (perpendicular.magnitude < 0.1f) perpendicular = Vector3.up;
            perpendicular.Normalize();
            
            Vector3 diffractedDir1 = Quaternion.AngleAxis(diffractionAngle, perpendicular) * direction;
            results.Add(WavePhenomenonResult.Partial(
                wave,
                position,
                diffractedDir1,
                energyPerDirection,
                wave.EnergyStrength - energyPerDirection,
                wave.DiffractionFactor,
                WavePhenomenonType.Diffraction));
            
            // Add opposite direction
            Vector3 diffractedDir2 = Quaternion.AngleAxis(-diffractionAngle, perpendicular) * direction;
            results.Add(WavePhenomenonResult.Partial(
                wave,
                position,
                diffractedDir2,
                energyPerDirection,
                0f,
                wave.DiffractionFactor,
                WavePhenomenonType.Diffraction));
            
            return results;
        }
        
        #endregion
        
        #region Refraction Methods
        
        /// <summary>
        /// Calculate wave refraction at material boundary
        /// </summary>
        /// <param name="wave">Incident wave</param>
        /// <param name="position">Refraction point</param>
        /// <param name="incidentDirection">Incident direction</param>
        /// <param name="surfaceNormal">Surface normal</param>
        /// <param name="material1">First material (incident side)</param>
        /// <param name="material2">Second material (refracted side)</param>
        /// <returns>Refraction result</returns>
        public static WavePhenomenonResult CalculateRefraction(
            Wave wave,
            Vector3 position,
            Vector3 incidentDirection,
            Vector3 surfaceNormal,
            WaveMaterial material1,
            WaveMaterial material2)
        {
            if (wave == null || material1 == null || material2 == null)
            {
                Debug.LogError("WavePhenomenonManager: Cannot calculate refraction with null wave or materials");
                return WavePhenomenonResult.Terminated(position, 0f, WavePhenomenonType.None);
            }
            
            var context = new WaveInteractionContext(wave, position, incidentDirection)
            {
                SurfaceNormal = surfaceNormal.normalized,
                Material = material1
            };
            context.Parameters["material2RefractionIndex"] = material2.refractionIndex;
            
            // TODO: Implement when WaveRefraction is created
            // Placeholder: Snell's Law calculation
            float n1 = material1.refractionIndex;
            float n2 = material2.refractionIndex;
            
            float incidentAngle = Vector3.Angle(-incidentDirection, surfaceNormal);
            float sinRefracted = (n1 / n2) * Mathf.Sin(incidentAngle * Mathf.Deg2Rad);
            
            if (Mathf.Abs(sinRefracted) > 1f)
            {
                // Total internal reflection
                return CalculateReflection(wave, position, incidentDirection, surfaceNormal, material1);
            }
            
            float refractedAngle = Mathf.Asin(sinRefracted) * Mathf.Rad2Deg;
            Vector3 refractedDirection = Quaternion.AngleAxis(refractedAngle - incidentAngle, Vector3.Cross(-incidentDirection, surfaceNormal)) * incidentDirection;
            
            return WavePhenomenonResult.Partial(
                wave,
                position,
                refractedDirection,
                wave.EnergyStrength * 0.9f, // Some energy loss at boundary
                wave.EnergyStrength * 0.1f,
                0.9f,
                WavePhenomenonType.Refraction);
        }
        
        #endregion
        
        #region Combined Phenomena
        
        /// <summary>
        /// Calculate wave interaction with obstacle (may involve reflection, penetration, or diffraction)
        /// Automatically determines which phenomenon occurs based on wave and material properties
        /// </summary>
        public static WavePhenomenonResult CalculateObstacleInteraction(
            Wave wave,
            Vector3 position,
            Vector3 direction,
            Vector3 surfaceNormal,
            WaveMaterial material,
            float obstacleSize = 0f,
            float obstacleThickness = 0f)
        {
            if (wave == null || material == null)
            {
                return WavePhenomenonResult.Terminated(position, 0f, WavePhenomenonType.None);
            }
            
            // Determine which phenomenon occurs based on wave properties and material
            float reflectionProb = material.GetEffectiveReflectionProbability(wave);
            float penetrationProb = material.GetEffectivePenetrationProbability(wave);
            float diffractionProb = wave.DiffractionFactor;
            
            // Random roll to determine outcome
            float roll = Random.Range(0f, 1f);
            
            if (roll < reflectionProb)
            {
                // Reflection occurs
                return CalculateReflection(wave, position, direction, surfaceNormal, material);
            }
            else if (roll < reflectionProb + penetrationProb)
            {
                // Penetration occurs
                return CalculatePenetration(wave, position, direction, material, obstacleThickness);
            }
            else if (obstacleSize > 0f && diffractionProb > 0.5f)
            {
                // Diffraction occurs (if obstacle is small enough)
                var results = CalculateDiffraction(wave, position, direction, obstacleSize, position);
                return results.Count > 0 ? results[0] : WavePhenomenonResult.Terminated(position, wave.EnergyStrength, WavePhenomenonType.Absorption);
            }
            else
            {
                // Absorption occurs
                return WavePhenomenonResult.Terminated(position, wave.EnergyStrength, WavePhenomenonType.Absorption);
            }
        }
        
        #endregion
    }
}

