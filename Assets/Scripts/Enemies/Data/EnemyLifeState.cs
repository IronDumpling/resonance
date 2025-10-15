using Resonance.Utilities;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Enemies.Data
{
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
    public static class EnemyLifeStateHelper
    {
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
