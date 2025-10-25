using UnityEngine;
using Resonance.Utilities.Waves;

namespace Resonance.Interfaces
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
    }
}