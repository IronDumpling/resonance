using Resonance.Utilities;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Enemies.Data
{
    /// <summary>
    /// 敌人生命值等级
    /// 基于生命值百分比阈值, 影响晶核紊乱值恢复速度和移动能力
    /// </summary>
    public enum EnemyHealthTier
    {
        Healthy,    // 80-100% - 健康状态, 紊乱值恢复 -3/s, 正常移动
        Injured,    // 60-80% - 轻伤状态, 紊乱值恢复 -1/s, 正常移动
        Wounded,    // 30-60% - 重伤状态, 紊乱值恢复 -0.5/s, 移动速度降低
        Critical    // 0-30% - 濒死状态, 紊乱值恢复 -0.2/s, 移动速度大幅降低
    }

    /// <summary>
    /// 敌人生命状态
    /// 基于物理生命值和晶核生命值
    /// </summary>
    public enum EnemyLifeState
    {
        Alive,      // 物理生命值 > 0
        Reviving,   // 物理生命值 = 0, 晶核生命值 > 0, 正在复活
        Dead        // 晶核生命值 = 0, 真正死亡
    }

    /// <summary>
    /// 敌人健康等级辅助类
    /// 提供敌人健康等级相关的计算和配置
    /// </summary>
    public static class EnemyHealthTierHelper
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
        public static EnemyHealthTier CalculateHealthTier(float healthPercentage)
        {
            if (healthPercentage >= HEALTHY_THRESHOLD)
                return EnemyHealthTier.Healthy;
            else if (healthPercentage >= INJURED_THRESHOLD)
                return EnemyHealthTier.Injured;
            else if (healthPercentage >= WOUNDED_THRESHOLD)
                return EnemyHealthTier.Wounded;
            else
                return EnemyHealthTier.Critical;
        }

        /// <summary>
        /// 获取指定健康等级的紊乱值恢复速率
        /// </summary>
        public static float GetChaosRecoveryRate(EnemyHealthTier tier)
        {
            switch (tier)
            {
                case EnemyHealthTier.Healthy:
                    return HEALTHY_CHAOS_RECOVERY;
                case EnemyHealthTier.Injured:
                    return INJURED_CHAOS_RECOVERY;
                case EnemyHealthTier.Wounded:
                    return WOUNDED_CHAOS_RECOVERY;
                case EnemyHealthTier.Critical:
                    return CRITICAL_CHAOS_RECOVERY;
                default:
                    return CRITICAL_CHAOS_RECOVERY;
            }
        }

        /// <summary>
        /// 获取指定健康等级的移动速度修正系数
        /// </summary>
        public static float GetSpeedMultiplier(EnemyHealthTier tier)
        {
            switch (tier)
            {
                case EnemyHealthTier.Healthy:
                case EnemyHealthTier.Injured:
                    return 1.0f;
                case EnemyHealthTier.Wounded:
                    return WOUNDED_SPEED_MULTIPLIER;
                case EnemyHealthTier.Critical:
                    return CRITICAL_SPEED_MULTIPLIER;
                default:
                    return CRITICAL_SPEED_MULTIPLIER;
            }
        }

        /// <summary>
        /// 计算生命状态
        /// </summary>
        public static EnemyLifeState CalculateLifeState(float currentHealth, CoreHealthState coreHealthState)
        {
            // 晶核死亡 -> 敌人死亡
            if (coreHealthState == CoreHealthState.Destroyed)
                return EnemyLifeState.Dead;
            // 物理生命值 > 0 -> 存活
            else if (currentHealth > 0f)
                return EnemyLifeState.Alive;
            // 物理生命值 = 0, 但晶核存活 -> 复活中
            else
                return EnemyLifeState.Reviving;
        }

        /// <summary>
        /// 获取健康等级的描述文本
        /// </summary>
        public static string GetHealthTierDescription(EnemyHealthTier tier)
        {
            switch (tier)
            {
                case EnemyHealthTier.Healthy:
                    return "Healthy";
                case EnemyHealthTier.Injured:
                    return "Injured";
                case EnemyHealthTier.Wounded:
                    return "Wounded";
                case EnemyHealthTier.Critical:
                    return "Critical";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// 获取生命状态的描述文本
        /// </summary>
        public static string GetLifeStateDescription(EnemyLifeState state)
        {
            switch (state)
            {
                case EnemyLifeState.Alive:
                    return "Alive";
                case EnemyLifeState.Reviving:
                    return "Reviving";
                case EnemyLifeState.Dead:
                    return "Dead";
                default:
                    return "Unknown";
            }
        }
    }
}
