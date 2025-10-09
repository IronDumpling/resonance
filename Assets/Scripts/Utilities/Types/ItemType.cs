using UnityEngine;

namespace Resonance.Utilities
{
    /// <summary>
    /// Item type enum
    /// </summary>
    public enum ItemType
    {
        Consumable,    // Ammo, etc.
        Tool,          // Key, etc.
        Module,        // Wave Module
        Weapon         // Pistol, etc.
    }

    public enum ConsumableType
    {
        Ammo,
        CoreEnergy
    }
}