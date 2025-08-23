using UnityEngine;

namespace Resonance.Interfaces
{
    /// <summary>
    /// 可受伤害物体的接口
    /// 实现此接口的对象可以接受伤害
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="damageSource">伤害来源位置</param>
        /// <param name="damageType">伤害类型</param>
        void TakeDamage(float damage, Vector3 damageSource, string damageType = "Normal");
        
        /// <summary>
        /// 是否还活着/可受伤
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// 当前生命值
        /// </summary>
        float CurrentHealth { get; }
        
        /// <summary>
        /// 最大生命值
        /// </summary>
        float MaxHealth { get; }
    }
}
