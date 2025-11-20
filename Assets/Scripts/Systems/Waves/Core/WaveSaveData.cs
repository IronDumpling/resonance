using UnityEngine;
using Resonance.Shared.Types;

namespace Resonance.Systems.Waves
{
    /// <summary>
    /// Wave save data structure
    /// Stores only primary properties; secondary properties are calculated on load
    /// </summary>
    [System.Serializable]
    public class WaveSaveData
    {
        // Primary properties
        public WaveformType waveformType;
        public float frequency;
        public float amplitude;
        public float unit;
        public float[] waveformTable;
        
        public WaveSaveData()
        {
            waveformType = WaveformType.Sine;
            frequency = 1.0f;
            amplitude = 1.0f;
            unit = 1.0f;
            waveformTable = null; // Will be generated based on waveformType
        }
    }
}
