using UnityEngine;

namespace Resonance.Shared.Types
{
    /// <summary>
    /// Balance tier enum - represents enemy's balance state (stance/posture)
    /// Similar to Sekiro's posture system
    /// </summary>
    public enum BalanceTier
    {
        Balanced,    // 80-100% - Fully balanced, normal behavior
        Shaken,      // 60-80% - Slightly shaken, normal speed
        Unstable,    // 30-60% - Unstable, reduced speed
        Teetering    // 0-30% - About to lose balance, heavily reduced speed
    }

    /// <summary>
    /// Balance tier helper class
    /// Provides calculation and configuration for balance tiers
    /// </summary>
    public static class BalanceTierHelper
    {
        // Balance tier thresholds
        public const float BALANCED_THRESHOLD = 0.8f;     // 80%
        public const float SHAKEN_THRESHOLD = 0.6f;       // 60%
        public const float UNSTABLE_THRESHOLD = 0.3f;     // 30%
        
        // Movement speed multipliers
        public const float BALANCED_SPEED_MULTIPLIER = 1.0f;     // 100% when balanced
        public const float SHAKEN_SPEED_MULTIPLIER = 1.0f;       // 100% when shaken
        public const float UNSTABLE_SPEED_MULTIPLIER = 0.7f;     // 70% when unstable
        public const float TEETERING_SPEED_MULTIPLIER = 0.5f;    // 50% when teetering

        /// <summary>
        /// Calculate balance tier based on balance percentage
        /// </summary>
        /// <param name="balancePercentage">Balance percentage (0-1)</param>
        /// <returns>Balance tier</returns>
        public static BalanceTier CalculateBalanceTier(float balancePercentage)
        {
            if (balancePercentage >= BALANCED_THRESHOLD)
                return BalanceTier.Balanced;
            else if (balancePercentage >= SHAKEN_THRESHOLD)
                return BalanceTier.Shaken;
            else if (balancePercentage >= UNSTABLE_THRESHOLD)
                return BalanceTier.Unstable;
            else
                return BalanceTier.Teetering;
        }

        /// <summary>
        /// Get description text for specified balance tier
        /// </summary>
        /// <param name="tier">Balance tier</param>
        /// <returns>Description text</returns>
        public static string GetBalanceTierDescription(BalanceTier tier)
        {
            switch (tier)
            {
                case BalanceTier.Balanced:
                    return "Balanced";
                case BalanceTier.Shaken:
                    return "Shaken";
                case BalanceTier.Unstable:
                    return "Unstable";
                case BalanceTier.Teetering:
                    return "Teetering";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Get speed multiplier for specified balance tier
        /// </summary>
        /// <param name="tier">Balance tier</param>
        /// <returns>Speed multiplier</returns>
        public static float GetSpeedMultiplier(BalanceTier tier)
        {
            switch (tier)
            {
                case BalanceTier.Balanced:
                    return BALANCED_SPEED_MULTIPLIER;
                case BalanceTier.Shaken:
                    return SHAKEN_SPEED_MULTIPLIER;
                case BalanceTier.Unstable:
                    return UNSTABLE_SPEED_MULTIPLIER;
                case BalanceTier.Teetering:
                    return TEETERING_SPEED_MULTIPLIER;
                default:
                    return TEETERING_SPEED_MULTIPLIER;
            }
        }
    }
}

