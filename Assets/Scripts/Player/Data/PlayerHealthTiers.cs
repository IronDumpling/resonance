using Resonance.Core.Data;

namespace Resonance.Player.Data
{
    /// <summary>
    /// 玩家生命值等级
    /// 基于生命值百分比阈值, 影响晶核紊乱值恢复速度
    /// </summary>
    public enum HealthTier
    {
        Healthy,    // 80-100% - 健康状态, 紊乱值恢复 -3/s
        Injured,    // 60-80% - 轻伤状态, 紊乱值恢复 -1/s
        Wounded,    // 30-60% - 重伤状态, 紊乱值恢复 -0.5/s
        Critical    // 0-30% - 濒死状态, 紊乱值恢复 -0.2/s
    }

    /// <summary>
    /// 健康等级辅助类
    /// 提供健康等级相关的计算和配置
    /// </summary>
    public static class HealthTierHelper
    {
        // 健康等级阈值
        public const float HEALTHY_THRESHOLD = 0.8f;     // 80%
        public const float INJURED_THRESHOLD = 0.6f;     // 60%
        public const float WOUNDED_THRESHOLD = 0.3f;     // 30%
        
        // 紊乱值恢复速率(负值表示下降)
        public const float HEALTHY_CHAOS_RECOVERY = -3f;
        public const float INJURED_CHAOS_RECOVERY = -1f;
        public const float WOUNDED_CHAOS_RECOVERY = -0.5f;
        public const float CRITICAL_CHAOS_RECOVERY = -0.2f;

        // 移动速度修正系数
        public const float WOUNDED_SPEED_MULTIPLIER = 0.7f;    // 重伤时移动速度70%
        public const float CRITICAL_SPEED_MULTIPLIER = 0.4f;   // 濒死时移动速度40%

        /// <summary>
        /// 根据生命值百分比计算健康等级
        /// </summary>
        /// <param name="healthPercentage">生命值百分比 (0-1)</param>
        /// <returns>健康等级</returns>
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
        /// 获取指定健康等级的紊乱值恢复速率
        /// </summary>
        /// <param name="tier">健康等级</param>
        /// <returns>紊乱值恢复速率(负值表示下降)</returns>
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
        /// 获取健康等级的描述文本
        /// </summary>
        /// <param name="tier">健康等级</param>
        /// <returns>描述文本</returns>
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
        /// 获取指定健康等级的移动速度修正系数
        /// </summary>
        /// <param name="tier">健康等级</param>
        /// <returns>移动速度修正系数</returns>
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
