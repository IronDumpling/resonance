using UnityEngine;
using Resonance.Shared.Types;

namespace Resonance.Shared.Interfaces
{
    /// <summary>
    /// Core interface for objects that can take damage
    /// Only defines the damage-taking method - specific health/balance properties are in separate interfaces
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Take damage using the new damage system
        /// Supports multiple damage types (Physical Health, Core Health, Balance) in a single attack
        /// </summary>
        /// <param name="damageInfo">Damage information containing damage types and amounts</param>
        void TakeDamage(DamageInfo damageInfo);
    }
    
    /// <summary>
    /// Interface for objects that have physical health (Players)
    /// </summary>
    public interface IHasPhysicalHealth
    {
        /// <summary>
        /// Physical health state
        /// </summary>
        PhysicalHealthState PhysicalState { get; }
        
        /// <summary>
        /// Current physical health value
        /// </summary>
        float CurrentPhysicalHealth { get; }
        
        /// <summary>
        /// Maximum physical health value
        /// </summary>
        float MaxPhysicalHealth { get; }
    }
    
    /// <summary>
    /// Interface for objects that have core health (Players and Enemies)
    /// </summary>
    public interface IHasCoreHealth
    {
        /// <summary>
        /// Core health state
        /// </summary>
        CoreHealthState CoreState { get; }
        
        /// <summary>
        /// Current core health value
        /// </summary>
        float CurrentCoreHealth { get; }
        
        /// <summary>
        /// Maximum core health value
        /// </summary>
        float MaxCoreHealth { get; }
    }
    
    /// <summary>
    /// Interface for objects that have balance/stance system (Enemies only)
    /// Similar to Sekiro's posture system - depletes on hit, recovers over time
    /// </summary>
    public interface IHasBalance
    {
        /// <summary>
        /// Current balance value (0 = unbalanced/vulnerable)
        /// </summary>
        float CurrentBalance { get; }
        
        /// <summary>
        /// Maximum balance value
        /// </summary>
        float MaxBalance { get; }
    }
}
