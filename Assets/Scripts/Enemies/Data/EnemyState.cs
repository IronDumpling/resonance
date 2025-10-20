using UnityEngine;

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
    /// 敌人状态数据类 - 集中存储所有状态信息
    /// 由 EnemyController 负责每帧更新
    /// </summary>
    public class EnemyStateData
    {
        // 原始数据缓存（从 EnemyRuntimeStats 同步）
        private float _currentHealth;
        private float _currentCoreHealth;
        private bool _isStunned;
        
        /// <summary>
        /// 当前逻辑状态（Normal/Stunned/Reviving/Dead）
        /// 使用 enum 而非多个 bool，避免状态混乱
        /// </summary>
        public EnemyState CurrentState { get; private set; }
        
        /// <summary>
        /// 生命值相关状态 - 三个互斥的 bool
        /// </summary>
        
        // 物理生命和晶核生命都存在
        public bool IsPhysicallyAlive => _currentHealth > 0f && _currentCoreHealth > 0f;
        
        // 物理生命耗尽，但晶核生命存在（可复活）
        public bool IsPhysicallyDead => _currentHealth <= 0f && _currentCoreHealth > 0f;
        
        // 晶核生命耗尽（真正死亡）
        public bool IsCoreDead => _currentCoreHealth <= 0f;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public EnemyStateData()
        {
            _currentHealth = 0f;
            _currentCoreHealth = 0f;
            _isStunned = false;
            CurrentState = EnemyState.Dead;
        }
        
        /// <summary>
        /// 更新状态数据（由 EnemyController 每帧调用）
        /// </summary>
        /// <param name="health">当前物理生命值</param>
        /// <param name="coreHealth">当前晶核生命值</param>
        /// <param name="isStunned">是否处于眩晕状态</param>
        public void UpdateState(float health, float coreHealth, bool isStunned)
        {
            // 缓存原始数据
            _currentHealth = health;
            _currentCoreHealth = coreHealth;
            _isStunned = isStunned;
            
            // 计算当前状态（优先级：Dead > Stunned > Reviving > Normal）
            EnemyState newState;
            
            if (IsCoreDead)
            {
                newState = EnemyState.Dead;
            }
            else if (_isStunned)
            {
                newState = EnemyState.Stunned;
            }
            else if (IsPhysicallyDead)
            {
                newState = EnemyState.Reviving;
            }
            else
            {
                newState = EnemyState.Normal;
            }
            
            CurrentState = newState;
        }
        
        /// <summary>
        /// 获取当前健康信息（用于调试）
        /// </summary>
        public string GetHealthInfo()
        {
            return $"Health: {_currentHealth:F1}, CoreHealth: {_currentCoreHealth:F1}, " +
                   $"State: {CurrentState}, IsPhysicallyAlive: {IsPhysicallyAlive}, " +
                   $"IsPhysicallyDead: {IsPhysicallyDead}, IsCoreDead: {IsCoreDead}";
        }
    }

    /// <summary>
    /// 敌人状态辅助类
    /// 提供敌人状态相关的描述和配置
    /// </summary>
    public static class EnemyStateHelper
    {
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
