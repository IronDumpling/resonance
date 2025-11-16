using UnityEngine;
using Resonance.Utilities.Waves;
using Resonance.Utilities.Types;

namespace Resonance.Utilities.Waves.WavePhenomenons
{
    /// <summary>
    /// Wave propagation modes
    /// </summary>
    public enum WavePropagationMode
    {
        Spherical,      // Spherical expansion - 360° detection/sonar
        Directional     // Directional beam - from laser to shotgun (controlled by DiffractionFactor)
    }
    
    /// <summary>
    /// Wave Propagation - calculates energy distribution as wave travels
    /// - Spherical: 360° detection/sonar with 1/r² decay
    /// - Directional: Focused beam with spread controlled by wave's DiffractionFactor
    ///   (high diffraction = shotgun-like spread, low diffraction = laser-like focus)
    /// </summary>
    public class WavePropagation : IWavePhenomenon
    {
        #region IWavePhenomenon Implementation
        
        public WavePhenomenonType PhenomenonType => WavePhenomenonType.Propagation;
        
        public bool CanApply(WaveInteractionContext context)
        {
            return context != null && context.Wave != null;
        }
        
        public WavePhenomenonResult Calculate(WaveInteractionContext context)
        {
            if (!CanApply(context))
            {
                Debug.LogError("WavePropagation: Cannot apply - invalid context");
                return WavePhenomenonResult.Terminated(
                    context?.Position ?? Vector3.zero,
                    0f,
                    WavePhenomenonType.None);
            }
            
            // Get propagation mode from parameters
            WavePropagationMode mode = WavePropagationMode.Directional;
            if (context.Parameters != null && context.Parameters.ContainsKey("propagationMode"))
            {
                mode = (WavePropagationMode)(int)context.Parameters["propagationMode"];
            }
            
            // Get propagation distance from parameters
            float deltaDistance = 0f;
            if (context.Parameters != null && context.Parameters.ContainsKey("propagationDistance"))
            {
                deltaDistance = context.Parameters["propagationDistance"];
            }
            
            // Calculate propagation step
            return CalculatePropagationStep(
                context.Wave,
                context.Position,
                context.Direction,
                context.DistanceTraveled,
                mode,
                deltaDistance);
        }
        
        #endregion
        
        #region Public Static Methods
        
        /// <summary>
        /// Calculate energy at a specific point in space
        /// </summary>
        /// <param name="wave">The wave being propagated</param>
        /// <param name="origin">Wave origin point</param>
        /// <param name="targetPoint">Point to calculate energy at</param>
        /// <param name="mode">Propagation mode</param>
        /// <param name="propagationDirection">Initial propagation direction (for directional mode)</param>
        /// <returns>Energy value at target point</returns>
        public static float CalculateEnergyAtPoint(
            Wave wave,
            Vector3 origin,
            Vector3 targetPoint,
            WavePropagationMode mode,
            Vector3 propagationDirection = default)
        {
            if (wave == null)
            {
                Debug.LogError("WavePropagation: Wave is null");
                return 0f;
            }
            
            float distance = Vector3.Distance(origin, targetPoint);
            
            switch (mode)
            {
                case WavePropagationMode.Spherical:
                    return CalculateSphericalEnergy(wave, distance);
                    
                case WavePropagationMode.Directional:
                    if (propagationDirection == Vector3.zero)
                    {
                        propagationDirection = (targetPoint - origin).normalized;
                    }
                    return CalculateDirectionalEnergy(wave, distance, origin, targetPoint, propagationDirection);
                    
                default:
                    return wave.GetEnergyAtDistance(distance);
            }
        }
        
        /// <summary>
        /// Calculate a single propagation step (for iterative simulation)
        /// Returns updated wave state after traveling deltaDistance
        /// </summary>
        public static WavePhenomenonResult CalculatePropagationStep(
            Wave wave,
            Vector3 currentPosition,
            Vector3 direction,
            float traveledDistance,
            WavePropagationMode mode,
            float deltaDistance = 0f)
        {
            if (wave == null)
            {
                Debug.LogError("WavePropagation: Wave is null");
                return WavePhenomenonResult.Terminated(currentPosition, 0f, WavePhenomenonType.None);
            }
            
            // Calculate new position
            Vector3 newPosition = currentPosition + direction.normalized * deltaDistance;
            float newDistance = traveledDistance + deltaDistance;
            
            // Calculate energy at new position
            // Find original origin by backtracking
            Vector3 originalOrigin = currentPosition - direction.normalized * traveledDistance;
            float remainingEnergy = CalculateEnergyAtPoint(
                wave, 
                originalOrigin,
                newPosition,
                mode,
                direction);
            
            // Calculate energy lost
            float initialEnergy = wave.EnergyStrength;
            float energyLost = initialEnergy - remainingEnergy;
            
            // Check if wave should terminate
            float effectiveRange = wave.GetEffectiveRange();
            bool shouldTerminate = remainingEnergy < initialEnergy * 0.01f || // Less than 1% energy
                                   newDistance >= effectiveRange;
            
            // Create result wave (same properties, just different position)
            Wave propagatedWave = wave; // In real implementation, might want to create a copy
            
            // Determine result type
            if (shouldTerminate)
            {
                return WavePhenomenonResult.Terminated(
                    newPosition,
                    remainingEnergy,
                    WavePhenomenonType.Propagation);
            }
            else
            {
                var result = WavePhenomenonResult.Partial(
                    propagatedWave,
                    newPosition,
                    direction,
                    remainingEnergy,
                    energyLost,
                    remainingEnergy / initialEnergy, // Intensity factor
                    WavePhenomenonType.Propagation);
                
                // Add propagation-specific parameters
                result.Parameters["traveledDistance"] = newDistance;
                result.Parameters["propagationMode"] = (float)mode;
                result.Parameters["effectiveRange"] = effectiveRange;
                result.Parameters["remainingRangePercent"] = 1f - (newDistance / effectiveRange);
                result.Parameters["beamSpread"] = GetBeamSpreadAngle(wave, newDistance);
                
                return result;
            }
        }
        
        /// <summary>
        /// Get beam spread angle at a given distance
        /// Used for visualizing cone/shotgun spread
        /// </summary>
        public static float GetBeamSpreadAngle(Wave wave, float distance)
        {
            if (wave == null) return 0f;
            
            // Base spread is controlled by DiffractionFactor
            // High diffraction (0.8-1.0) = wide spread (shotgun)
            // Low diffraction (0.1-0.3) = narrow spread (laser)
            float baseSpread = Mathf.Lerp(2f, 45f, wave.DiffractionFactor); // 2-45 degrees
            
            // Spread increases slightly with distance (natural divergence)
            // But high-frequency (low diffraction) waves resist divergence
            float divergenceRate = Mathf.Lerp(0.02f, 0.1f, wave.DiffractionFactor); // 2-10% per unit
            float distanceSpread = baseSpread * (1f + distance * divergenceRate);
            
            // Clamp to reasonable range
            return Mathf.Clamp(distanceSpread, 1f, 90f);
        }
        
        /// <summary>
        /// Check if a point is within the affected area
        /// Useful for spherical mode to find all targets in range
        /// </summary>
        public static bool IsPointAffected(
            Wave wave,
            Vector3 origin,
            Vector3 point,
            WavePropagationMode mode,
            Vector3 direction,
            float energyThreshold = 1f)
        {
            float energyAtPoint = CalculateEnergyAtPoint(wave, origin, point, mode, direction);
            return energyAtPoint >= energyThreshold;
        }
        
        #endregion
        
        #region Private Calculation Methods
        
        /// <summary>
        /// Spherical propagation: energy decays as 1/r² (geometric) + exponential (absorption)
        /// Used for detection/sonar - 360° coverage, fast decay
        /// </summary>
        private static float CalculateSphericalEnergy(Wave wave, float distance)
        {
            if (distance < 0.01f) return wave.EnergyStrength;
            
            // Geometric decay: 1/(1 + r²)
            // Using (1 + r²) instead of r² to avoid singularity at r=0
            // and to make energy decay more graceful for gameplay
            float geometricDecay = 1.0f / (1.0f + distance * distance);
            
            // Absorption decay: e^(-α×r) from Wave.cs
            float absorptionDecay = Mathf.Exp(-wave.EnergyAttenuationFactor * distance);
            
            // Combined decay
            float finalEnergy = wave.EnergyStrength * geometricDecay * absorptionDecay;
            
            // Apply diffraction modifier: high diffraction = wave travels farther in all directions
            // Simulates wave bending around obstacles
            float diffractionBonus = Mathf.Lerp(0.8f, 1.2f, wave.DiffractionFactor);
            finalEnergy *= diffractionBonus;
            
            return Mathf.Max(0f, finalEnergy);
        }
        
        /// <summary>
        /// Directional propagation: energy travels in a cone/beam
        /// Spread controlled by wave's DiffractionFactor:
        /// - Low diffraction (high freq): Laser-like narrow beam
        /// - High diffraction (low freq): Shotgun-like wide cone
        /// </summary>
        private static float CalculateDirectionalEnergy(
            Wave wave,
            float distance,
            Vector3 origin,
            Vector3 targetPoint,
            Vector3 shootDirection)
        {
            // 1. Linear decay along propagation axis (use Wave.cs method)
            float axialEnergy = wave.GetEnergyAtDistance(distance);
            
            // 2. Calculate angular offset from beam center
            Vector3 toTarget = (targetPoint - origin).normalized;
            float angleOffset = Vector3.Angle(shootDirection.normalized, toTarget);
            
            // 3. Calculate beam spread based on DiffractionFactor
            // This is the KEY: DiffractionFactor controls beam width
            // High DiffractionFactor (low freq wave) = Wide beam (shotgun)
            // Low DiffractionFactor (high freq wave) = Narrow beam (laser)
            float beamSpread = GetBeamSpreadAngle(wave, distance);
            
            // 4. Energy distribution within cone using Gaussian-like falloff
            // Points near center get full energy, edges taper off smoothly
            float normalizedAngle = angleOffset / beamSpread;
            
            // Gaussian falloff: e^(-(x²/2σ²))
            // σ controls how "soft" the edge is
            float sigma = 0.5f; // Adjust for harder/softer edges
            float angleFactor = Mathf.Exp(-(normalizedAngle * normalizedAngle) / (2f * sigma * sigma));
            
            // 5. Apply penetration factor: high penetration = energy concentrated at center
            // Low penetration = energy spreads more evenly
            float penetrationModifier = Mathf.Lerp(0.7f, 1.0f, wave.PenetrationFactor);
            angleFactor = Mathf.Pow(angleFactor, 1.0f / penetrationModifier);
            
            // 6. Combine axial decay with angular distribution
            float finalEnergy = axialEnergy * angleFactor;
            
            return Mathf.Max(0f, finalEnergy);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get recommended propagation mode based on wave properties
        /// Helper for AI/automatic mode selection
        /// </summary>
        public static WavePropagationMode GetRecommendedMode(Wave wave)
        {
            if (wave == null) return WavePropagationMode.Directional;
            
            // High energy = Directional (attack)
            if (wave.EnergyStrength > 50f)
            {
                return WavePropagationMode.Directional;
            }
            
            // Low energy + high diffraction = Spherical (detection)
            if (wave.EnergyStrength < 30f && wave.DiffractionFactor > 0.7f)
            {
                return WavePropagationMode.Spherical;
            }
            
            // Default to directional
            return WavePropagationMode.Directional;
        }
        
        /// <summary>
        /// Estimate maximum affected radius for a wave
        /// Useful for optimization (don't check beyond this radius)
        /// </summary>
        public static float EstimateMaxRadius(Wave wave, WavePropagationMode mode)
        {
            float baseRange = wave.GetEffectiveRange();
            
            switch (mode)
            {
                case WavePropagationMode.Spherical:
                    // Spherical decays faster, but covers all directions
                    return baseRange * 0.5f;
                    
                case WavePropagationMode.Directional:
                    // For directional, max radius is range × max spread angle
                    float maxSpread = GetBeamSpreadAngle(wave, baseRange);
                    float maxSpreadRadians = maxSpread * Mathf.Deg2Rad;
                    return baseRange * Mathf.Tan(maxSpreadRadians);
                    
                default:
                    return baseRange;
            }
        }
        
        /// <summary>
        /// Get visual representation points for beam (for rendering)
        /// Returns array of points that form the beam cone outline
        /// </summary>
        public static Vector3[] GetBeamOutlinePoints(
            Wave wave,
            Vector3 origin,
            Vector3 direction,
            float distance,
            int segments = 16)
        {
            float spreadAngle = GetBeamSpreadAngle(wave, distance);
            Vector3[] points = new Vector3[segments + 1];
            
            Vector3 endPoint = origin + direction.normalized * distance;
            float endRadius = distance * Mathf.Tan(spreadAngle * Mathf.Deg2Rad);
            
            // Create perpendicular vectors for circular cross-section
            Vector3 perpendicular = Vector3.Cross(direction.normalized, Vector3.up);
            if (perpendicular.sqrMagnitude < 0.01f)
            {
                perpendicular = Vector3.Cross(direction.normalized, Vector3.right);
            }
            perpendicular.Normalize();
            
            Vector3 perpendicular2 = Vector3.Cross(direction.normalized, perpendicular);
            
            // Generate circle points at end of beam
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 offset = (Mathf.Cos(angle) * perpendicular + Mathf.Sin(angle) * perpendicular2) * endRadius;
                points[i] = endPoint + offset;
            }
            
            return points;
        }
        
        #endregion
    }
}

