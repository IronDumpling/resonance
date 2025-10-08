using UnityEngine;

namespace Resonance.Interfaces
{
    /// <summary>
    /// Interface for objects that can be used
    /// Used by the new PlayerUseAction system
    /// </summary>
    public interface IUsable
    {
        /// <summary>
        /// Use the object
        /// </summary>
        void Use();
    }
}