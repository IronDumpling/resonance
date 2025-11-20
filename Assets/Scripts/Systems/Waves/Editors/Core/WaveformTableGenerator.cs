using UnityEngine;
using Resonance.Shared.Types;

namespace Resonance.Systems.Waves
{
    public static class WaveformTableGenerator
    {
        public static float[] Generate(WaveformType type, int resolution)
        {
            float[] table = new float[resolution];
            switch (type)
            {
                case WaveformType.Sine:
                    GenerateSine(table, resolution);
                    break;
                case WaveformType.Square:
                    GenerateSquare(table, resolution); // Default 50% duty cycle
                    break;
                case WaveformType.Triangle:
                    GenerateTriangle(table, resolution);
                    break;
                case WaveformType.Sawtooth:
                    GenerateSawtooth(table, resolution); // Default rising sawtooth
                    break;
                case WaveformType.Constant:
                    GenerateConstant(table, resolution);
                    break;
                case WaveformType.Custom: // Custom means it's a result of operations
                    Debug.LogWarning("WaveformTableGenerator: Custom waveform type not implemented.");
                    break;
                default:
                    // Maybe return silence or a sine wave as default
                    GenerateSine(table, resolution);
                    break;
            }
            return table;
        }

        #region Default Generate Waveform Methods

        private static void GenerateSine(float[] table, int resolution)
        {
            for (int i = 0; i < resolution; i++)
            {
                table[i] = Mathf.Sin((float)i / resolution * 2f * Mathf.PI);
            }
        }

        private static void GenerateSquare(float[] table, int resolution, float dutyCycle = 0.5f)
        {
            int splitPoint = Mathf.Clamp(Mathf.RoundToInt(resolution * dutyCycle), 0, resolution);
            for (int i = 0; i < resolution; i++)
            {
                table[i] = (i < splitPoint) ? 1.0f : -1.0f;
            }
        }

        private static void GenerateTriangle(float[] table, int resolution)
        {
            for (int i = 0; i < resolution; i++)
            {
                float phase = (float)i / resolution;
                if (phase < 0.5f)
                {
                    table[i] = -1.0f + 4.0f * phase; // Rises from -1 to 1 over first half
                }
                else
                {
                    table[i] = 1.0f - 4.0f * (phase - 0.5f); // Falls from 1 to -1 over second half
                }
            }
        }

        private static void GenerateSawtooth(float[] table, int resolution) // Rising Saw
        {
            for (int i = 0; i < resolution; i++)
            {
                table[i] = -1.0f + 2.0f * ((float)i / resolution);
            }
        }

        private static void GenerateConstant(float[] table, int resolution)
        {
            for (int i = 0; i < resolution; i++)
            {
                table[i] = 0.0f;
            }
        }

        #endregion

        #region Parameterized Generate Waveform Methods

        /// <summary>
        /// Generate square/pulse wave with custom duty cycle
        /// </summary>
        public static float[] GenerateSquareParameterized(int resolution, float dutyCycle)
        {
            float[] table = new float[resolution];
            GenerateSquare(table, resolution, dutyCycle);
            return table;
        }
        
        /// <summary>
        /// Generate triangle wave with custom rise and fall amplitudes
        /// </summary>
        public static float[] GenerateTriangleParameterized(int resolution, float riseAmplitude, float fallAmplitude)
        {
            float[] table = new float[resolution];
            
            // If amplitudes are equal, generate symmetric triangle
            if (Mathf.Approximately(riseAmplitude, fallAmplitude))
            {
                GenerateTriangle(table, resolution);
                // Scale by amplitude
                for (int i = 0; i < resolution; i++)
                {
                    table[i] *= riseAmplitude;
                }
            }
            else
            {
                // Asymmetric triangle (sawtooth-like)
                float totalAmplitude = riseAmplitude + fallAmplitude;
                float risePhase = riseAmplitude / totalAmplitude;
                
                for (int i = 0; i < resolution; i++)
                {
                    float phase = (float)i / resolution;
                    if (phase < risePhase)
                    {
                        // Rising phase
                        table[i] = -fallAmplitude + (riseAmplitude + fallAmplitude) * (phase / risePhase);
                    }
                    else
                    {
                        // Falling phase
                        float fallPhase = (phase - risePhase) / (1.0f - risePhase);
                        table[i] = riseAmplitude - (riseAmplitude + fallAmplitude) * fallPhase;
                    }
                }
            }
            
            return table;
        }
        
        /// <summary>
        /// Generate noise waveform
        /// </summary>
        public static float[] GenerateNoise(int resolution)
        {
            float[] table = new float[resolution];
            System.Random random = new System.Random();
            for (int i = 0; i < resolution; i++)
            {
                table[i] = (float)(random.NextDouble() * 2.0 - 1.0); // Range: [-1, 1]
            }
            return table;
        }

        #endregion
    }
}