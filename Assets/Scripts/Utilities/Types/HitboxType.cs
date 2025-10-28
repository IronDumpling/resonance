using UnityEngine;

namespace Resonance.Utilities.Types
{
    public enum HitboxType 
    { 
        Head,
        Body,
        Knee,
        Core, 
    }

    /// <summary>
    /// Hitbox damage multiplier configuration
    /// Defines how each hitbox type modifies incoming damage
    /// </summary>
    [System.Serializable]
    public class HitboxMultipliers
    {
        public float physicalHealthMultiplier = 1f;
        public float coreHealthMultiplier = 0f;
        public float chaosMultiplier = 1f;

        public HitboxMultipliers(float physical, float core, float chaos)
        {
            physicalHealthMultiplier = physical;
            coreHealthMultiplier = core;
            chaosMultiplier = chaos;
        }
    }
}