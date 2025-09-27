using Resonance.Core.Data;

namespace Resonance.Enemies.Data
{
    /// <summary>
    /// 敌人生命值等级
    /// 基于生命值百分比阈值，影响韧性恢复速度和移动能力
    /// 与玩家使用相同的等级系统
    /// </summary>
    public enum EnemyHealthTier
    {
        Healthy,    // 80-100% - 健康状态，韧性恢复+10，正常移动
        Injured,    // 60-80% - 轻伤状态，韧性恢复+8，正常移动
        Wounded,    // 30-60% - 重伤状态，韧性恢复+3，移动速度降低
        Critical    // 0-30% - 濒死状态，韧性恢复+2，移动速度大幅降低
    }

    /// <summary>
    /// 敌人韧性状态
    /// 基于当前韧性值
    /// </summary>
    public enum EnemyResilienceState
    {
        Normal,     // 韧性值 > 眩晕阈值
        Stunned     // 韧性值 <= 眩晕阈值（倒地状态）
    }

    /// <summary>
    /// 敌人生命状态
    /// 基于生命值和晶核完整度
    /// </summary>
    public enum EnemyLifeState
    {
        Alive,      // 生命值 > 0
        Reviving,   // 生命值 = 0，晶核完整度 > 0，正在复活
        Dead        // 生命值 = 0，晶核完整度 = 0，真正死亡
    }

    /// <summary>
    /// 敌人健康等级辅助类
    /// 提供敌人健康等级相关的计算和配置
    /// </summary>
    public static class EnemyHealthTierHelper
    {
        // 健康等级阈值（与玩家相同）
        public const float HEALTHY_THRESHOLD = 0.8f;     // 80%
        public const float INJURED_THRESHOLD = 0.6f;     // 60%
        public const float WOUNDED_THRESHOLD = 0.3f;     // 30%
        
        // 韧性恢复速率（与玩家相同）
        public const float HEALTHY_RESILIENCE_REGEN = 10f;
        public const float INJURED_RESILIENCE_REGEN = 8f;
        public const float WOUNDED_RESILIENCE_REGEN = 3f;
        public const float CRITICAL_RESILIENCE_REGEN = 2f;

        // 移动速度修正系数
        public const float WOUNDED_SPEED_MULTIPLIER = 0.7f;    // 重伤时移动速度70%
        public const float CRITICAL_SPEED_MULTIPLIER = 0.4f;   // 濒死时移动速度40%

        /// <summary>
        /// 根据生命值百分比计算健康等级
        /// </summary>
        /// <param name="healthPercentage">生命值百分比 (0-1)</param>
        /// <returns>健康等级</returns>
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
        /// 获取指定健康等级的韧性恢复速率
        /// </summary>
        /// <param name="tier">健康等级</param>
        /// <returns>韧性恢复速率</returns>
        public static float GetResilienceRegenRate(EnemyHealthTier tier)
        {
            switch (tier)
            {
                case EnemyHealthTier.Healthy:
                    return HEALTHY_RESILIENCE_REGEN;
                case EnemyHealthTier.Injured:
                    return INJURED_RESILIENCE_REGEN;
                case EnemyHealthTier.Wounded:
                    return WOUNDED_RESILIENCE_REGEN;
                case EnemyHealthTier.Critical:
                    return CRITICAL_RESILIENCE_REGEN;
                default:
                    return CRITICAL_RESILIENCE_REGEN;
            }
        }

        /// <summary>
        /// 获取指定健康等级的移动速度修正系数
        /// </summary>
        /// <param name="tier">健康等级</param>
        /// <returns>移动速度修正系数</returns>
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
        /// 计算韧性状态
        /// </summary>
        /// <param name="currentResilience">当前韧性值</param>
        /// <param name="stunThreshold">眩晕阈值</param>
        /// <returns>韧性状态</returns>
        public static EnemyResilienceState CalculateResilienceState(float currentResilience, float stunThreshold)
        {
            return currentResilience > stunThreshold ? EnemyResilienceState.Normal : EnemyResilienceState.Stunned;
        }

        /// <summary>
        /// 计算生命状态
        /// </summary>
        /// <param name="currentHealth">当前生命值</param>
        /// <param name="coreIntegrity">晶核完整度 (0-1)</param>
        /// <returns>生命状态</returns>
        public static EnemyLifeState CalculateLifeState(float currentHealth, float coreIntegrity)
        {
            if (currentHealth > 0f)
                return EnemyLifeState.Alive;
            else if (coreIntegrity > 0f)
                return EnemyLifeState.Reviving;
            else
                return EnemyLifeState.Dead;
        }

        /// <summary>
        /// 获取健康等级的描述文本
        /// </summary>
        /// <param name="tier">健康等级</param>
        /// <returns>描述文本</returns>
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
        /// <param name="state">生命状态</param>
        /// <returns>描述文本</returns>
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
