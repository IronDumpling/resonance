using UnityEngine;

namespace Resonance.Utilities.Types
{
    /// <summary>
    /// Wave chaos state enumeration
    /// </summary>
    public enum WaveChaosState
    {
        Order,      // Chaos < threshold
        Chaos       // Chaos >= max
    }

    /// <summary>
    /// Waveform type enumeration
    /// </summary>
    public enum WaveformType
    {
        Sine,
        Square,
        Triangle,
        Sawtooth,
        Constant,
        Custom
    }

    /// <summary>
    /// Wave interaction result enumeration
    /// </summary>
    public enum WaveInteractionResult
    {
        Perfect, // > 90%
        Good,  // > 75%
        Miss   // < 75%
    }
}