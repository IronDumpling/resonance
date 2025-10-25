using UnityEngine;
using Resonance.Utilities.Types;

namespace Resonance.Interfaces
{
    /// <summary>
    /// Interface for objects that can be damaged
    /// Simplified interface using only DamageInfo structure
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Take damage using the new damage system
        /// Supports multiple damage types (Physical Health, Core Health, Chaos) in a single attack
        /// </summary>
        /// <param name="damageInfo">Damage information containing damage types and amounts</param>
        void TakeDamage(DamageInfo damageInfo);
        
        #region State Properties
        
        /// <summary>
        /// Physical health state
        /// </summary>
        PhysicalHealthState PhysicalState { get; }
        
        /// <summary>
        /// Core health state
        /// </summary>
        CoreHealthState CoreState { get; }
        
        /// <summary>
        /// Wave chaos state
        /// </summary>
        WaveChaosState ChaosState { get; }
        
        #endregion
        
        #region Health Values
        
        /// <summary>
        /// Current physical health
        /// </summary>
        float CurrentPhysicalHealth { get; }
        
        /// <summary>
        /// Max physical health
        /// </summary>
        float MaxPhysicalHealth { get; }
        
        /// <summary>
        /// Current core health
        /// </summary>
        float CurrentCoreHealth { get; }
        
        /// <summary>
        /// Max core health
        /// </summary>
        float MaxCoreHealth { get; }
        
        /// <summary>
        /// Current chaos value
        /// </summary>
        float CurrentChaos { get; }
        
        /// <summary>
        /// Max chaos value
        /// </summary>
        float MaxChaos { get; }
        
        #endregion
    }
}
