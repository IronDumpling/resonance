using UnityEngine;
using Resonance.Systems.Waves;
using Resonance.Shared.Types;

namespace Resonance.Shared.Interfaces
{
    /// <summary>
    /// Interface for objects that have a Wave system (Player, Enemy)
    /// Can perform wave attacks on other IWavable objects
    /// </summary>
    public interface IWavable
    {
        /// <summary>
        /// Get the Wave object associated with this IWavable
        /// </summary>
        Wave GetWave();

        /// <summary>
        /// Get the base damages for wave attacks
        /// </summary>
        Damages GetWaveBaseDamages();

        /// <summary>
        /// Apply wave damages from a source wavable
        /// </summary>
        /// <param name="damages">Damages to apply</param>
        /// <param name="sourceWavable">The source of the wave attack</param>
        /// <param name="description">Description of the damage source</param>
        /// <returns>True if damage was successfully applied</returns>
        bool ApplyWaveDamages(Damages damages, IWavable sourceWavable, string description = "Wave Damage");
    }
}