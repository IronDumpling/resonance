using UnityEngine;

namespace Resonance.Interfaces
{
    /// <summary>
    /// 可受伤害物体的接口
    /// 实现此接口的对象可以接受伤害，支持双血量系统
    /// </summary>
    public interface IDamageable
    {
        #region Damage Methods
        
        /// <summary>
        /// 受到伤害 (支持双血量系统)
        /// </summary>
        /// <param name="damageInfo">伤害信息</param>
        void TakeDamage(DamageInfo damageInfo);
        
        /// <summary>
        /// 受到物理伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="damageSource">伤害来源位置</param>
        void TakePhysicalDamage(float damage, Vector3 damageSource);
        
        /// <summary>
        /// 受到精神伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="damageSource">伤害来源位置</param>
        void TakeMentalDamage(float damage, Vector3 damageSource);
        
        #endregion
        
        #region Health Properties
        
        /// <summary>
        /// 是否物理存活 (物理血量 > 0)
        /// </summary>
        bool IsPhysicallyAlive { get; }
        
        /// <summary>
        /// 是否精神存活 (精神血量 > 0)
        /// </summary>
        bool IsMentallyAlive { get; }
        
        /// <summary>
        /// 是否处于物理死亡状态 (物理血量 = 0 但精神血量 > 0)
        /// </summary>
        bool IsInPhysicalDeathState { get; }
        
        /// <summary>
        /// 当前物理血量
        /// </summary>
        float CurrentPhysicalHealth { get; }
        
        /// <summary>
        /// 最大物理血量
        /// </summary>
        float MaxPhysicalHealth { get; }
        
        /// <summary>
        /// 当前精神血量
        /// </summary>
        float CurrentMentalHealth { get; }
        
        /// <summary>
        /// 最大精神血量
        /// </summary>
        float MaxMentalHealth { get; }
        
        #endregion
    }
}
