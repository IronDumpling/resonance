using UnityEngine;

namespace Resonance.Player.Shooting
{
    /// <summary>
    /// Recoil state - Runtime data for weapon recoil system
    /// Tracks accumulated recoil offset and recovery timing
    /// </summary>
    public class RecoilState
    {
        // Recoil offset (world space)
        public Vector3 currentRecoilOffset;    // Current recoil offset applied to aim point
        
        // Consecutive shooting tracking
        public int consecutiveShots;           // Number of shots fired consecutively
        
        // Recovery timing
        public float recoveryTimer;            // Time until recoil starts recovering
        
        /// <summary>
        /// Initialize state with zero recoil
        /// </summary>
        public RecoilState()
        {
            currentRecoilOffset = Vector3.zero;
            consecutiveShots = 0;
            recoveryTimer = 0f;
        }
        
        /// <summary>
        /// Reset state to zero recoil
        /// </summary>
        public void Reset()
        {
            currentRecoilOffset = Vector3.zero;
            consecutiveShots = 0;
            recoveryTimer = 0f;
        }
    }
}

