using UnityEngine;

namespace Resonance.Shared.Types
{
    /// <summary>
    /// Item type enum
    /// </summary>
    public enum ItemType
    {
        Consumable,    // EnergyBottle, Healant, etc.
        Tool,          // Key, etc.
        Module,        // Wave Module (WaveModuleGraph items)
        WaveOutput,    // WaveGun, CrystalCore, WaveDiffuser, etc.
    }

    /// <summary>
    /// Consumable type enum
    /// </summary>
    public enum ConsumableType
    {
        EnergyBottle,  // Restores Crystal Core Energy
        Healant,       // Restores Crystal Core Health
        None           // Default/unspecified
    }

    /// <summary>
    /// Wave output device type enum
    /// </summary>
    public enum WaveOutputType
    {
        WaveGun,        // Fires wave projectiles
        CrystalCore,    // Crystal resonator - amplifies/modifies waves
        WaveDiffuser    // Wave diffuser - creates area effects
    }
}