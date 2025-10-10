using UnityEngine;
using Resonance.Utilities;

namespace Resonance.Interfaces
{
    /// <summary>
    /// Interface for objects that can be damaged
    /// Implementing this interface allows objects to take damage
    /// </summary>
    public interface IDamageable
    {
        #region Damage Methods
        
        /// <summary>
        /// Take damage (supports health, resilience and core)
        /// </summary>
        /// <param name="damageInfo">Damage information</param>
        void TakeDamage(DamageInfo damageInfo);
        
        /// <summary>
        /// 受到物理伤害
        /// </summary>
        /// <param name="damage">Damage value</param>
        /// <param name="damageSource">Damage source position</param>
        void TakeHealthDamage(float damage, Vector3 damageSource);
        
        /// <summary>
        /// 受到精神伤害
        /// </summary>
        /// <param name="damage">Damage value</param>
        /// <param name="damageSource">Damage source position</param>
        void TakeCoreDamage(float damage, Vector3 damageSource);

        /// <summary>
        /// Take resilience damage
        /// </summary>
        /// <param name="damage">Damage value</param>
        /// <param name="damageSource">Damage source position</param>
        void TakeResilienceDamage(float damage, Vector3 damageSource);
        
        #endregion
        
        #region Health Properties
        
        /// <summary>
        /// Is alive (health > 0)
        /// TODO: refactor to IsHealthAlive
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// Is core alive (core capacity > 0)
        /// </summary>
        bool IsCoreAlive { get; }
        
        /// <summary>
        /// Is in death state (health = 0 but core capacity > 0)
        /// TODO: refactor to OnlyHealthDeath, and add OnlyCoreDeath
        /// </summary>
        bool IsInDeathState { get; }
        
        /// <summary>
        /// Current health
        /// </summary>
        float CurrentHealth { get; }
        
        /// <summary>
        /// Max health
        /// </summary>
        float MaxHealth { get; }
        
        /// <summary>
        /// Current core health
        /// </summary>
        float CurrentCoreCapacity { get; }
        
        /// <summary>
        /// Max core health
        /// </summary>
        float MaxCoreCapacity { get; }
        
        #endregion
    }
}
