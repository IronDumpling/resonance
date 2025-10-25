using UnityEngine;

namespace Resonance.Utilities
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
        Noise
    }
}