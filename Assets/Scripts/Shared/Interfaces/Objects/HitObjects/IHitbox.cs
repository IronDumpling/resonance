using Resonance.Shared.Types;

namespace Resonance.Shared.Interfaces
{
    public interface IHitbox
    {
        /// <summary>
        /// Process physical damage from shooting system
        /// </summary>
        /// <param name="damageInfo">Incoming damage information</param>
        /// <returns>Modified damage information after hitting this hitbox</returns>
        DamageInfo ProcessDamageHit(DamageInfo damageInfo);
    }
}