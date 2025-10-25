using UnityEngine;

namespace Resonance.Utilities
{
    /// <summary>
    /// Physical health state enumeration
    /// </summary>
    public enum PhysicalHealthState
    {
        Alive,      // Physical health > 0
        Dead        // Physical health = 0
    }
    
    /// <summary>
    /// Core health state enumeration
    /// </summary>
    public enum CoreHealthState
    {
        Intact,     // Core health > 0
        Destroyed   // Core health = 0
    }

    /// <summary>
    /// Crystal energy tier
    /// Based on energy/maximum energy percentage
    /// </summary>
    public enum CrystalEnergyTier
    {
        Abundant,   // > 80% - Abundant
        Normal,     // > 30%, ≤ 80% - Normal  
        Low         // > 0%, ≤ 30% - Low
    }
}