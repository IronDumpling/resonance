using UnityEngine;

namespace Resonance.Interfaces
{
    /// <summary>
    /// 可破坏物体的接口
    /// 用于环境中的可破坏对象，如箱子、墙壁等
    /// </summary>
    public interface IDestructible
    {
        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="damageSource">伤害来源位置</param>
        void TakeDamage(float damage, Vector3 damageSource);
        
        /// <summary>
        /// 是否已被摧毁
        /// </summary>
        bool IsDestroyed { get; }
        
        /// <summary>
        /// 当前耐久度
        /// </summary>
        float CurrentDurability { get; }
        
        /// <summary>
        /// 最大耐久度
        /// </summary>
        float MaxDurability { get; }
        
        /// <summary>
        /// 摧毁时调用
        /// </summary>
        void OnDestroyed();
    }
}
