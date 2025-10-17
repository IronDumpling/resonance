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
    /// Wave chaos state enumeration
    /// </summary>
    public enum WaveChaosState
    {
        Order,      // Chaos < threshold
        Chaos       // Chaos >= max
    }
}