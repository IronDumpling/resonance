using UnityEngine;

namespace Resonance.Utilities.Types
{
    public enum HealthTier
    {
        Healthy,    // 80-100% - 健康状态, 紊乱值恢复 -3/s
        Injured,    // 60-80% - 轻伤状态, 紊乱值恢复 -1/s
        Wounded,    // 30-60% - 重伤状态, 紊乱值恢复 -0.5/s
        Critical    // 0-30% - 濒死状态, 紊乱值恢复 -0.2/s
    }

    /// <summary>
    /// Health tier helper class
    /// Provides calculation and configuration for health tiers
    /// </summary>
    public static class HealthTierHelper
    {
        // Health tier thresholds
        public const float HEALTHY_THRESHOLD = 0.8f;     // 80%
        public const float INJURED_THRESHOLD = 0.6f;     // 60%
        public const float WOUNDED_THRESHOLD = 0.3f;     // 30%
        
        // Chaos recovery rate
        public const float HEALTHY_CHAOS_RECOVERY = -2f;
        public const float INJURED_CHAOS_RECOVERY = -1f;
        public const float WOUNDED_CHAOS_RECOVERY = -0.5f;
        public const float CRITICAL_CHAOS_RECOVERY = -0.2f;

        // Movement speed multiplier
        public const float WOUNDED_SPEED_MULTIPLIER = 0.7f;    // 70% when wounded
        public const float CRITICAL_SPEED_MULTIPLIER = 0.4f;   // 40% when critical

        /// <summary>
        /// Calculate health tier based on health percentage
        /// </summary>
        /// <param name="healthPercentage">Health percentage (0-1)</param>
        /// <returns>Health tier</returns>
        public static HealthTier CalculateHealthTier(float healthPercentage)
        {
            if (healthPercentage >= HEALTHY_THRESHOLD)
                return HealthTier.Healthy;
            else if (healthPercentage >= INJURED_THRESHOLD)
                return HealthTier.Injured;
            else if (healthPercentage >= WOUNDED_THRESHOLD)
                return HealthTier.Wounded;
            else
                return HealthTier.Critical;
        }

        /// <summary>
        /// Get chaos recovery rate for specified health tier
        /// </summary>
        /// <param name="tier">Health tier</param>
        /// <returns>Chaos recovery rate (negative value indicates decrease)</returns>
        public static float GetChaosRecoveryRate(HealthTier tier)
        {
            switch (tier)
            {
                case HealthTier.Healthy:
                    return HEALTHY_CHAOS_RECOVERY;
                case HealthTier.Injured:
                    return INJURED_CHAOS_RECOVERY;
                case HealthTier.Wounded:
                    return WOUNDED_CHAOS_RECOVERY;
                case HealthTier.Critical:
                    return CRITICAL_CHAOS_RECOVERY;
                default:
                    return CRITICAL_CHAOS_RECOVERY;
            }
        }

        /// <summary>
        /// Get description text for specified health tier
        /// </summary>
        /// <param name="tier">Health tier</param>
        /// <returns>Description text</returns>
        public static string GetHealthTierDescription(HealthTier tier)
        {
            switch (tier)
            {
                case HealthTier.Healthy:
                    return "Healthy";
                case HealthTier.Injured:
                    return "Injured";
                case HealthTier.Wounded:
                    return "Wounded";
                case HealthTier.Critical:
                    return "Critical";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Get speed multiplier for specified health tier
        /// </summary>
        /// <param name="tier">Health tier</param>
        /// <returns>Speed multiplier</returns>
        public static float GetSpeedMultiplier(HealthTier tier)
        {
            switch (tier)
            {
                case HealthTier.Healthy:
                    return 1.0f;
                case HealthTier.Injured:
                    return 1.0f;
                case HealthTier.Wounded:
                    return WOUNDED_SPEED_MULTIPLIER;
                case HealthTier.Critical:
                    return CRITICAL_SPEED_MULTIPLIER;
                default:
                    return CRITICAL_SPEED_MULTIPLIER;
            }
        }
    }
}