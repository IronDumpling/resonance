using UnityEngine;
using Resonance.Utilities.Types;

namespace Resonance.Utilities.Waves
{
    public static class WaveModifier
    {
        public static float[] Modify(float[] waveformTable, WaveModifierType modifierType)
        {
            switch (modifierType)
            {
                case WaveModifierType.Inverter:
                    return Inverter(waveformTable);
                case WaveModifierType.Amplifier:
                    return Amplifier(waveformTable, 2f);
                case WaveModifierType.Filter:
                    return Filter(waveformTable);
                default:
                    return waveformTable;
            }
        }

        private static float[] Inverter(float[] waveformTable)
        {
            for (int i = 0; i < waveformTable.Length; i++)
            {
                waveformTable[i] = -waveformTable[i];
            }
            return waveformTable;
        }

        private static float[] Amplifier(float[] waveformTable, float multiplier)
        {
            for (int i = 0; i < waveformTable.Length; i++)
            {
                waveformTable[i] = waveformTable[i] * multiplier;
            }
            return waveformTable;
        }

        private static float[] Filter(float[] waveformTable)
        {
            for (int i = 0; i < waveformTable.Length; i++)
            {
                waveformTable[i] = Mathf.Clamp(waveformTable[i], -1f, 1f);
            }
            return waveformTable;
        }
    }
}