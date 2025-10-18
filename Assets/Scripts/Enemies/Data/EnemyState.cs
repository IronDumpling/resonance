using Resonance.Utilities;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Enemies.Data
{
    /// <summary>
    /// 敌人状态枚举 - 统一的状态管理
    /// 基于物理生命值、晶核生命值和战斗状态
    /// </summary>
    public enum EnemyState
    {
        Normal,      // Alive and active
        Stunned,     // Temporarily incapacitated by chaos damage
        Reviving,    // Physical health depleted, restoring (core alive)
        Dead         // Core destroyed, permanently dead
    }

    /// <summary>
    /// 敌人状态辅助类
    /// 提供敌人状态相关的计算和配置
    /// </summary>
    public static class EnemyStateHelper
    {
        /// <summary>
        /// 计算敌人状态（不包括 Stunned，Stunned 由 Controller 单独管理）
        /// </summary>
        public static EnemyState CalculateLifeState(float currentHealth, CoreHealthState coreHealthState)
        {
            // 晶核死亡 -> 敌人死亡
            if (coreHealthState == CoreHealthState.Destroyed)
                return EnemyState.Dead;
            // 物理生命值 > 0 -> 正常存活
            else if (currentHealth > 0f)
                return EnemyState.Normal;
            // 物理生命值 = 0, 但晶核存活 -> 复活中
            else
                return EnemyState.Reviving;
        }

        /// <summary>
        /// 获取状态的描述文本
        /// </summary>
        public static string GetStateDescription(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Normal:
                    return "Normal";
                case EnemyState.Stunned:
                    return "Stunned";
                case EnemyState.Reviving:
                    return "Reviving";
                case EnemyState.Dead:
                    return "Dead";
                default:
                    return "Unknown";
            }
        }
    }
}
