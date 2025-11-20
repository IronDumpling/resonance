using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Shared.Types
{
    /// <summary>
    /// Wave active state enumeration
    /// </summary>
    public enum WaveActiveState
    {
        Resonance,
        High_Functioning_II,
        High_Functioning_I,
        Normal,
        Low_Functioning,
        Hibernation
    }

    /// <summary>
    /// Waveform type enumeration
    /// </summary>
    public enum WaveformType
    {
        Sine,
        Pulse,
        Square,
        Triangle,
        Sawtooth,
        Constant,
        Custom
    }

    /// <summary>
    /// Wave modifier type enumeration
    /// </summary>
    public enum WaveModifierType
    {
        Inverter,    // Invert the wave
        Amplifier,   // Amplify the wave
        Filter       // Filter the wave
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